using Code.Core.Bus;
using Code.Units.Base;

namespace Code.GameEvents.Unit
{
    public struct UnitDeathEvent : IEvent
    {
        public AbstractUnit Unit { get; private set; }

        public UnitDeathEvent(AbstractUnit unit)
        {
            Unit = unit;
        }
    }
}
