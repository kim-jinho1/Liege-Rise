namespace Code.Military.Combat.FSM
{
    /// <summary>
    /// 플레이어 명령(이동·정지·공격)을 받을 수 있는 상태임을 표시합니다.
    /// 선언하지 않은 상태에서는 SquadStateMachine이 명령을 무시합니다.
    /// </summary>
    public interface ICanReceiveOrder
    { }
}
