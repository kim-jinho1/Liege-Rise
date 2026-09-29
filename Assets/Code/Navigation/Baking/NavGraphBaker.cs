using Code.Navigation.Graph;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Navigation.Baking
{
    /// <summary>
    /// 타일맵 기반 맵을 스캔하여 통행 가능 셀에 노드를 만들고 이웃을 연결하는 베이킹 클래스입니다.
    /// </summary>
    public sealed class NavGraphBaker
    {
        private static readonly Vector2Int[] RectDirections =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
        };

        private readonly PathBaker.WalkableLayer[] _walkableMaps;
        private readonly Tilemap[] _blockingMaps;
        private readonly BakedDataSO _bakedData;
        private readonly BuildingOccupancy _occupancy;
        private readonly TerrainLevelResolver _resolver;

        public NavGraphBaker(PathBaker.WalkableLayer[] walkableMaps, Tilemap[] blockingMaps,
            BakedDataSO bakedData, BuildingOccupancy occupancy)
        {
            _walkableMaps = walkableMaps;
            _blockingMaps = blockingMaps;
            _bakedData = bakedData;
            _occupancy = occupancy;
            _resolver = new TerrainLevelResolver(walkableMaps);
        }

        /// <summary>
        /// 좌표 변환·셀 크기 계산의 기준이 되는 타일맵(walkableMaps의 첫 번째).
        /// </summary>
        private Tilemap PrimaryMap =>
            _walkableMaps is { Length: > 0 } ? _walkableMaps[0].map : null;

        /// <summary>
        /// 전체 맵을 베이크합니다: 통행 가능 셀에 노드를 만들고 이웃을 연결합니다.
        /// </summary>
        public void BakeFull()
        {
            _bakedData.ClearPoints();

            BoundsInt bounds = CalculateWalkableBounds();

            for (int x = bounds.xMin; x < bounds.xMax; x++)
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y);
                    if (CanMovePosition(pos))
                        AddPoint(pos);
                }

            foreach (NodeData node in _bakedData.points)
                LinkNeighbors(node);

            TerrainLevelResolver.WarnIsolatedRamps(_bakedData);
        }

        /// <summary>
        /// 지정된 영역을 재베이크합니다.
        /// 해당 영역과 주변 셀의 노드를 제거하고 다시 생성한 뒤 이웃을 연결합니다.
        /// </summary>
        public void RebakeArea(BoundsInt changedArea)
        {
            BoundsInt affectedArea = new BoundsInt(
                changedArea.min - Vector3Int.one,
                changedArea.size + Vector3Int.one * 2);

            for (int x = affectedArea.xMin; x < affectedArea.xMax; x++)
                for (int y = affectedArea.yMin; y < affectedArea.yMax; y++)
                    if (_bakedData.TryGetNode(new Vector3Int(x, y, 0), out NodeData node))
                        _bakedData.RemovePoint(node);

            for (int x = affectedArea.xMin; x < affectedArea.xMax; x++)
                for (int y = affectedArea.yMin; y < affectedArea.yMax; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    if (CanMovePosition(pos))
                        AddPoint(pos);
                }

            BoundsInt relinkArea = new BoundsInt(
                affectedArea.min - Vector3Int.one,
                affectedArea.size + Vector3Int.one * 2);
            for (int x = relinkArea.xMin; x < relinkArea.xMax; x++)
                for (int y = relinkArea.yMin; y < relinkArea.yMax; y++)
                    if (_bakedData.TryGetNode(new Vector3Int(x, y, 0), out NodeData node))
                        LinkNeighbors(node);
        }

        /// <summary>
        /// 건물 footprint 영역 안에 건설 불가 셀이 하나라도 있으면 false를 반환합니다.
        /// 베이크된 노드가 없는 셀(통행 불가 지형)과 층이 섞인 영역도 건설 불가로 간주해 차단합니다.
        /// </summary>
        public bool CanBuildFootprint(Vector3 worldCenter, Vector2Int size)
        {
            Tilemap map = PrimaryMap;
            if (map == null || _bakedData == null)
                return true;

            Vector3 cellSize = map.cellSize;
            Vector3 origin = worldCenter - new Vector3(
                (size.x - 1) * 0.5f * cellSize.x,
                (size.y - 1) * 0.5f * cellSize.y, 0f);
            Vector3Int baseCell = map.WorldToCell(origin);
            int footprintLevel = 0;
            bool levelKnown = false;

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector3Int cell = new Vector3Int(baseCell.x + x, baseCell.y + y, 0);
                    if (!_bakedData.TryGetNode(cell, out NodeData node) || !node.canBuildGeneral)
                        return false;

                    // 층이 걸친 자리에는 세울 수 없습니다.
                    if (!levelKnown)
                    {
                        footprintLevel = node.level;
                        levelKnown = true;
                    }
                    else if (node.level != footprintLevel)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 월드 좌표를 건물 크기를 고려해 가장 가까운 셀 정렬 위치로 스냅합니다.
        /// 변이 짝수면 셀 경계, 홀수면 셀 중앙에 정렬됩니다.
        /// </summary>
        public Vector3 SnapToGrid(Vector3 worldPos, Vector2Int size)
        {
            Tilemap map = PrimaryMap;
            if (map == null)
                return worldPos;

            Vector3 cellSize = map.cellSize;
            Vector3 offset = new Vector3((size.x - 1) * 0.5f * cellSize.x, (size.y - 1) * 0.5f * cellSize.y, 0f);

            Vector3Int cell = map.WorldToCell(worldPos - offset);
            return map.GetCellCenterWorld(cell) + offset;
        }

        private void AddPoint(Vector3Int pos)
        {
            Vector3 worldPos = PrimaryMap.GetCellCenterWorld(pos);
            worldPos.z = 0f;
            _resolver.Resolve(pos, out int level, out TopographyType topography);
            _bakedData.AddPoint(worldPos, pos, topography, level, IsBuildableCell(pos));
        }

        private bool CanMovePosition(Vector3Int pos)
        {
            if (_occupancy.IsOccupied(pos))
                return false;

            // 차단 레이어(벽 등)에 타일이 있으면 바닥(walkable)이 있어도 통행 불가.
            if (_blockingMaps != null)
            {
                foreach (Tilemap blocker in _blockingMaps)
                    if (blocker != null && blocker.HasTile(pos))
                        return false;
            }

            // walkableMaps 중 하나라도 타일이 있으면 통행 가능.
            foreach (PathBaker.WalkableLayer layer in _walkableMaps)
            {
                if (layer.map != null && layer.map.HasTile(pos))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 해당 셀이 건물 건설 가능한지 판정합니다. 건설 허용 타일맵에 타일이 있어야 하며,
        /// 건설 불가 타일맵(다리 등)이 덮고 있으면 차단합니다.
        /// </summary>
        private bool IsBuildableCell(Vector3Int pos)
        {
            if (_blockingMaps != null)
            {
                foreach (Tilemap blocker in _blockingMaps)
                    if (blocker != null && blocker.HasTile(pos))
                        return false;
            }

            bool buildable = false;
            foreach (PathBaker.WalkableLayer layer in _walkableMaps)
            {
                if (layer.map == null || !layer.map.HasTile(pos))
                    continue;

                if (!layer.buildable)
                    return false; // 건설 불가 레이어가 덮고 있으면 즉시 차단

                buildable = true;
            }

            return buildable;
        }

        /// <summary>
        /// 모든 walkableMaps의 cellBounds를 합집합으로 계산합니다.
        /// </summary>
        private BoundsInt CalculateWalkableBounds()
        {
            bool hasBounds = false;
            int xMin = 0, yMin = 0, xMax = 0, yMax = 0;

            foreach (PathBaker.WalkableLayer layer in _walkableMaps)
            {
                Tilemap map = layer.map;
                if (map == null)
                    continue;

                map.CompressBounds();
                BoundsInt b = map.cellBounds;

                if (!hasBounds)
                {
                    xMin = b.xMin; yMin = b.yMin;
                    xMax = b.xMax; yMax = b.yMax;
                    hasBounds = true;
                }
                else
                {
                    xMin = Mathf.Min(xMin, b.xMin);
                    yMin = Mathf.Min(yMin, b.yMin);
                    xMax = Mathf.Max(xMax, b.xMax);
                    yMax = Mathf.Max(yMax, b.yMax);
                }
            }

            return new BoundsInt(xMin, yMin, 0, xMax - xMin, yMax - yMin, 1);
        }

        private void LinkNeighbors(NodeData currentNode)
        {
            currentNode.neighbors.Clear();

            Vector2Int cell = new Vector2Int(currentNode.cellPosition.x, currentNode.cellPosition.y);
            foreach (Vector2Int dir in RectDirections)
            {
                if (!TryGetLinkable(currentNode, cell + dir, out NodeData adjacent))
                    continue;

                // 대각선은 두 직교 이웃이 모두 열려 있을 때만 통과시킵니다.
                // 절벽 모서리·건물 사이를 대각으로 스쳐 지나가는 것을 막습니다.
                if (dir.x != 0 && dir.y != 0
                    && (!TryGetLinkable(currentNode, cell + new Vector2Int(dir.x, 0), out _)
                        || !TryGetLinkable(currentNode, cell + new Vector2Int(0, dir.y), out _)))
                    continue;

                currentNode.AddNeighbor(adjacent);
            }
        }

        /// <summary>
        /// 인접 셀에 노드가 있고 층 규칙상 연결 가능하면 true를 반환합니다.
        /// </summary>
        private bool TryGetLinkable(in NodeData from, Vector2Int cell, out NodeData adjacent)
        {
            if (!_bakedData.TryGetNode(new Vector3Int(cell.x, cell.y, 0), out adjacent))
                return false;

            return TerrainLevelResolver.CanLink(from, adjacent);
        }
    }
}
