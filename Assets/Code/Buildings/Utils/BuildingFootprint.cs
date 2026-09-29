using Code.Navigation.Baking;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Buildings.Utils
{
    /// <summary>
    /// 건물 발자국(Building Footprint) 관련 유틸리티 클래스입니다.
    /// </summary>
    public static class BuildingFootprint
    {
        /// <summary>
        /// PathBaker 베이스 타일맵의 셀 크기입니다.
        /// </summary>
        public static Vector2 CellSize
        {
            get
            {
                Tilemap map = PathBaker.Instance != null ? PathBaker.Instance.BaseTilemap : null;
                return map != null ? map.cellSize : Vector2.one;
            }
        }

        /// <summary>
        /// 셀 단위 크기를 월드 단위 발자국 크기로 환산합니다.
        /// </summary>
        public static Vector2 WorldSize(Vector2Int size)
        {
            Vector2 cell = CellSize;
            return new Vector2(size.x * cell.x, size.y * cell.y);
        }

        /// <summary>
        /// 발자국의 절반 크기(중심에서 가장자리까지)를 반환합니다.
        /// </summary>
        public static Vector2 HalfExtents(Vector2Int size)
            => WorldSize(size) * 0.5f;

        /// <summary>
        /// 점유·표시가 함께 보는 기준 타일맵입니다. 없으면 null입니다.
        /// </summary>
        public static Tilemap BaseMap
            => PathBaker.Instance != null ? PathBaker.Instance.BaseTilemap : null;

        /// <summary>
        /// 건물이 실제로 먹는 셀 영역입니다.
        /// 점유(Code.Navigation.Baking.BuildingOccupancy)와 화면 표시가 이 하나를 공유해야
        /// 합니다. 표시가 transform.position을 그대로 중심으로 그리면, 건물이 칸 중앙에 정확히
        /// 서 있지 않을 때 실제 점유와 최대 한 칸까지 어긋납니다.
        /// </summary>
        public static BoundsInt CellBounds(Tilemap map, Vector3 position, Vector2Int size)
        {
            Vector3 cell = map.cellSize;
            Vector3 origin = position - new Vector3(
                (size.x - 1) * 0.5f * cell.x,
                (size.y - 1) * 0.5f * cell.y, 0f);

            Vector3Int baseCell = map.WorldToCell(origin);
            return new BoundsInt(baseCell.x, baseCell.y, 0, size.x, size.y, 1);
        }

        /// <summary>
        /// 셀 영역의 월드 좌표 중심입니다. 표시를 그 칸들 위에 정확히 얹을 때 씁니다.
        /// </summary>
        public static Vector3 CellBoundsCenter(Tilemap map, BoundsInt bounds)
        {
            Vector3 min = map.CellToWorld(new Vector3Int(bounds.xMin, bounds.yMin, 0));
            Vector3 max = map.CellToWorld(new Vector3Int(bounds.xMax, bounds.yMax, 0));
            return (min + max) * 0.5f;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 건물 발자국을 반투명 채움 큐브 + 와이어 큐브 기즈모로 그립니다.
        /// </summary>
        public static void DrawGizmo(Vector3 center, Vector2Int size, Color color)
        {
            Vector2 footprint = WorldSize(size);
            Gizmos.color = new Color(color.r, color.g, color.b, 0.12f);
            Gizmos.DrawCube(center, footprint);
            Gizmos.color = new Color(color.r, color.g, color.b, 0.9f);
            Gizmos.DrawWireCube(center, footprint);
        }
#endif
    }
}
