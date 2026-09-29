using Code.Core.Bus;
using Code.Units.Base;

namespace Code.GameEvents.Unit
{
    public struct UnitSpawnEvent : IEvent
    {
        public Owner Owner { get; private set; }
        public AbstractUnit Unit { get; private set; }

        public UnitSpawnEvent(Owner owner, AbstractUnit unit)
        {
            Owner = owner;
            Unit = unit;
        }
    }
}
