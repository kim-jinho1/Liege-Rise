using Code.Commandable.Interfaces;
using Code.Core.Bus;

namespace Code.GameEvents.Unit
{
    public struct UnitDeselectEvent : IEvent
    {
        public ISelectable Unit { get; private set; }

        public UnitDeselectEvent(ISelectable unit)
        {
            Unit = unit;
        }
    }
}
