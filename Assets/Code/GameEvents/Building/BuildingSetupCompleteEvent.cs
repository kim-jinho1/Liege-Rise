using Code.Buildings.Base;
using Code.Core.Bus;

namespace Code.GameEvents.Building
{
    public struct BuildingSetupCompleteEvent : IEvent
    {
        public AbstractBuilding Building { get; private set; }
        public Owner Owner { get; private set; }

        public BuildingSetupCompleteEvent(Owner owner, AbstractBuilding building)
        {
            Owner = owner;
            Building = building;
        }
    }
}
