#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using Code.Buildings.Base;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Building;
using UnityEngine;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;

namespace Code.Navigation.Baking
{
    /// <summary>
    /// 타일맵 기반 맵을 스캔하여 통행 가능 셀에 노드를 만들고 이웃을 연결하는 베이킹 클래스입니다.
    /// </summary>
    public partial class PathBaker : MonoSingleton<PathBaker>
    {
        /// <summary>
        /// 통행 가능한 타일맵 한 장과 그 층 정보입니다.
        /// 레이어를 나누는 행위 자체가 고도 지정이며, 층이 다른 셀은 램프로만 이어집니다.
        /// </summary>
        [Serializable]
        public struct WalkableLayer
        {
            public Tilemap map;     // 통행 가능한 타일맵
            public bool buildable;  // 건설을 허용할지. 다리·램프는 false
            public int level;       // 고도. 클수록 높은 층이고 평지는 0
            public bool isRamp;     // 한 단계 위 층과의 연결 허용. level은 낮은 쪽 층으로 둔다
        }

        [Header("Walkable Base")]
        [SerializeField] private WalkableLayer[] walkableMaps; // 통행 가능 셀

        [Header("Blocking Base")]
        [SerializeField] private Tilemap[] blockingMaps; // 통행 불가 셀

        [Header("Data")]
        [SerializeField] private BakedDataSO bakedData;

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool isDrawGizmo = true;
        [SerializeField] private Color nodeColor = Color.white;
        [SerializeField] private Color edgeColor = Color.red;
#endif

        /// <summary>
        /// PathAgent가 Awake에서 자동으로 참조를 가져가는 프로퍼티입니다.
        /// 플레이 중에는 사본입니다 — 에셋에 대고 다시 구우면 그 자국이 다음 판까지 남습니다.
        /// </summary>
        public BakedDataSO BakedData
            => _runtimeData ??= RuntimeBakedData.ForPlay(bakedData);
        public Tilemap BaseTilemap => PrimaryMap;

        /// <summary>
        /// 좌표 변환·셀 크기 계산의 기준이 되는 타일맵(walkableMaps의 첫 번째).
        /// 모든 walkableMaps는 동일한 Grid를 공유한다고 가정합니다.
        /// </summary>
        private Tilemap PrimaryMap =>
            walkableMaps is { Length: > 0 } ? walkableMaps[0].map : null;

        private readonly BuildingOccupancy _occupancy = new();
        private NavGraphBaker _baker;
        private BakedDataSO _runtimeData;

        /// <summary>
        /// 현재 직렬화 참조로 지연 생성되는 베이커입니다.
        /// </summary>
        private NavGraphBaker Baker
            => _baker ??= new NavGraphBaker(walkableMaps, blockingMaps, BakedData, _occupancy);

        private BusSubscriptionScope _subscriptionScope;

        [ContextMenu("Bake Map (Full)")]
        private void BakeMapData()
        {
            if (PrimaryMap == null || bakedData == null)
                return;

            try
            {
                Baker.BakeFull();
                SaveIfInUnityEditor();
                Debug.Log($"<color=green>Map Baking Complete! Total Nodes: {bakedData.points.Count}</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Baking Failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 런타임 중 특정 영역이 변경되었을 때 해당 영역만 재굽습니다.
        /// </summary>
        public void RebakeMapRuntime(BoundsInt changedArea)
            => Baker.RebakeArea(changedArea);

        /// <summary>
        /// 임의의 콜라이더(자원 등)가 차지하는 셀을 점유 목록에 등록하고 해당 영역을 재굽습니다.
        /// </summary>
        public void RegisterObstacle(Collider2D col)
            => SetObstacle(col, occupied: true);

        /// <summary>
        /// 등록했던 콜라이더 영역을 점유 목록에서 해제하고 해당 영역을 재굽습니다.
        /// </summary>
        public void UnregisterObstacle(Collider2D col)
            => SetObstacle(col, occupied: false);

        private void SetObstacle(Collider2D col, bool occupied)
        {
            if (col == null || PrimaryMap == null)
                return;

            Vector3Int cell = PrimaryMap.WorldToCell(col.bounds.center);
            _occupancy.SetCell(cell, occupied);
            Baker.RebakeArea(new BoundsInt(cell, Vector3Int.one));
        }

        /// <summary>
        /// 건물 footprint 영역 안에 건설 불가 셀이 하나라도 있으면 false를 반환합니다.
        /// </summary>
        public bool CanBuildFootprint(Vector3 worldCenter, Vector2Int size)
            => Baker.CanBuildFootprint(worldCenter, size);

        /// <summary>
        /// 월드 좌표를 건물 크기를 고려하여 가장 가까운 셀 정렬 위치로 스냅합니다.
        /// </summary>
        public Vector3 SnapToGrid(Vector3 worldPos, Vector2Int size)
            => Baker.SnapToGrid(worldPos, size);

        /// <summary>
        /// 건설 시작 시 단일 셀을 즉시 재굽습니다.
        /// </summary>
        public void RebakeAtConstruction(Vector3 worldPos)
        {
            if (PrimaryMap == null)
                return;

            Vector3Int cell = PrimaryMap.WorldToCell(worldPos);
            Baker.RebakeArea(new BoundsInt(cell, Vector3Int.one));
        }

        private void OnEnable()
        {
            _subscriptionScope = new BusSubscriptionScope();
            _subscriptionScope.SubscribeAll<BuildingSpawnEvent>(OnBuildingSpawn);
            _subscriptionScope.SubscribeAll<BuildingDeathEvent>(OnBuildingDeath);
        }

        private void OnDisable()
        {
            _subscriptionScope?.Dispose();
        }

        private void OnBuildingSpawn(BuildingSpawnEvent evt)
        {
            _occupancy.SetBuilding(PrimaryMap, evt.Building, occupied: true);
            RebakeAtBuilding(evt.Building);
        }

        private void OnBuildingDeath(BuildingDeathEvent evt)
        {
            _occupancy.SetBuilding(PrimaryMap, evt.Building, occupied: false);
            RebakeAtBuilding(evt.Building);
        }

        private void RebakeAtBuilding(AbstractBuilding building)
        {
            if (building == null || PrimaryMap == null)
                return;

            Baker.RebakeArea(_occupancy.GetBuildingCellBounds(PrimaryMap, building));
        }

        private void SaveIfInUnityEditor()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(bakedData);
            AssetDatabase.SaveAssets();
#endif
        }
    }
}
