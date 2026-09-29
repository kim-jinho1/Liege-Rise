using System.Collections.Generic;
using Code.Navigation.Baking;
using Code.Navigation.Graph;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Navigation.Pathfinding
{
    /// <summary>
    /// A* 알고리즘과 경로 최적화(Line-of-Sight)를 담당하는 최적화된 엔진.
    /// 모든 PathAgent가 이 엔진을 공유하여 메모리 사용량을 줄입니다.
    /// </summary>
    public static class PathfindingEngine
    {
        private static readonly Stack<Node> NodePool = new(128);
        private static readonly List<Node> RentedNodes = new(128);
        private static readonly HashSet<Vector3Int> ClosedSet = new(128);
        private static readonly List<Node> PathResult = new(128);
        private static readonly Dictionary<Vector3Int, float> BestG = new(128);

        private static readonly PriorityQueue<Node> OpenList = new();

        /// <summary>
        /// A* 알고리즘을 실행하여 월드 좌표 경로를 반환합니다.
        /// </summary>
        public static int CalculatePath(Vector3 startPos, Vector3 endPos, BakedDataSO bakedData, Tilemap tilemap,
            float agentRadius, Vector3[] pathBuffer)
        {
            Vector3Int startCell = tilemap.WorldToCell(startPos);
            Vector3Int endCell = tilemap.WorldToCell(endPos);

            if (!bakedData.TryGetNode(startCell, out NodeData startData))
                startCell = FindNearestValidCell(startCell, bakedData);

            if (!bakedData.TryGetNode(endCell, out _))
                endCell = FindNearestValidCell(endCell, bakedData);

            if (!bakedData.TryGetNode(startCell, out startData) || !bakedData.TryGetNode(endCell, out NodeData endData))
                return 0;

            // 성분이 다르면 경로가 없다. 이 검사를 건너뛰면 도달 불가 요청이
            // 열린 목록이 빌 때까지 그래프 전체를 펼치는 최악 탐색이 된다.
            if (!bakedData.AreConnected(startCell, endCell))
                return 0;

            lock (NodePool) // 스레드 안전성 확보 (필요 시)
            {
                RentedNodes.Clear();
                ClosedSet.Clear();
                PathResult.Clear();
                BestG.Clear();
                OpenList.Clear();

                Node startNode = RentNode();
                startNode.NodeData = startData;
                startNode.CellPosition = startData.cellPosition;
                startNode.WorldPosition = startData.worldPosition;
                startNode.G = 0f;
                startNode.F = Vector3.Distance(startData.cellPosition, endData.cellPosition);
                BestG[startNode.CellPosition] = 0f;
                OpenList.Push(startNode);

                Node destinationNode = null;

                while (OpenList.Count > 0)
                {
                    Node current = OpenList.Pop();

                    if (current.CellPosition == endData.cellPosition)
                    {
                        destinationNode = current;
                        break;
                    }

                    if (!ClosedSet.Add(current.CellPosition))
                        continue;

                    foreach (LinkData link in current.NodeData.neighbors)
                    {
                        if (ClosedSet.Contains(link.endCellPosition))
                            continue;

                        if (!bakedData.TryGetNode(link.endCellPosition, out NodeData nextData))
                            continue;

                        float newG = current.G + link.cost;
                        if (BestG.TryGetValue(link.endCellPosition, out float recordedG) && newG >= recordedG)
                            continue;

                        BestG[link.endCellPosition] = newG;

                        Node next = RentNode();
                        next.NodeData = nextData;
                        next.CellPosition = nextData.cellPosition;
                        next.WorldPosition = nextData.worldPosition;
                        next.ParentNode = current;
                        next.G = newG;
                        next.F = newG + Vector3.Distance(link.endCellPosition, endData.cellPosition);
                        OpenList.Push(next);
                    }
                }

                if (destinationNode != null)
                {
                    Node temp = destinationNode;
                    while (temp != null)
                    {
                        PathResult.Add(temp);
                        temp = temp.ParentNode;
                    }

                    PathResult.Reverse();

                    int count = ExtractWaypoints(PathResult, bakedData, tilemap, agentRadius, pathBuffer);

                    // 노드 좌표는 전부 셀 중심이라 요청 좌표는 경로 어디에도 들어가지 않는다.
                    // 요청 지점이 마지막 셀 안일 때만 끝점을 바꾼다. 그래야 도착 위치가 제각각이 아니다
                    if (count > 0 && tilemap.WorldToCell(endPos) == destinationNode.CellPosition)
                        pathBuffer[count - 1] = new Vector3(endPos.x, endPos.y, 0f);

                    ReturnAllNodes();
                    return count;
                }

                ReturnAllNodes();
                return 0;
            }
        }

        private static int ExtractWaypoints(List<Node> path, BakedDataSO bakedData, Tilemap tilemap, float radius, Vector3[] buffer)
        {
            if (path.Count == 0)
                return 0;

            int idx = 0;
            buffer[idx++] = path[0].WorldPosition;
            int current = 0;

            while (current < path.Count - 1)
            {
                if (idx >= buffer.Length - 1)
                    break;

                // 보이는 가장 먼 노드를 이진 탐색으로 찾는다. 순서대로 훑으면 탁 트인 곳에서 노드 수의 제곱만큼
                // 시야 검사가 돌아 — 검사 하나가 두 점 사이를 0.4칸 간격 세 줄로 훑는다 — 큰 맵에서 한 건이 수 ms였다.
                // 바로 다음 노드는 이웃이라 늘 보인다고 두고, 끝까지 보이면 검사 한 번으로 끝난다.
                int baseLevel = path[current].NodeData.level;
                Vector3 from = path[current].WorldPosition;
                int visible = current + 1;
                int blocked;

                if (HasLineOfSight(from, path[^1].WorldPosition, radius, bakedData, tilemap, baseLevel))
                {
                    visible = path.Count - 1;
                }
                else
                {
                    blocked = path.Count - 1;
                    while (blocked - visible > 1)
                    {
                        int mid = (visible + blocked) / 2;
                        if (HasLineOfSight(from, path[mid].WorldPosition, radius, bakedData, tilemap, baseLevel))
                            visible = mid;
                        else
                            blocked = mid;
                    }
                }

                buffer[idx++] = path[visible].WorldPosition;
                current = visible;
            }

            return idx;
        }

        /// <summary>
        /// 두 지점 사이가 뚫려 있는지 검사합니다.
        /// baseLevel과 다른 층의 셀을 지나면 시야가 끊깁니다 — 절벽을 가로지르는 직선 압축을 막습니다.
        /// </summary>
        public static bool HasLineOfSight(Vector3 from, Vector3 to, float radius, BakedDataSO bakedData,
            Tilemap tilemap, int baseLevel)
        {
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            if (dist < 0.001f)
                return true;

            Vector3 perpendicular = new Vector3(-dir.y, dir.x, 0f).normalized * radius;

            return CheckLineClear(from, to, dist, bakedData, tilemap, baseLevel)
                   && CheckLineClear(from + perpendicular, to + perpendicular, dist, bakedData, tilemap, baseLevel)
                   && CheckLineClear(from - perpendicular, to - perpendicular, dist, bakedData, tilemap, baseLevel);
        }

        /// <summary>
        /// 곧장 걸어갈 수 있는지입니다. 두 점이 같은 층이고 그 사이가 뚫려 있어야 합니다.
        /// A*를 돌리기 전에 이것부터 묻습니다. 실제 이동의 대부분은 대형 자리로 몇 걸음 옮기는 것이고,
        /// 그 계산이 분대원 수만큼 곱해져 프레임을 먹습니다.
        /// </summary>
        public static bool CanWalkStraight(Vector3 from, Vector3 to, float agentRadius, BakedDataSO bakedData,
            Tilemap tilemap)
        {
            if (bakedData == null || tilemap == null)
                return false;

            if (!bakedData.TryGetNode(tilemap.WorldToCell(from), out NodeData start)
                || !bakedData.TryGetNode(tilemap.WorldToCell(to), out NodeData end))
                return false;

            return start.level == end.level
                   && HasLineOfSight(from, to, agentRadius, bakedData, tilemap, start.level);
        }

        private static bool CheckLineClear(Vector3 from, Vector3 to, float dist, BakedDataSO bakedData,
            Tilemap tilemap, int baseLevel)
        {
            float step = tilemap.cellSize.x * 0.4f;
            int steps = Mathf.CeilToInt(dist / step);
            for (int i = 1; i <= steps; i++)
            {
                Vector3 sample = Vector3.Lerp(from, to, (float)i / steps);
                Vector3Int cell = tilemap.WorldToCell(sample);
                if (!bakedData.TryGetNode(cell, out NodeData node) || node.level != baseLevel)
                    return false;
            }

            return true;
        }

        public static Vector3Int FindNearestValidCell(Vector3Int origin, BakedDataSO bakedData)
        {
            for (int i = 1; i <= 4; i++)
            {
                for (int dx = -i; dx <= i; dx++)
                {
                    for (int dy = -i; dy <= i; dy++)
                    {
                        if (Mathf.Abs(dx) != i && Mathf.Abs(dy) != i)
                            continue;

                        Vector3Int candidate = origin + new Vector3Int(dx, dy, 0);
                        if (bakedData.TryGetNode(candidate, out _))
                            return candidate;
                    }
                }
            }

            return origin;
        }

        private static Node RentNode()
        {
            Node n = NodePool.Count > 0 ? NodePool.Pop() : new Node();
            n.G = 0f;
            n.F = 0f;
            n.ParentNode = null;
            RentedNodes.Add(n);
            return n;
        }

        private static void ReturnAllNodes()
        {
            foreach (Node n in RentedNodes)
                NodePool.Push(n);
            RentedNodes.Clear();
        }
    }
}
