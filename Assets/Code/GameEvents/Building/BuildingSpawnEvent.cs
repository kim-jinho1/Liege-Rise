using Code.Buildings.Base;
using Code.Core.Bus;

namespace Code.GameEvents.Building
{
    public struct BuildingSpawnEvent : IEvent
    {
        public AbstractBuilding Building { get; private set; }
        public Owner Owner { get; private set; }

        public BuildingSpawnEvent(Owner owner, AbstractBuilding building)
        {
            Owner = owner;
            Building = building;
        }
    }
}
