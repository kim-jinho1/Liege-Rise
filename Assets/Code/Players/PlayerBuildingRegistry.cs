using System.Collections.Generic;
using Code.Buildings.Base;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Building;

namespace Code.Players
{
    /// <summary>
    /// 플레이어 소속으로 살아있는 건물 목록을 추적하는 싱글톤 레지스트리입니다.
    /// BuildingSpawnEvent/BuildingDeathEvent로 갱신됩니다.
    /// </summary>
    public class PlayerBuildingRegistry : MonoSingleton<PlayerBuildingRegistry>
    {
        private readonly HashSet<AbstractBuilding> _buildings = new();

        /// <summary>
        /// 현재 살아있는 플레이어 건물 목록 (읽기 전용).
        /// </summary>
        public IReadOnlyCollection<AbstractBuilding> Buildings => _buildings;

        protected override void Awake()
        {
            base.Awake();
            Bus<BuildingSpawnEvent>.OnEvent[Owner.Player] += HandleBuildingSpawn;
            Bus<BuildingDeathEvent>.OnEvent[Owner.Player] += HandleBuildingDeath;
        }

        protected override void OnDestroy()
        {
            Bus<BuildingSpawnEvent>.OnEvent[Owner.Player] -= HandleBuildingSpawn;
            Bus<BuildingDeathEvent>.OnEvent[Owner.Player] -= HandleBuildingDeath;
            base.OnDestroy();
        }

        private void HandleBuildingSpawn(BuildingSpawnEvent evt)
            => _buildings.Add(evt.Building);

        private void HandleBuildingDeath(BuildingDeathEvent evt)
            => _buildings.Remove(evt.Building);
    }
}
