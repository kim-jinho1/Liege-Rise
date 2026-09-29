using Code.Commandable.Interfaces;
using Code.Core.Bus;
using UnityEngine;

namespace Code.Military.Combat.FSM
{
    /// <summary>
    /// 명령이 없는 대기 상태입니다. 마지막 대형 그대로 제자리에 머무릅니다.
    /// 자동 반격 판정을 여기서 합니다 — 눈앞의 적은 제자리에서 치고(Tick),
    /// 맞았을 때만 사거리를 따지지 않고 반격에 들어갑니다(OnDamaged).
    /// 후자의 추격 상한은 SquadEngagingState가 앵커 기준 리시로 겁니다.
    /// </summary>
    public class SquadIdleState : SquadState, ICanReceiveOrder
    {
        private const float AutoCheckInterval = 0.15f; // 센서 틱(0.2초)보다 촘촘히 볼 이유가 없다

        // 사거리 밖이라도 이만큼 안의 적에게는 명령 없이 다가선다. 무한정 쫓지 않도록 상한을 둔다
        private const float AutoApproachRange = 7f;

        // 세워 둔 자리에서 이만큼까지만 나간다. 없으면 물러나는 적을 따라 맵 끝까지 끌려간다
        private const float AutoApproachLeash = 8f;

        private const float StepInterval = 0.4f;              // 매 틱 다시 세우면 분대원 BT가 계속 리셋된다
        private const float StepMoveThresholdSqr = 0.5625f;   // 0.75 유닛 — 목표가 이만큼 움직여야 다시 세운다

        private float _checkTimer;
        private float _patrolTimer;
        private float _stepTimer;
        private Vector3 _post;   // 대기를 시작한 자리. 자동 접근의 리시 기준이다
        private Vector3 _lastStepPoint;
        private bool _hasStepped;
        private bool _enemyNearby;

        public SquadIdleState(SquadStateMachine machine) : base(machine) { }

        public override SquadStateType Type => SquadStateType.Idle;

        public override void Enter()
        {
            _checkTimer = 0f;
            _stepTimer = 0f;
            _hasStepped = false;
            _enemyNearby = false;
            _post = Machine.Origin;

            // 같은 순간에 배치된 분대들이 한 몸처럼 움직이지 않도록 첫 발을 흩는다
            _patrolTimer = Random.Range(0f, PatrolInterval);
        }

        public override void Tick(float deltaTime)
        {
            _stepTimer -= deltaTime;

            _checkTimer -= deltaTime;
            if (_checkTimer <= 0f)
            {
                _checkTimer = AutoCheckInterval;
                Scan();
            }

            // 적이 보이는 동안에는 순찰하지 않는다 — 걸어 나가면 등을 내주게 된다
            if (!_enemyNearby)
                TickPatrol(deltaTime);
        }

        private void Scan()
        {
            IDamageable target = Sensor.Target;
            _enemyNearby = IsEngageable(target);

            if (_enemyNearby && Roster.AnyInRange(target.Transform.gameObject, Machine.EngageRange))
            {
                TryEngageOrTurn(target);
                return;
            }

            // 플레이어가 찍어 준 목표가 먼저다. 눈앞의 적은 그 길에 있으면 접근 상태가 알아서 정리한다
            if (ResumeCommandedTarget())
                return;

            if (_enemyNearby)
                StepToward(target);
        }

        /// <summary>
        /// 사거리 밖이지만 코앞인 적에게 다가섭니다. 명령이 없어도 눈앞의 적은 치고 봅니다.
        /// 상태는 Idle 그대로 둡니다 — 순찰과 같은 이유입니다. 발만 옮기므로 매 틱 감지가
        /// 이어지고, 붙는 순간 위의 사거리 판정이 교전으로 넘깁니다.
        /// </summary>
        private void StepToward(IDamageable target)
        {
            // 적은 스스로 나서지 않는다. 나서게 두면 길 옆에 숨겨 둔 매복조를 찾아가 먼저 친다
            if (Machine.Owner != Owner.Player)
                return;

            Vector3 toTarget = target.Transform.position - Machine.Origin;
            if (toTarget.sqrMagnitude > AutoApproachRange * AutoApproachRange)
                return;

            if ((Machine.Origin - _post).sqrMagnitude > AutoApproachLeash * AutoApproachLeash)
                return;

            if (_stepTimer > 0f)
                return;

            _stepTimer = StepInterval;

            Vector3 point = StandoffPoint(target);

            // 목표가 안 움직였는데 다시 세우면 스윙과 경로가 매번 끊긴다. 발이 멈췄으면 다시 부른다
            if (_hasStepped && !Roster.AllArrived()
                && (point - _lastStepPoint).sqrMagnitude < StepMoveThresholdSqr)
                return;

            _lastStepPoint = point;
            _hasStepped = true;
            FormUp(point);
        }

        /// <summary>
        /// 초소 주변을 어슬렁거립니다.
        /// 상태는 Idle 그대로 둡니다. Moving으로 넘기면 그 상태가 센서 목표를 읽지 않아
        /// 순찰하다 적을 그냥 지나칩니다. 여기서는 분대원 발만 옮기므로 매 틱 감지가 계속 돕니다.
        /// </summary>
        private void TickPatrol(float deltaTime)
        {
            if (PatrolRadius <= 0f)
                return;

            _patrolTimer -= deltaTime;
            if (_patrolTimer > 0f)
                return;

            _patrolTimer = PatrolInterval;

            // 기준은 태어난 자리가 아니라 대기를 시작한 자리다. HomePost를 쓰면 행군 중에
            // 한 번 Idle로 떨어진 분대가 스폰 지점으로 되돌아간다 — 공세가 오다 말고 후퇴한다
            Vector2 offset = Random.insideUnitCircle * PatrolRadius;
            FormUp(_post + new Vector3(offset.x, offset.y, 0f));
        }

        private float PatrolRadius
            => Machine.SquadSO == null ? 0f : Machine.SquadSO.PatrolRadius;

        private float PatrolInterval
            => Machine.SquadSO == null ? 6f : Mathf.Max(1f, Machine.SquadSO.PatrolInterval);

        /// <summary>
        /// 제자리에서 대형의 구멍만 메웁니다. 목적지를 바꾸지 않습니다.
        /// </summary>
        public override void OnMemberLost()
            => Roster.Arrange(Machine.Origin, Facing.DesiredAngle);

        /// <summary>
        /// 명령받은 목표가 아직 살아 있으면 다시 접근합니다. 이어받았으면 true입니다.
        /// 교전으로 끊겼던 공격 명령이 여기서 이어집니다 — 정지·이동 명령은 CommandedTarget을
        /// 지우므로 이 경로를 타지 않습니다.
        /// </summary>
        private bool ResumeCommandedTarget()
        {
            IDamageable commanded = Machine.CommandedTarget;
            if (!IsEngageable(commanded))
                return false;

            Machine.ChangeState(SquadStateType.Approaching);
            return true;
        }
    }
}
