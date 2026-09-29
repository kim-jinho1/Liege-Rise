using Code.Buildings.Base;
using Code.Core.Bus;

namespace Code.GameEvents.Building
{
    public struct BuildingDeathEvent : IEvent
    {
        public Owner Owner { get; private set; }
        public AbstractBuilding Building { get; private set; }

        public BuildingDeathEvent(Owner owner, AbstractBuilding building)
        {
            Owner = owner;
            Building = building;
        }
    }
}
