namespace Code.Military.Combat.FSM
{
    /// <summary>
    /// 분대의 상태를 나타내는 열거형입니다.
    /// 각 상태는 분대가 현재 수행 중인 행동을 나타내며, 이를 통해 분대의 행동 로직을 제어할 수 있습니다.
    /// </summary>
    public enum SquadStateType
    {
        Idle,
        Moving,
        Advancing,
        Approaching,
        Engaging,
        Mounted
    }
}
