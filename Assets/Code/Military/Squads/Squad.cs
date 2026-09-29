using System.Collections.Generic;
using Code.Buildings.Logic.DefenseBuilding;
using Code.Commandable;
using Code.Commandable.Interfaces;
using Code.Core.Bus;
using Code.GameEvents.Unit;
using Code.Military.Combat;
using Code.Military.Combat.FSM;
using Code.Military.Commanders;
using Code.Military.Data;
using Code.Military.Reinforcement;
using Code.Military.Squads.Formations;
using Code.Military.Stats;
using Code.Units.Combat;
using UnityEngine;

namespace Code.Military.Squads
{
    /// <summary>
    /// 분대 루트입니다. 콜라이더 없는 논리 객체이며 위치는 생존 분대원의 무게중심을 따라갑니다.
    /// 명령을 받아 상태 기계에 넘기고 협력 컴포넌트를 배선하는 것이 이 클래스의 전부입니다.
    /// 상속받은 Health는 사용하지 않습니다(실제 최대 체력은 Start에서 재계산합니다).
    /// </summary>
    public class Squad : AbstractCommandable, ISquad, ISquadContext
    {
        public int AliveCount => _roster.AliveCount;

        /// <summary>살아 있는 분대원 목록입니다. UI가 개별 체력을 읽는 통로입니다.</summary>
        public IReadOnlyList<AbstractCombatUnit> Members => _roster.Members;

        public int MissingCount
            => Mathf.Max(0, SquadSO.MemberCount - AliveCount);

        /// <summary>이 분대를 거느린 지휘관입니다. 배속은 CommanderRoster만 바꿉니다.</summary>
        public Commander Commander { get; private set; }

        public bool HasCommander => Commander != null;

        /// <summary>배속이 바뀔 때입니다. 깃발이 지휘관 색을 받는 통로입니다.</summary>
        public event System.Action CommanderChanged;

        public float DamageMultiplier
            => _stats.Get(SquadSO.DamageMultiplierStat)?.Value ?? 1f;

        public float AttackDelayMultiplier
            => _stats.Get(SquadSO.AttackDelayMultiplierStat)?.Value ?? 1f;

        /// <summary>런타임 스탯 시트입니다. 지휘관 버프가 보정을 꽂는 통로이며 Start 전에는 null입니다.</summary>
        public StatSheet Stats => _stats;

        /// <summary>맞은 구간을 알립니다. 표시가 판정을 다시 하지 않도록 여기서 한 번만 정합니다.</summary>
        public event System.Action<ArmorZone> ZoneHit;

        /// <summary>지정한 방향에서 들어온 피해에 곱할 방향 배율입니다. 맞은 멤버가 호출합니다.</summary>
        public float DirectionalMultiplierFrom(Vector3 attackerDirection)
        {
            if (_armor == null)
                return 1f;

            ArmorZone zone = _armor.ZoneFrom(attackerDirection);
            _armor.Record(zone, attackerDirection);
            ZoneHit?.Invoke(zone);
            return _armor.MultiplierOf(zone);
        }

        /// <summary>방향 피해 판정과 마지막 피격 기록입니다. Start 전에는 null입니다.</summary>
        public SquadArmor Armor => _armor;

        /// <summary>그 방향에서 맞으면 어느 구간인지만 봅니다. 피해도 알림도 없습니다 — 공세선 미리보기가 씁니다.</summary>
        public ArmorZone PeekZone(Vector3 attackerDirection)
            => _armor?.ZoneFrom(attackerDirection) ?? ArmorZone.Front;

        private SquadSO SquadSO
            => AbstractCommandableSO as SquadSO;

        /// <summary>결원 보충을 맡는 협력 객체입니다.</summary>
        public SquadReinforcement Reinforcement { get; private set; }
        /// <summary>대형 전환·재정렬과 그에 딸린 전투 보정을 맡는 협력 객체입니다.</summary>
        public SquadFormation Formation { get; private set; }

        /// <summary>현재 상태입니다. 지휘관 두뇌가 싸우는 분대를 건드리지 않으려고 봅니다.</summary>
        public SquadStateType State => _machine.Current;

        /// <summary>센서에 살아 있는 적이 잡혀 있는지입니다. 지휘관 두뇌의 "적이 있으면 민다" 판정입니다.</summary>
        public bool SeesEnemy
            => _sensor.Target is { } target && target.Transform != null && target.CurrentHealth > 0;
        public SquadSensor Sensor => _sensor; // 두뇌가 밀 방향을 그 적 쪽으로 틀 때 읽는다

        private StatSheet _stats;
        private SquadArmor _armor;
        private SquadTowerMount _towerMount;
        private SquadRoster _roster;
        private SquadFacing _facing;
        private SquadSensor _sensor;
        private SquadStateMachine _machine;
        private SquadFlagView _flag;

        protected override void Awake()
        {
            base.Awake();
            _roster = GetComponent<SquadRoster>();
            _facing = GetComponent<SquadFacing>();
            _sensor = GetComponent<SquadSensor>();
            _machine = new SquadStateMachine(this, _roster, _facing, _sensor);
            _towerMount = new SquadTowerMount(_roster, _machine);
        }

        protected override void Start()
        {
            base.Start();
            _stats = new StatSheet(SquadSO.Stats);
            _machine.BindStats(_stats); // 상태가 보정을 걸 통로. 기계는 Awake에 이미 만들어져 있다
            _armor = new SquadArmor(SquadSO, _facing, _stats);
            Reinforcement = new SquadReinforcement(this, _roster, SquadSO.MemberSO.Cost);
            RefreshMaxHealth(SquadSO.MemberCount * SquadSO.MemberSO.Health); // 5 × 300 = 1500

            _roster.SetUp(this, SquadSO);
            _roster.HealthChanged += HandleRosterHealthChanged;
            Formation = new SquadFormation(_roster, _facing, _machine, _stats, SquadSO);
            Formation.SelectInitial(0); // 회전 속도·밀어내기 반경·전투 보정이 같이 잡힌다
            _sensor.Setup(Owner, SquadSO.DetectRange);

            _flag = SquadFlagView.Attach(this, SquadSO.Flag);
            SquadOrderArrowView.Attach(this, _machine);
            SquadArcView.Attach(this, SquadSO, _facing);
            RefreshCommanderBadge();

            _roster.SpawnMembers(); // 반드시 센서 Setup 뒤 — 첫 스캔이 자기 분대원을 집는다
            Bus<SquadSpawnEvent>.Raise(Owner, new SquadSpawnEvent(Owner, this));
        }

        /// <summary>분대가 전멸했음을 알립니다. 지휘관 명부는 다음 틱에 스스로 정리합니다.</summary>
        public override void Die()
        {
            Bus<SquadDeathEvent>.Raise(Owner, new SquadDeathEvent(Owner, this));
            base.Die();
        }

        protected override void OnDestroy()
        {
            if (_roster != null)
                _roster.HealthChanged -= HandleRosterHealthChanged;

            Formation?.Dispose();
            base.OnDestroy();
        }

        private void Update()
        {
            if (AliveCount <= 0) // 전멸한 프레임에도 Update가 한 번 더 돈다
                return;

            _machine.UpdateMachine(Time.deltaTime);
            Reinforcement?.Tick(Time.deltaTime);
            Formation?.Tick(Time.deltaTime);
        }

        protected void LateUpdate()
        {
            if (AliveCount <= 0)
                return;

            transform.position = _roster.MembersCenter;
        }

        public override void Select()
        {
            base.Select();
            _roster.SetSelectionVisual(true);
        }

        public override void DeSelect()
        {
            base.DeSelect();
            _roster.SetSelectionVisual(false);
        }

        /// <summary>분대원 하나를 가리켜도 분대 전체에 외곽선이 뜹니다 — 클릭이 분대로 접히는 것과 같은 규칙입니다.</summary>
        public override void SetHoveredVisual(bool hovered) => _roster.SetHoveredVisual(hovered);
        public override void SetGuideHighlight(bool on) => _roster.SetGuideHighlight(on);

        /// <summary>분대원 체력 합을 루트 체력으로 옮깁니다. 루트는 콜라이더가 없어 이 경로가 유일한 갱신원입니다.</summary>
        private void HandleRosterHealthChanged()
        {
            int current = Mathf.Clamp(_roster.TotalCurrentHealth, 0, MaxHealth);
            if (current == CurrentHealth)
                return;

            int last = CurrentHealth;
            CurrentHealth = current;
            RaiseHealthUpdated(last, current);
        }

        /// <summary>분대원 사망을 알립니다. 전멸이면 분대도 죽고, 아니면 현재 상태가 대형의 구멍을 메웁니다.</summary>
        public void NotifyMemberDied(AbstractCombatUnit member)
        {
            _roster.NotifyDied(member);

            if (AliveCount <= 0)
            {
                Die();
                return;
            }

            _machine.OnMemberLost();
        }

        /// <summary>분대원이 피해를 입었음을 현재 상태에 알립니다. 대기 중이면 여기서 반격이 시작됩니다.</summary>
        public void NotifyMemberDamaged(Vector3 attackerDirection)
        {
            if (_machine == null || AliveCount <= 0)
                return;

            _machine.OnMemberDamaged(attackerDirection);
        }

        /// <summary>배속 지휘관을 바꿉니다. CommanderRoster만 부릅니다 — 명부와 이 값이 갈라지면 안 됩니다.</summary>
        public void SetCommander(Commander commander)
        {
            if (Commander == commander)
                return;

            Commander = commander;
            RefreshCommanderBadge();
            CommanderChanged?.Invoke();
        }

        /// <summary>깃발 배지를 지휘관 색·번호로 다시 칠합니다. 지휘관이 Start에서 번호를 받은 뒤에도 부릅니다.</summary>
        public void RefreshCommanderBadge()
        {
            if (_flag == null)
                return;

            if (Commander != null)
                _flag.SetBadge(Commander.Color, Commander.Ordinal);
            else
                _flag.ClearBadge();
        }

        /// <summary>정면 반대쪽으로 지정 거리만큼 떨어진 후방 지점입니다.</summary>
        public Vector3 RearPoint(float distance)
            => transform.position - (Vector3)(FacingDirection * distance);
        public Vector2 FacingDirection => _facing.Direction;

        /// <summary>이 분대가 탑승형 타워를 쓸 수 있는지 여부입니다.</summary>
        public bool CanMountTower
            => SquadSO != null && SquadSO.CanMountTower;

        /// <summary>현재 탑승 중인 타워입니다. 타워에 없으면 null입니다.</summary>
        public MannedTowerBuilding MountedTower => _towerMount?.Current;

        /// <summary>분대원을 탑승형 타워에 올립니다. 쓸 수 없는 분대면 무시됩니다.</summary>
        public void MountTower(MannedTowerBuilding tower)
        { if (CanMountTower) _towerMount.Mount(tower); }

        /// <summary>타워에서 내려 다시 명령을 받는 상태로 돌아갑니다.</summary>
        public void DismountTower()
            => _towerMount?.Dismount();

        /// <summary>분대를 지정한 위치로 이동시킵니다. 커맨드 SO가 호출하는 진입점입니다.</summary>
        public override void MoveTo(Vector3 destination)
            => _machine.OrderMove(destination);

        /// <summary>지휘관이 선 위 자리로 밉니다. 걷는 동안에도 적을 보고 붙습니다.</summary>
        public void Advance(Vector3 destination, Vector2 facing)
            => _machine.OrderAdvance(destination, facing);

        /// <summary>분대의 현재 행동을 중단하고 대기 상태로 되돌립니다.</summary>
        public void Stop() => _machine.OrderStop();

        /// <summary>지정한 목표를 공격하도록 명령합니다. 사거리 밖이면 대형째 접근합니다.</summary>
        public void Attack(IDamageable target)
            => _machine.OrderAttack(target);

        /// <summary>현재 적용 중인 대형입니다. 보유 목록이 비어 있으면 null입니다.</summary>
        public FormationSO CurrentFormation
            => _roster.CurrentFormation;

        /// <summary>이 분대가 지정한 대형을 사용할 수 있는지 확인합니다. UI 버튼의 활성 판정에 씁니다.</summary>
        public bool CanUseFormation(FormationSO formation)
            => _roster.CanUse(formation);

        /// <summary>자세를 변경합니다. 재정렬이 끝나야 새 대형의 전투 보정이 붙습니다.</summary>
        public void SetFormation(int index)
            => Formation?.Select(index);

        /// <summary>대형 에셋으로 자세를 변경합니다. 보유하지 않은 대형이면 무시됩니다.</summary>
        public void SetFormation(FormationSO formation)
            => Formation?.Select(formation);
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || _machine == null)
                return;

            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, _machine.Label);
        }
#endif
    }
}
