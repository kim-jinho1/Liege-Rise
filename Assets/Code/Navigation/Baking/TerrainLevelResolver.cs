using Code.Navigation.Graph;
using UnityEngine;

namespace Code.Navigation.Baking
{
    /// <summary>
    /// 통행 가능 타일맵 여러 장에서 셀 하나의 고도와 지형 성격을 판정하고,
    /// 두 노드를 이웃으로 연결해도 되는지 결정합니다.
    /// </summary>
    public sealed class TerrainLevelResolver
    {
        private readonly PathBaker.WalkableLayer[] _layers;

        public TerrainLevelResolver(PathBaker.WalkableLayer[] layers) => _layers = layers;

        /// <summary>
        /// 셀을 덮고 있는 레이어 중 가장 높은 고도와 그 지형 성격을 반환합니다.
        /// 같은 고도에 램프 레이어가 겹쳐 있으면 램프로 판정합니다.
        /// </summary>
        public void Resolve(Vector3Int cell, out int level, out TopographyType topography)
        {
            level = 0;
            topography = TopographyType.None;

            if (_layers == null)
                return;

            bool found = false;
            foreach (PathBaker.WalkableLayer layer in _layers)
            {
                if (layer.map == null || !layer.map.HasTile(cell))
                    continue;

                if (!found || layer.level > level)
                {
                    level = layer.level;
                    topography = layer.isRamp ? TopographyType.Ramp : TopographyType.Flat;
                    found = true;
                    continue;
                }

                if (layer.level == level && layer.isRamp)
                    topography = TopographyType.Ramp;
            }
        }

        /// <summary>
        /// 두 노드를 이웃으로 연결할 수 있는지 판정합니다.
        /// 같은 층이면 항상, 한 층 차이면 어느 한쪽이 램프일 때만 연결합니다.
        /// </summary>
        public static bool CanLink(in NodeData a, in NodeData b)
        {
            int gap = Mathf.Abs(a.level - b.level);
            if (gap == 0)
                return true;
            if (gap > 1)
                return false;

            return a.topographyType == TopographyType.Ramp
                   || b.topographyType == TopographyType.Ramp;
        }

        /// <summary>
        /// 층간 연결을 하나도 만들지 못한 램프 셀을 경고로 알립니다.
        /// 램프를 잘못된 층 레이어에 그렸거나 절벽으로 둘러싸인 경우를 잡습니다.
        /// </summary>
        public static void WarnIsolatedRamps(BakedDataSO bakedData)
        {
            if (bakedData == null || bakedData.points == null)
                return;

            int isolated = 0;
            Vector3Int sample = Vector3Int.zero;

            foreach (NodeData node in bakedData.points)
            {
                if (node.topographyType != TopographyType.Ramp || Bridges(bakedData, node))
                    continue;

                if (isolated++ == 0)
                    sample = node.cellPosition;
            }

            if (isolated > 0)
                Debug.LogWarning($"<color=yellow>[NavGraphBaker]</color> 층간 연결이 없는 램프 셀 {isolated}개. " +
                                 $"예: {sample} — 램프 레이어의 level이 '낮은 쪽' 층과 같은지 확인하세요.");
        }

        /// <summary>
        /// 이 노드가 다른 층으로 넘어가는 링크를 하나라도 가지고 있는지 확인합니다.
        /// </summary>
        private static bool Bridges(BakedDataSO bakedData, in NodeData node)
        {
            foreach (LinkData link in node.neighbors)
            {
                if (bakedData.TryGetNode(link.endCellPosition, out NodeData other) && other.level != node.level)
                    return true;
            }

            return false;
        }
    }
}
