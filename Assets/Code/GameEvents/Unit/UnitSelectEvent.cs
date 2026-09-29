using Code.Commandable.Interfaces;
using Code.Core.Bus;

namespace Code.GameEvents.Unit
{
    public struct UnitSelectEvent : IEvent
    {
        public ISelectable Unit { get; private set; }

        public UnitSelectEvent(ISelectable unit)
        {
            Unit = unit;
        }
    }
}
