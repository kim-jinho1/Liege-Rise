using System.Collections.Generic;
using Code.Commandable.Interfaces;
using Code.Core.Bus;
using Code.Military.Data;
using Code.Military.Squads;
using Code.Military.Stats;
using UnityEngine;

namespace Code.Military.Combat.FSM
{
    /// <summary>
    /// 분대 교전 상태 기계입니다. MonoBehaviour가 아니며 Squad가 생성하고 Update에서 구동합니다.
    /// Squad 참조는 들고 있지 않고 GameObject만 잡아, 상태가 소유자에 직접 도달하지 못하게 합니다.
    /// </summary>
    public class SquadStateMachine
    {
        /// <summary>
        /// 분대원 명부입니다. 상태가 생성자에서 잡아갑니다.
        /// </summary>
        public SquadRoster Roster { get; }

        /// <summary>
        /// 분대 정면입니다. 상태가 생성자에서 잡아갑니다.
        /// </summary>
        public SquadFacing Facing { get; }

        /// <summary>
        /// 분대 센서입니다. 상태가 생성자에서 잡아갑니다.
        /// </summary>
        public SquadSensor Sensor { get; }

        /// <summary>
        /// 현재 상태의 종류입니다.
        /// </summary>
        public SquadStateType Current => _current.Type;

        /// <summary>
        /// 씬 뷰 기즈모에 표시할 상태 라벨입니다.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// 분대 루트의 현재 위치입니다.
        /// </summary>
        public Vector3 Origin => _root.transform.position;

        /// <summary>
        /// ClosestPoint 계산의 기준이 되는 분대 루트 오브젝트입니다.
        /// </summary>
        public GameObject RootObject => _root;

        /// <summary>
        /// 분대의 소유자입니다. 명령 없이 스스로 나서는 판정이 이것을 봅니다 —
        /// 적은 AI가 준 길만 걷습니다(길 옆에 숨은 매복조를 찾아 나서면 매복이 성립하지 않습니다).
        /// </summary>
        public Owner Owner { get; }

        /// <summary>
        /// 분대가 처음 선 자리입니다. 순찰은 이 지점을 벗어나지 않습니다.
        /// </summary>
        public Vector3 HomePost { get; }

        /// <summary>
        /// 분대원의 공격 사거리입니다(MemberSO.AttackConfig 기준).
        /// </summary>
        public float AttackRange { get; }

        /// <summary>
        /// 공격 가능한 정면 콘의 전체 각도입니다. 판정은 이 값의 절반과 비교합니다.
        /// </summary>
        public float ConeAngle { get; }

        /// <summary>
        /// 교전으로 넘어가도 되는 거리입니다. 공격 사거리에 대형 여유를 더한 값입니다.
        /// 접근 정지 지점이 사거리의 80%인데 교전 판정까지 사거리 그대로면 여유가 0이라,
        /// 멤버 콜라이더·분리 반경·대형 간격이 조금만 밀어내도 문턱을 못 넘고 접근만 반복합니다.
        /// 교전에 들어가면 멤버가 각자 BT로 사거리를 좁히므로 여유를 줘도 손해가 없습니다.
        /// </summary>
        public float EngageRange => AttackRange + FormationSlack;

        // 대형 폭·분리 반경이 만드는 오차. 근접 분대(사거리 1.2)에서 특히 크게 작용한다.
        private const float FormationSlack = 0.75f;

        /// <summary>
        /// 이동 명령으로 지정된 목적지입니다.
        /// </summary>
        public Vector3 Destination { get; private set; }

        /// <summary>
        /// 전진 명령이 준 정면 방향입니다. 목적지가 아니라 지휘관이 미는 방향을 봅니다.
        /// </summary>
        public Vector2 AdvanceFacing { get; private set; }

        /// <summary>
        /// 플레이어가 지정한 공격 목표입니다. 없으면 null입니다.
        /// </summary>
        public IDamageable CommandedTarget { get; private set; }

        /// <summary>
        /// 지금 교전 중인 목표입니다. 교전 진입 시 확정되며 매 틱 센서를 다시 읽지 않습니다.
        /// </summary>
        public IDamageable EngageTarget { get; private set; }

        /// <summary>
        /// 분대 데이터입니다. 상태가 수치를 읽을 통로이며 Squad 자체로는 도달하지 못합니다.
        /// </summary>
        public SquadSO SquadSO { get; }

        /// <summary>
        /// 분대의 런타임 스탯 시트입니다. 물려받기 전에는 null입니다.
        /// </summary>
        public StatSheet Stats { get; private set; }

        private readonly GameObject _root; // Squad 타입을 들고 있지 않는다
        private readonly Dictionary<SquadStateType, SquadState> _states = new();
        private SquadState _current;

        private bool CanOrder => _current is ICanReceiveOrder;

        public SquadStateMachine(Squad squad, SquadRoster roster, SquadFacing facing, SquadSensor sensor)
        {
            Roster = roster;
            Facing = facing;
            Sensor = sensor;
            _root = squad.gameObject;
            Owner = squad.Owner;
            HomePost = squad.transform.position; // 분대원 스폰 전이라 아직 배치 지점 그대로다

            SquadSO squadSO = squad.AbstractCommandableSO as SquadSO;
            Debug.Assert(squadSO != null && squadSO.MemberSO != null && squadSO.MemberSO.AttackConfig != null,
                $"{squad.name}: SquadSO / MemberSO / AttackConfig가 비어 있습니다.");

            SquadSO = squadSO;
            AttackRange = squadSO.MemberSO.AttackConfig.AttackRange;
            ConeAngle = squadSO.AttackConeAngle;

            Register(new SquadIdleState(this));
            Register(new SquadMovingState(this));
            Register(new SquadAdvancingState(this));
            Register(new SquadApproachingState(this));
            Register(new SquadEngagingState(this));
            Register(new SquadMountedState(this));

            _current = _states[SquadStateType.Idle]; // Enter()를 부르지 않는다 — 아직 분대원 스폰 전이다
            Label = nameof(SquadStateType.Idle);
        }

        private void Register(SquadState state)
            => _states.Add(state.Type, state);

        /// <summary>
        /// 현재 상태를 한 프레임 진행합니다. 전멸 가드는 소유자가 겁니다.
        /// </summary>
        public void UpdateMachine(float deltaTime)
            => _current.Tick(deltaTime);

        /// <summary>
        /// 상태를 교체합니다. 같은 타입이어도 Exit → Enter를 태웁니다 —
        /// Moving → Moving 재진입이 새 목적지를 반영하는 유일한 경로이기 때문입니다.
        /// </summary>
        public void ChangeState(SquadStateType type)
        {
            Debug.Assert(_states.ContainsKey(type), $"등록되지 않은 분대 상태입니다: {type}");
            _current.Exit();
            _current = _states[type];
            Label = type.ToString();
            _current.Enter();
        }

        /// <summary>
        /// 목표를 확정하고 교전 상태로 넘어갑니다. 상태 쪽 공용 헬퍼가 부릅니다.
        /// </summary>
        public void EngageWith(IDamageable target)
        {
            EngageTarget = target;
            ChangeState(SquadStateType.Engaging);
        }

        /// <summary>
        /// 이동 명령입니다. 명령을 받을 수 있는 상태에서만 처리됩니다.
        /// </summary>
        public void OrderMove(Vector3 destination)
        {
            if (!CanOrder)
                return;

            CommandedTarget = null;
            Destination = destination;
            ChangeState(SquadStateType.Moving);
        }

        /// <summary>
        /// 전진 명령입니다. 지휘관이 선 위 자리와 미는 방향을 함께 줍니다.
        /// </summary>
        public void OrderAdvance(Vector3 destination, Vector2 facing)
        {
            if (!CanOrder)
                return;

            CommandedTarget = null;
            Destination = destination;
            AdvanceFacing = facing;
            ChangeState(SquadStateType.Advancing);
        }

        /// <summary>
        /// 정지 명령입니다. 명령을 받을 수 있는 상태에서만 처리됩니다.
        /// </summary>
        public void OrderStop()
        {
            if (!CanOrder)
                return;

            CommandedTarget = null;
            Roster.HaltAll();
            ChangeState(SquadStateType.Idle);
        }

        /// <summary>
        /// 지정 공격 명령입니다. 명령을 받을 수 있는 상태에서만 처리됩니다.
        /// </summary>
        public void OrderAttack(IDamageable target)
        {
            if (!CanOrder)
                return;

            CommandedTarget = target;
            ChangeState(SquadStateType.Approaching);
        }

        /// <summary>
        /// 스탯 시트를 물려받습니다. 상태가 스탯에 보정을 걸 통로입니다.
        /// </summary>
        // Squad는 Awake에 기계를, Start에 스탯을 만든다 — 생성자로는 받을 수 없어 따로 물린다
        public void BindStats(StatSheet stats)
            => Stats = stats;

        /// <summary>
        /// 타워 탑승 상태로 넣거나 뺍니다. 명령이 아니라 상태 전환이므로 CanOrder를 검사하지 않습니다 —
        /// 검사하면 Mounted에서 영원히 빠져나오지 못합니다.
        /// </summary>
        public void SetMounted(bool mounted)
            => ChangeState(mounted ? SquadStateType.Mounted : SquadStateType.Idle);

        /// <summary>
        /// 분대원 사망을 현재 상태에 전달합니다.
        /// </summary>
        public void OnMemberLost()
            => _current.OnMemberLost();

        /// <summary>
        /// 대형이 바뀌었음을 현재 상태에 전달합니다. 어디에 다시 세울지는 상태가 압니다.
        /// </summary>
        public void NotifyFormationChanged()
            => _current.OnFormationChanged();

        /// <summary>
        /// 분대원 피격을 현재 상태에 전달합니다. 반격 여부는 상태가 판단합니다.
        /// </summary>
        public void OnMemberDamaged(Vector3 attackerDirection)
            => _current.OnDamaged(attackerDirection);
    }
}
