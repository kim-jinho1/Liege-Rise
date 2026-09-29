namespace Code.Core.Bus
{
    /// <summary>
    /// 게임 오브젝트의 소유자를 나타냅니다. (플레이어/적/중립)
    /// </summary>
    public enum Owner
    {
        Invalid = 0,
        Player = 1,
        Unowned = 5,
        Enemy = 6 // 적
    }
}
