using System.Collections.Generic;
using Code.Buildings.Logic.CoreBuilding;
using Code.Commandable;
using Code.Core.Bus;
using Code.GameEvents.Building;
using Code.Military.Commanders;
using Code.Military.Squads;
using UnityEngine;

namespace Code.EnemyAI
{
    /// <summary>
    /// 출격한 적 병력을 끝까지 소유하는 부대입니다. 가장 가까운 플레이어 기지를 목표로 삼고,
    /// 목표가 무너지면 다음 기지로 넘어가며, 명부가 비면 스스로 사라집니다.
    /// 지휘관이 붙어 있으면 두뇌(EnemyLineBrain)가 선으로 움직이고, 지휘관이 죽으면 예전처럼 본부로 직진합니다.
    /// </summary>
    public class AssaultForce : MonoBehaviour
    {
        /// <summary>
        /// 부대의 현재 중심 좌표입니다. 미니맵이 읽습니다.
        /// </summary>
        public Vector3 Center { get; private set; }

        /// <summary>
        /// 현재 목표 기지입니다. 없으면 null입니다.
        /// </summary>
        public HeadquartersBuilding Target => _target;

        /// <summary>
        /// 생존 병력 수입니다. 분대는 생존 인원만큼 셉니다.
        /// </summary>
        public int Strength { get; private set; }

        /// <summary>
        /// 이 갈래의 두뇌입니다. 지휘관 없이 나온 갈래는 null입니다.
        /// </summary>
        public EnemyLineBrain Brain { get; private set; }

        private const float TickInterval = 1f;

        private readonly List<AbstractCommandable> _roster = new(16);
        private BusSubscriptionScope _scope;
        private HeadquartersBuilding _target;
        private float _nextTick;
        private bool _brainFell;

        private void Awake()
        {
            _scope = new BusSubscriptionScope();
            _scope.Subscribe<BuildingDeathEvent>(Owner.Player, HandlePlayerBuildingDied);
            AssaultRegistry.Register(this);
        }

        private void OnDestroy()
        {
            _scope?.Dispose();
            AssaultRegistry.Unregister(this);
        }

        private void Update()
        {
            if (Time.time < _nextTick)
                return;

            _nextTick = Time.time + TickInterval;

            Prune();
            if (_roster.Count == 0)
            {
                Destroy(gameObject);
                return;
            }

            RefreshCenter();

            // 이벤트를 놓쳤거나 목표가 다른 경로로 사라진 경우의 보험
            if (_target == null)
                AcquireTarget();

            if (!HasBrain)
            {
                // 지휘관이 방금 쓰러졌다 — 선이 사라진 분대에 예전 방식으로 한 번 명령을 준다
                if (Brain != null && !_brainFell)
                {
                    _brainFell = true;
                    IssueOrders();
                }

                return;
            }

            Brain.Tick(TickInterval, Center, _target);
        }

        private bool HasBrain => Brain is { IsAlive: true };

        public static AssaultForce Create(List<AbstractCommandable> units, Commander commander = null)
        {
            GameObject host = new(nameof(AssaultForce));
            AssaultForce force = host.AddComponent<AssaultForce>();
            force._roster.AddRange(units);

            foreach (AbstractCommandable unit in units)
                unit.WasDispatched = true;

            if (commander != null)
            {
                commander.WasDispatched = true;
                force._roster.Add(commander);
                force.Brain = new EnemyLineBrain(commander, units);
            }

            force.RefreshCenter();

            // 명령은 첫 틱에서 내린다 — 방금 생성한 분대는 아직 Start 전이라
            // 스탯도 분대원도 없어서 지금 Attack을 부르면 터진다. 첫 Update는 모든 Start 뒤에 돈다
            return force;
        }

        /// <summary>
        /// 플레이어 기지가 파괴되면 목표를 갱신합니다.
        /// 파괴 중인 오브젝트는 ==로 판정할 수 없으므로 참조 동일성으로 확인합니다.
        /// </summary>
        private void HandlePlayerBuildingDied(BuildingDeathEvent evt)
        {
            if (!ReferenceEquals(evt.Building, _target))
                return;

            _target = null;
            AcquireTarget();
        }

        /// <summary>
        /// 목표가 없으면 가장 가까운 플레이어 기지를 찾습니다. 두뇌가 없을 때만 직접 명령합니다.
        /// </summary>
        private void AcquireTarget()
        {
            _target = FindNextTarget();

            if (_target == null)
                return; // 남은 기지가 없다 = GameOver가 났거나 곧 난다

            if (!HasBrain)
                IssueOrders();
        }

        /// <summary>
        /// 부대 명부의 병력에게 목표를 향해 이동 또는 공격 명령을 내립니다.
        /// </summary>
        private void IssueOrders()
        {
            if (_target == null)
                return;

            foreach (AbstractCommandable unit in _roster)
            {
                if (unit == null || unit.IsDead)
                    continue;

                // 분대에 이동을 시키면 SquadMovingState가 센서를 안 읽어 본부 앞에 멈춰 선다.
                // 목적지가 본부 중심이라 도달 판정도 안 나 Idle로 넘어가지 못한다
                if (unit is Squad squad)
                    squad.Attack(_target);
                else
                    unit.MoveTo(_target.transform.position);
            }
        }

        /// <summary>
        /// 죽었거나 파괴된 병력을 명부에서 제거합니다.
        /// </summary>
        private void Prune()
        {
            // 뒤에서부터 지운다 — 앞에서 지우면 인덱스가 밀린다
            for (int i = _roster.Count - 1; i >= 0; i--)
                if (_roster[i] == null || _roster[i].IsDead)
                    _roster.RemoveAt(i);
        }

        /// <summary>
        /// 생존 병력의 평균 좌표와 병력 수를 다시 계산합니다.
        /// </summary>
        private void RefreshCenter()
        {
            Vector3 sum = Vector3.zero;
            int alive = 0;
            int strength = 0;

            foreach (AbstractCommandable unit in _roster)
            {
                if (unit == null || unit.IsDead)
                    continue;

                sum += unit.transform.position;
                alive++;

                // 분대는 인원만큼 센다 — 항목 수로 세면 5명짜리 분대가 1이 된다
                strength += unit is ISquad squad ? squad.AliveCount : 1;
            }

            Strength = strength;

            if (alive == 0)
                return; // 다음 틱의 Prune이 명부를 비우고 부대가 사라진다

            Center = sum / alive;
            transform.position = Center; // 씬 뷰에서 부대 위치를 눈으로 보려고 맞춰 둔다
        }

        /// <summary>
        /// 중심에서 가장 가까운 플레이어 기지를 찾습니다. 남은 기지가 없으면 null입니다.
        /// </summary>
        private HeadquartersBuilding FindNextTarget()
        {
            HeadquartersBuilding best = null;
            float bestSqr = float.MaxValue;

            foreach (HeadquartersBuilding hq in FindObjectsByType<HeadquartersBuilding>())
            {
                // Owner를 거르지 않으면 적이 자기 본진을 치러 간다
                if (hq == null || hq.Owner != Owner.Player || hq.IsDead)
                    continue;

                float sqr = ((Vector2)hq.transform.position - (Vector2)Center).sqrMagnitude;
                if (sqr >= bestSqr)
                    continue;

                bestSqr = sqr;
                best = hq;
            }

            return best;
        }
    }
}
