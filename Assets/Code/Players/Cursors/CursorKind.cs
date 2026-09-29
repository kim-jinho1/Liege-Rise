namespace Code.Players.Cursors
{
    /// <summary>
    /// 마우스 아래 상황에 따라 갈아 끼우는 커서 종류입니다.
    /// </summary>
    public enum CursorKind
    {
        Default = 0, // 기본 커서
        Select,
        Build,
        GatherWood,
        GatherStone,
        Attack,
        Invalid
    }
}
