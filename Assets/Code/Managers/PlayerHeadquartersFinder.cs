using Code.Buildings.Logic.CoreBuilding;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Building;
using UnityEngine;

namespace Code.Managers
{
    /// <summary>
    /// 플레이어 본성(HeadquartersBuilding)을 런타임에 찾아 finder에 연결합니다.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class PlayerHeadquartersFinder : MonoBehaviour
    {
        [SerializeField] private CommandableFinderSO finderSO;

        private void Awake()
        {
            Debug.Assert(finderSO, $"[{this}]: finderSO가 할당되지 않았습니다.");

            finderSO.Set(null); // 이전 씬/플레이 세션에서 남은 SO 에셋 참조를 초기화합니다.
            Bus<BuildingSpawnEvent>.OnEvent[Owner.Player] += HandlePlayerBuildingSpawn;
        }

        private void OnDestroy()
            => Bus<BuildingSpawnEvent>.OnEvent[Owner.Player] -= HandlePlayerBuildingSpawn;

        /// <summary>
        /// 플레이어의 첫 본성이 스폰되면 finder에 등록하고 구독을 해제합니다.
        /// </summary>
        private void HandlePlayerBuildingSpawn(BuildingSpawnEvent evt)
        {
            if (evt.Building is not HeadquartersBuilding castle)
                return;

            finderSO.Set(castle);
            Bus<BuildingSpawnEvent>.OnEvent[Owner.Player] -= HandlePlayerBuildingSpawn;
        }
    }
}
