using Code.Buildings.Interfaces;
using Code.Commandable.Interfaces;
using UnityEngine;

namespace Code.Military.Combat.FSM
{
    /// <summary>
    /// 지정 공격 명령 전용 상태입니다. 명령받은 목표에 대형째 접근합니다.
    /// Idle의 자동 반격은 이 상태를 거치지 않습니다 — 가만히 있다가 반격만 하지 추격하지 않기 때문입니다.
    /// 단 건물을 치러 가는 중에는 감지된 적을 먼저 정리합니다(PickTarget 참고).
    /// </summary>
    public class SquadApproachingState : SquadState, ICanReceiveOrder
    {
        private const float RepathInterval = 0.4f; // 매 프레임 재배치하면 멤버마다 A*가 매 프레임 돈다

        // 목표 지점이 이만큼도 안 움직였으면 재배치를 건너뛴다. 건물을 치러 갈 때가 이 경우다.
        private const float RepathMoveThresholdSqr = 0.5625f; // 0.75 유닛

        // 건물을 치러 가다 갈아타는 판정 거리. 교전 여유의 이 배수 안에 든 적만 길을 막은 것으로 본다
        private const float BlockingFactor = 2f;

        private float _repathTimer;
        private Vector3 _lastFormUpPoint;
        private bool _hasFormedUp;

        public SquadApproachingState(SquadStateMachine machine) : base(machine) { }

        public override SquadStateType Type => SquadStateType.Approaching;

        public override void Enter()
        {
            _repathTimer = 0f;
            _hasFormedUp = false;
        }

        public override void Tick(float deltaTime)
        {
            IDamageable commanded = Machine.CommandedTarget;

            if (!IsEngageable(commanded))
            {
                Machine.ChangeState(SquadStateType.Idle);
                return;
            }

            IDamageable target = PickTarget(commanded);

            if (Roster.AnyInRange(target.Transform.gameObject, Machine.EngageRange))
            {
                TryEngageOrTurn(target); // 붙었으면 더 이동하지 않고 콘만 맞춘다
                return;
            }

            _repathTimer -= deltaTime;
            if (_repathTimer > 0f)
                return;

            _repathTimer = RepathInterval;

            Vector3 point = StandoffPoint(target);

            // 목표 지점이 안 움직였어도 발이 멈췄으면 다시 보낸다. MoveTo는 한 번짜리 명령이라,
            // 길이 막혀 멈춘 분대원을 아무도 다시 부르지 않으면 그 자리에 선 채로 남는다
            bool stalled = Roster.AllArrived();

            if (_hasFormedUp && !stalled
                && (point - _lastFormUpPoint).sqrMagnitude < RepathMoveThresholdSqr)
                return;

            _lastFormUpPoint = point;
            _hasFormedUp = true;
            FormUp(point);
        }

        public override void OnMemberLost()
        {
            _repathTimer = 0f;   // 다음 틱에 재배치
            _hasFormedUp = false; // 대형이 바뀌었으니 문턱을 건너뛴다
        }

        /// <summary>
        /// 접근 중 대형을 바꾸면 다음 틱에 정지 지점 기준으로 다시 세웁니다.
        /// </summary>
        public override void OnFormationChanged()
            => OnMemberLost();

        /// <summary>
        /// 실제로 향할 목표입니다. 건물을 치러 가는 도중에 적을 만나면 그쪽을 먼저 정리합니다.
        /// 명령 목표는 CommandedTarget에 그대로 남아 있어, 길이 트이면 Idle이 다시 접근시킵니다.
        /// </summary>
        private IDamageable PickTarget(IDamageable commanded)
        {
            IDamageable spotted = Sensor.Target;
            if (spotted == commanded || !IsEngageable(spotted))
                return commanded;

            // 팔 닿는 곳까지 붙은 적은 무엇을 쫓던 중이든 먼저 친다.
            //
            // 이게 없으면 쫓기만 하는 행렬이 생긴다: A를 쫓는 B를 C가 쫓고 그 C를 D가 쫓는데,
            // 다들 걸음이 같아 거리가 줄지 않으므로 아무도 사거리에 못 든다. 옆에 붙은 적을
            // 무시하고 계속 걷기 때문이다. 붙는 순간 멈춰 서면 그 자리에서 줄이 끊긴다
            if (Roster.AnyInRange(spotted.Transform.gameObject, Machine.EngageRange))
                return spotted;

            if (commanded is not IBuilding)
                return commanded;

            // 길을 막고 선 적만 먼저 친다. 감지됐다는 것만으로 갈아타면, 찍어 준 건물을 두고
            // 저 멀리 보이는 딴 놈에게 달려간다 — 플레이어 눈에는 명령을 무시한 것이다
            if (!Roster.AnyInRange(spotted.Transform.gameObject, Machine.EngageRange * BlockingFactor))
                return commanded;

            return spotted;
        }
    }
}
