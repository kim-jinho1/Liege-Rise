using System.Collections.Generic;
using Code.Buildings.Base;
using Code.Buildings.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Navigation.Baking
{
    /// <summary>
    /// 건물 점유 상태를 관리하는 클래스입니다.
    /// </summary>
    public sealed class BuildingOccupancy
    {
        private readonly HashSet<Vector3Int> _cells = new();

        /// <summary>
        /// 해당 셀이 점유 중인지 여부를 반환합니다.
        /// </summary>
        public bool IsOccupied(Vector3Int cell)
            => _cells.Contains(cell);

        /// <summary>
        /// 단일 셀의 점유 상태를 설정합니다.
        /// </summary>
        public void SetCell(Vector3Int cell, bool occupied)
        {
            if (occupied)
                _cells.Add(cell);
            else
                _cells.Remove(cell);
        }

        /// <summary>
        /// 건물이 차지하는 모든 셀의 점유 상태를 일괄 설정합니다.
        /// </summary>
        public void SetBuilding(Tilemap map, AbstractBuilding building, bool occupied)
        {
            if (building == null || map == null)
                return;

            BoundsInt bounds = GetBuildingCellBounds(map, building);
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            for (int y = bounds.yMin; y < bounds.yMax; y++)
                SetCell(new Vector3Int(x, y, 0), occupied);
        }

        /// <summary>
        /// 건물이 차지하는 셀 영역을 계산합니다.
        /// 배치 시스템이 BuildingSO.Size + SnapToGrid로 격자 정렬하므로 동일하게 Size 기준으로 계산합니다.
        /// </summary>
        public BoundsInt GetBuildingCellBounds(Tilemap map, AbstractBuilding building)
        {
            // 셀 계산은 BuildingFootprint 하나만 쥔다 — 여기와 표시가 따로 계산하면 그림이 어긋난다
            if (building.BuildingSO != null && building.BuildingSO.Size is { x: > 0, y: > 0 })
                return BuildingFootprint.CellBounds(map, building.transform.position, building.BuildingSO.Size);

            if (building.TryGetComponent(out Collider2D col))
                return GetColliderCellBounds(map, col.bounds);

            Vector3Int cell = map.WorldToCell(building.transform.position);
            return new BoundsInt(cell, Vector3Int.one);
        }

        /// <summary>
        /// 콜라이더의 월드 경계를 차지하는 셀 영역(BoundsInt)으로 변환합니다.
        /// </summary>
        private static BoundsInt GetColliderCellBounds(Tilemap map, Bounds worldBounds)
        {
            const float eps = 0.01f;
            Vector3Int min = map.WorldToCell(new Vector3(worldBounds.min.x + eps, worldBounds.min.y + eps, 0f));
            Vector3Int max = map.WorldToCell(new Vector3(worldBounds.max.x - eps, worldBounds.max.y - eps, 0f));
            Vector3Int size = max - min + Vector3Int.one;
            return new BoundsInt(min, size);
        }
    }
}
