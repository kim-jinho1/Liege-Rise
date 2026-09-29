namespace Code.Military.Squads
{
    /// <summary>
    /// 구성원과 지휘 체계를 추적하기 위한 특정 속성을 갖춘 분대를 나타냅니다.
    /// </summary>
    public interface ISquad
    {
        /// <summary>
        /// 생존 중인 분대원 수입니다.
        /// </summary>
        int AliveCount { get; }

        /// <summary>
        /// 정원 대비 부족한 멤버 수입니다. 보충 비용 산정에 쓰입니다.
        /// </summary>
        int MissingCount { get; }

        /// <summary>
        /// 휘관이 임명되어 있는지 여부입니다.
        /// </summary>
        bool HasCommander { get; }
    }
}
