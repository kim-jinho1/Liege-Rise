namespace Code.Buildings.Logic.ProductionBuilding
{
    /// <summary>
    /// 분대 주문이 거절되는 이유입니다. 버튼이 잠긴 이유를 플레이어에게 알릴 때 씁니다.
    /// </summary>
    public enum SquadOrderRefusal
    {
        None,
        UnknownSquad,
        QueueFull,
        PopulationCap,
        NotEnoughSupplies
    }
}
