using Code.Core.Bus;

namespace Code.GameEvents.Economy
{
    /// <summary>
    /// 인구 수가 변경될 때 발생하는 이벤트입니다.
    /// 델타는 클램프 후 실제로 바뀐 양입니다 — 0에서 더 깎이면 요청량과 다릅니다.
    /// </summary>
    public struct PopulationChangedEvent : IEvent
    {
        public Owner Owner { get; private set; }
        public int PawnCount { get; private set; }
        public int PawnCapacity { get; private set; }
        public int SquadCount { get; private set; }
        public int SquadCapacity { get; private set; }
        public int PawnDelta { get; private set; }
        public int SquadDelta { get; private set; }

        public PopulationChangedEvent(Owner owner, int pawnCount, int pawnCapacity, int squadCount, int squadCapacity,
            int pawnDelta = 0, int squadDelta = 0)
        {
            Owner = owner;
            PawnCount = pawnCount;
            PawnCapacity = pawnCapacity;
            SquadCount = squadCount;
            SquadCapacity = squadCapacity;
            PawnDelta = pawnDelta;
            SquadDelta = squadDelta;
        }
    }
}
