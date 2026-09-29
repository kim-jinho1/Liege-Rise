namespace Code.Military.Combat.FSM
{
    /// <summary>
    /// 플레이어 이동 명령을 수행하는 상태입니다. 이 상태에서는 센서 목표를 읽지 않으므로
    /// 자동 교전이 자연히 억제됩니다(구 HasPlayerOrder 플래그의 자리).
    /// </summary>
    public class SquadMovingState : SquadState, ICanReceiveOrder
    {
        // 멤버 MoveTo는 블랙보드만 쓰고 BT가 다음 틱에 SetDestination을 부른다.
        // 그 사이 RemainingDistance가 직전 값(정지 상태면 0)이라 AllArrived()가 즉시 true다.
        private const float OrderSettleTime = 0.25f;

        private float _settleTimer;

        public SquadMovingState(SquadStateMachine machine) : base(machine) { }

        public override SquadStateType Type => SquadStateType.Moving;

        public override void Enter()
        {
            FormUp(Machine.Destination);
            _settleTimer = OrderSettleTime;
        }

        public override void Tick(float deltaTime)
        {
            _settleTimer -= deltaTime;
            if (_settleTimer > 0f)
                return;

            if (Roster.AllArrived())
                Machine.ChangeState(SquadStateType.Idle);
        }

        /// <summary>
        /// 남은 인원으로 대형을 다시 짜고 정착 유예를 걸어줍니다.
        /// </summary>
        public override void OnMemberLost()
        {
            FormUp(Machine.Destination);
            _settleTimer = OrderSettleTime;
        }

        /// <summary>
        /// 행군 중 대형을 바꾸면 목적지 기준으로 다시 세웁니다. 선 자리가 아니라 가던 곳입니다.
        /// </summary>
        public override void OnFormationChanged()
            => OnMemberLost();
    }
}
