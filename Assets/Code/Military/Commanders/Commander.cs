using System;
using System.Collections.Generic;
using Code.Commandable.Interfaces;
using Code.Core.Bus;
using Code.GameEvents.Unit;
using Code.Military.Data;
using Code.Military.Squads;
using Code.Units.BT;
using Code.Units.Combat;
using UnityEngine;

namespace Code.Military.Commanders
{
    /// <summary>
    /// 지휘관 유닛입니다. 분대 3~6개를 거느리고 공세선 하나를 실행합니다.
    /// 상속받은 Squad(ISquadContext)는 비워 둡니다 — 채우면 분대원으로 집계되어 생존 수가 어긋납니다.
    /// 실제 일은 협력 객체가 나눠 갖습니다: 명부(CommanderRoster) · 두뇌(CommanderLineExecutor) · 버프(CommanderAura).
    /// </summary>
    public class Commander : AbstractCombatUnit
    {
        private const float FollowInterval = 0.5f;
        private const float ArriveThreshold = 0.3f;

        // 원거리 지휘관의 발사 위치입니다. 근접 지휘관은 비워 두면 AttackResolver가 근접으로 처리합니다.
        [SerializeField] private Transform firePositionTrm;

        /// <summary>거느린 분대 명부입니다. 배속·해제는 여기로만 합니다.</summary>
        public CommanderRoster Roster { get; private set; }

        public CommanderSO CommanderSO
            => AbstractCommandableSO as CommanderSO;

        /// <summary>
        /// 지휘관은 자기 자신이 선택됩니다. 기본 구현은 소속 분대로 접는데 지휘관은 분대가 없어 영영 안 잡힙니다.
        /// </summary>
        public override ISelectable SelectionProxy => this;

        /// <summary>같은 진영 안에서의 번호입니다(1부터). 죽으면 번호가 비고 다음 지휘관이 그 번호를 받습니다.</summary>
        public int Ordinal { get; private set; }

        /// <summary>깃발·공세선·배속 분대 깃발이 함께 쓰는 색입니다.</summary>
        public Color Color { get; private set; } = Color.white;

        public bool HasLine => _executor.HasLine;
        public BattleLine Line => _executor.Line;
        public float LineAdvance => _executor.Advance;

        /// <summary>선을 긋거나 지웠을 때입니다. 표시가 이것만 듣습니다.</summary>
        public event Action LineChanged;

        private static readonly List<Commander> Alive = new(16);

        /// <summary>
        /// 살아 있는 지휘관 전부입니다. 번호 순서는 보장하지 않으니 필요하면 부르는 쪽이 정렬합니다.
        /// </summary>
        public static IReadOnlyList<Commander> AliveCommanders => Alive;

        private readonly CommanderLineExecutor _executor = new();
        private CommanderAura _aura;
        private float _followTimer;

        protected override void Awake()
        {
            base.Awake();
            int capacity = CommanderSO != null ? CommanderSO.SquadCapacity : CommanderRoster.DefaultCapacity;
            Roster = new CommanderRoster(this, capacity);
        }

        protected override void Start()
        {
            base.Start();
            if (firePositionTrm != null)
                SetVariableValue(BTVariables.FirePositionTrm, firePositionTrm);

            CommanderSO so = CommanderSO;
            _aura = new CommanderAura(this, so != null ? so.Passive : null, so != null ? so.AuraRange : 6f);
            Roster.SquadReleased += _aura.Remove;

            ClaimOrdinal();
            Alive.Add(this);

            SquadMarkerStyleSO style = SquadMarkerStyleSO.Load();
            SquadFlagView flag = SquadFlagView.Attach(this, style.CommanderFlag, true);
            if (flag != null)
                flag.SetBadge(Color, Ordinal);

            // 임명 커맨드는 Start 전에 첫 분대를 배속한다 — 그때는 번호가 0이라 여기서 다시 칠한다
            for (int i = 0; i < Roster.Squads.Count; i++)
                Roster.Squads[i].RefreshCommanderBadge();

            BattleLineView.Attach(this);
        }

        protected override void OnDestroy()
        {
            Alive.Remove(this);
            base.OnDestroy();
        }

        /// <summary>
        /// 분대를 거느리는 동안에는 자동 교전하지 않습니다. 전선 뒤에서 버프만 공급합니다.
        /// </summary>
        public override bool CanAutoAttack()
            => Roster.Count == 0;

        /// <summary>
        /// 공세선을 받습니다. 거느린 분대가 없으면 그을 수 없습니다.
        /// </summary>
        public bool SetLine(in BattleLine line)
        {
            if (IsDead || Roster.Count == 0 || !line.IsValid)
                return false;

            _executor.SetLine(line);
            Stop();
            LineChanged?.Invoke();
            Bus<CommanderOrderEvent>.Raise(Owner,
                new CommanderOrderEvent(Owner, this, CommanderOrderKind.LineDrawn, line.Center));
            return true;
        }

        public void ClearLine()
        {
            if (!_executor.HasLine)
                return;

            _executor.Clear();
            LineChanged?.Invoke();
        }

        /// <summary>
        /// 죽으면 선이 사라지고 분대는 그 자리에 남습니다. 버프는 여기서 되돌립니다.
        /// </summary>
        public override void Die()
        {
            _executor.Clear();
            _aura?.Clear();
            Roster.ReleaseAll();
            LineChanged?.Invoke();
            base.Die();
        }

        private void Update()
        {
            if (IsDead)
                return;

            Roster.Prune();
            float deltaTime = Time.deltaTime;
            if (_executor.Tick(deltaTime, Roster.Squads))
                LineChanged?.Invoke(); // 전선에 다 섰고 적이 없다 — 선이 할 일을 마쳤다
            _aura.Tick(deltaTime, Roster.Squads, transform.position);
            Follow(deltaTime);
        }

        /// <summary>
        /// 전선 중앙 뒤를 따라갑니다. 대형 슬롯이 아니므로 로스터가 자리를 잡아 주지 않습니다.
        /// </summary>
        private void Follow(float deltaTime)
        {
            if (!_executor.HasLine || !_executor.HasProgress)
                return;

            _followTimer -= deltaTime;
            if (_followTimer > 0f)
                return;

            _followTimer = FollowInterval;

            float rear = CommanderSO != null ? CommanderSO.LineRearOffset : 3f;
            Vector3 point = _executor.FollowPoint(rear);
            point.z = transform.position.z;

            if ((transform.position - point).sqrMagnitude > ArriveThreshold * ArriveThreshold)
                MoveTo(point);
        }

        /// <summary>
        /// 같은 진영의 살아 있는 지휘관이 안 쓰는 가장 작은 번호를 받습니다.
        /// 색은 아군만 번호로 돌려 쓰고, 적은 몇이 서든 한 색입니다 — 색이 진영을 말해야 합니다.
        /// </summary>
        private void ClaimOrdinal()
        {
            int ordinal = 1;
            bool taken = true;

            while (taken)
            {
                taken = false;
                for (int i = 0; i < Alive.Count; i++)
                {
                    if (Alive[i].Owner != Owner || Alive[i].Ordinal != ordinal)
                        continue;

                    taken = true;
                    ordinal++;
                    break;
                }
            }

            Ordinal = ordinal;

            SquadMarkerStyleSO style = SquadMarkerStyleSO.Load();
            if (Owner != Owner.Player)
            {
                Color = style.EnemyCommanderColor;
                return;
            }

            Color[] palette = style.CommanderColors;
            if (palette != null && palette.Length > 0)
                Color = palette[(ordinal - 1) % palette.Length];
        }
    }
}
