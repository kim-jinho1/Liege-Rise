namespace Code.Buildings.Interfaces
{
    /// <summary>
    /// 폰 인구 한도를 공급하는 건물 데이터입니다.
    /// 한도만 공급하며 폰을 실제로 스폰하는지는 관여하지 않습니다.
    /// </summary>
    public interface IPawnCapacityProvider
    {
        /// <summary>
        /// 이 건물이 늘려주는 폰 인구 한도입니다.
        /// </summary>
        int PawnCapacity { get; }
    }
}
