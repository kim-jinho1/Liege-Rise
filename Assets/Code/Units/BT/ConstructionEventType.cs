using Unity.Behavior;

namespace Code.Units.BT
{
    [BlackboardEnum]
    public enum ConstructionEventType
    {
        ArrivedAt,
        Begin,
        Cancel, //취소된거(사망, 등)
        Abort, // 도착했는데 할 수가 없어서 종료된거.
        Completed
    }
}
