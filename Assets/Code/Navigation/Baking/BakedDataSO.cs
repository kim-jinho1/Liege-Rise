using System.Collections.Generic;
using Code.Navigation.Graph;
using UnityEngine;

namespace Code.Navigation.Baking
{
    /// <summary>
    /// PathBaker가 구운 네비게이션 그래프 데이터를 저장
    /// </summary>
    [CreateAssetMenu(fileName = "BakedData", menuName = "SO/Path/BakedData", order = 0)]
    public class BakedDataSO : ScriptableObject
    {
        public List<NodeData> points = new();

        private Dictionary<Vector3Int, NodeData> _pointDict;

        // 연결 성분 번호. 그래프가 바뀔 때마다 버려지고 다음 질의에서 다시 채워진다.
        private Dictionary<Vector3Int, int> _regionIds;
        private bool _regionsDirty = true;

        private void OnEnable() => RebuildDict();

        /// <summary>
        /// List 전체를 기반으로 Dictionary를 재구성합니다.
        /// </summary>
        public void RebuildDict()
        {
            _pointDict = new Dictionary<Vector3Int, NodeData>(points.Count);
            foreach (NodeData node in points)
                _pointDict[node.cellPosition] = node;

            MarkRegionsDirty();
        }

        /// <summary>
        /// 그래프가 바뀌었음을 알립니다. 연결 성분은 다음 질의 때 다시 계산됩니다.
        /// </summary>
        public void MarkRegionsDirty() => _regionsDirty = true;

        /// <summary>
        /// 두 셀이 같은 연결 성분에 속하는지 검사합니다.
        /// 성분이 다르면 두 셀 사이에는 어떤 경로도 존재하지 않습니다.
        /// A* 앞에서 이 검사로 걸러야 도달 불가 요청이 전체 그래프 탐색으로 번지지 않습니다.
        /// </summary>
        public bool AreConnected(Vector3Int a, Vector3Int b)
        {
            EnsureRegions();
            return _regionIds.TryGetValue(a, out int regionA)
                   && _regionIds.TryGetValue(b, out int regionB)
                   && regionA == regionB;
        }

        /// <summary>
        /// 연결 성분 번호를 다시 칠합니다. 노드 수에 선형이며, 그래프가 바뀐 뒤 첫 질의에서 한 번만 돕니다.
        /// </summary>
        private void EnsureRegions()
        {
            if (!_regionsDirty && _regionIds != null)
                return;

            _regionIds = new Dictionary<Vector3Int, int>(points.Count);
            Queue<Vector3Int> frontier = new();
            int region = 0;

            foreach (NodeData seed in points)
            {
                if (_regionIds.ContainsKey(seed.cellPosition))
                    continue;

                region++;
                _regionIds[seed.cellPosition] = region;
                frontier.Enqueue(seed.cellPosition);

                while (frontier.Count > 0)
                {
                    if (!TryGetNode(frontier.Dequeue(), out NodeData node) || node.neighbors == null)
                        continue;

                    foreach (LinkData link in node.neighbors)
                    {
                        if (_regionIds.ContainsKey(link.endCellPosition) || !HasNode(link.endCellPosition))
                            continue;

                        _regionIds[link.endCellPosition] = region;
                        frontier.Enqueue(link.endCellPosition);
                    }
                }
            }

            _regionsDirty = false;
        }

        /// <summary>
        /// 저장된 모든 노드를 삭제합니다.
        /// </summary>
        public void ClearPoints()
        {
            points.Clear();
            _pointDict?.Clear();
            MarkRegionsDirty();
        }

        /// <summary>
        /// 새 노드를 추가합니다.
        /// </summary>
        public void AddPoint(Vector3 worldPosition, Vector3Int cellPosition, TopographyType topographyType,
            int level, bool canBuildGeneral)
        {
            NodeData node = new NodeData(worldPosition, cellPosition, topographyType, level, canBuildGeneral);
            points.Add(node);
            _pointDict ??= new Dictionary<Vector3Int, NodeData>();
            _pointDict[cellPosition] = node;
            MarkRegionsDirty();
        }

        /// <summary>
        /// 노드를 제거합니다.
        /// </summary>
        public void RemovePoint(NodeData node)
        {
            points.Remove(node);
            _pointDict?.Remove(node.cellPosition);
            MarkRegionsDirty();
        }

        public bool HasNode(Vector3Int cellPosition)
            => _pointDict != null && _pointDict.ContainsKey(cellPosition);

        public bool TryGetNode(Vector3Int cellPosition, out NodeData nodeData)
        {
            if (_pointDict != null && _pointDict.TryGetValue(cellPosition, out nodeData))
                return true;
            nodeData = default;
            return false;
        }
    }
}
