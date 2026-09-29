namespace Code.Economics
{
    /// <summary>
    /// 일꾼이 운반한 자원을 수령할 수 있는 대상(가공/생산 건물 등)의 공통 계약입니다.
    /// </summary>
    public interface IResourceReceiver
    {
        /// <summary>
        /// 이 대상이 해당 자원을 수령하는지 여부를 반환합니다.
        /// </summary>
        bool AcceptsResource(ResourceSO resource);

        /// <summary>
        /// 자원을 수령하고 실제로 받은 양을 반환합니다. (수령하지 못한 잔량 = amount - 반환값)
        /// </summary>
        int DeliverResources(ResourceSO resource, int amount);
    }
}
