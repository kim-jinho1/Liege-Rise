using Unity.Behavior;

namespace Code.Units.BT
{
    [BlackboardEnum]
    public enum UnitCommands
    {
        Stop,
        Move,
        Gather,
        ReturnSupplies,
        ConstructBuilding,
        Attack,
        Repair,
        TransportResource,
        MountTower,
        StationedAtBuilding
    }
}
