using Code.Commandable.Interfaces;
using UnityEngine;

namespace Code.Military.Combat.FSM
{
    /// <summary>
    /// 지휘관이 민 전진 상태입니다. 이동처럼 자리로 걷되, 대기처럼 눈앞의 적을 봅니다 —
    /// 사거리 안에 적이 들면 그 자리에서 교전에 들어가고, 맞으면 사거리를 따지지 않고 반격합니다.
    /// 정면은 목적지가 아니라 지휘관이 준 방향입니다. 선을 따라 옆으로 옮겨도 적을 보고 걷습니다.
    /// </summary>
    public class SquadAdvancingState : SquadState, ICanReceiveOrder
    {
        private const float OrderSettleTime = 0.25f; // MoveTo가 블랙보드만 쓰고 BT가 다음 틱에 경로를 잡는다
        private const float ScanInterval = 0.15f;

        // 자리가 막혀 도착 판정이 영영 안 서는 분대가 있다. 무리가 이만큼 시간 동안 이만큼도 안 움직였으면 선 것으로 본다
        private const float StallInterval = 1f;
        private const float StallDistanceSqr = 0.0025f;

        private float _settleTimer;
        private float _scanTimer;
        private float _stallTimer;
        private Vector3 _stallOrigin;

        public SquadAdvancingState(SquadStateMachine machine) : base(machine) { }

        public override SquadStateType Type => SquadStateType.Advancing;

        public override void Enter()
        {
            FormUpFacing();
            _scanTimer = 0f;
        }

        public override void Tick(float deltaTime)
        {
            _scanTimer -= deltaTime;
            if (_scanTimer <= 0f)
            {
                _scanTimer = ScanInterval;
                if (TryEngageNearby())
                    return;
            }

            _settleTimer -= deltaTime;
            if (_settleTimer > 0f)
                return;

            if (Roster.AllArrived() || Stalled(deltaTime))
                Machine.ChangeState(SquadStateType.Idle);
        }

        private bool Stalled(float deltaTime)
        {
            _stallTimer -= deltaTime;
            if (_stallTimer > 0f)
                return false;

            _stallTimer = StallInterval;
            bool stalled = (Machine.Origin - _stallOrigin).sqrMagnitude < StallDistanceSqr;
            _stallOrigin = Machine.Origin;
            return stalled;
        }

        /// <summary>
        /// 미는 방향 앞에 있는 적만 교전으로 넘깁니다.
        /// 센서는 방향을 보지 않고 가장 가까운 적을 주므로, 그대로 받으면 옆이나 뒤의 적이
        /// 부대를 끌고 갑니다. 선을 그은 방향이 아니라 우연히 가까운 적이 목표가 되는 것입니다 —
        /// 플레이어는 화살표대로 밀라고 명령했는데 엉뚱한 쪽을 치는 것으로 보입니다.
        /// 옆의 적은 자리에 다 서면 Idle이 잡고, 실제로 맞으면 OnDamaged가 반격시킵니다.
        /// </summary>
        private bool TryEngageNearby()
        {
            IDamageable target = Sensor.Target;
            if (!IsEngageable(target))
                return false;

            if (!Roster.AnyInRange(target.Transform.gameObject, Machine.EngageRange))
                return false;

            // 기준은 지금 정면이 아니라 지휘관이 준 방향이다. 정면은 교전하며 흔들린다
            Vector2 toTarget = target.Transform.position - Machine.Origin;
            if (Vector2.Angle(Machine.AdvanceFacing, toTarget) > Machine.ConeAngle * 0.5f)
                return false;

            TryEngageOrTurn(target);
            return true;
        }

        public override void OnMemberLost()
            => FormUpFacing();

        public override void OnFormationChanged()
            => FormUpFacing();

        private void FormUpFacing()
        {
            Facing.SetDesired(Machine.AdvanceFacing);
            Roster.Arrange(Machine.Destination, Facing.DesiredAngle);
            _settleTimer = OrderSettleTime;
            _stallTimer = StallInterval + OrderSettleTime; // 발이 떨어지기 전에 정지로 오판하지 않게 한 박자 늦춘다
            _stallOrigin = Machine.Origin;
        }
    }
}
