using Code.Navigation.Baking;
using Code.Navigation.Graph;
using Code.Navigation.Pathfinding;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Military.Commanders
{
    /// <summary>
    /// 공세선을 지형에 맞춥니다 — 슬롯을 통행 가능한 칸으로 옮기고, 적의 다음 선을 직선이 아니라
    /// 실제 길(다리·경사로)을 따라 잡습니다. 베이크 데이터가 없으면 아무것도 바꾸지 않습니다.
    /// </summary>
    public static class LineTerrain
    {
        private const int PathCapacity = 512;
        private static readonly Vector3[] PathBuffer = new Vector3[PathCapacity];

        /// <summary>
        /// 가장 가까운 통행 칸의 중심입니다. 4칸 안에 없으면 그대로 돌려줍니다.
        /// </summary>
        public static Vector2 Snap(Vector2 point)
        {
            if (!TryGetMap(out BakedDataSO baked, out Tilemap map))
                return point;

            Vector3Int cell = map.WorldToCell(point);
            if (baked.TryGetNode(cell, out NodeData node))
                return node.worldPosition;

            Vector3Int nearest = PathfindingEngine.FindNearestValidCell(cell, baked);
            return baked.TryGetNode(nearest, out node) ? node.worldPosition : point;
        }

        /// <summary>
        /// 출발점에서 목표까지의 길을 따라 지정 거리만큼 나간 지점과 그 자리의 진행 방향입니다.
        /// 길이 없으면 false — 부르는 쪽이 직선으로 대신합니다.
        /// </summary>
        public static bool TryStepAlongPath(Vector2 from, Vector2 to, float distance,
            out Vector2 point, out Vector2 direction)
        {
            point = default;
            direction = default;

            if (!TryGetMap(out BakedDataSO baked, out Tilemap map))
                return false;

            int count = PathfindingEngine.CalculatePath(from, to, baked, map, 0.5f, PathBuffer);
            if (count == 0)
                return false;

            return Walk(from, to, count, distance, out point, out direction);
        }

        /// <summary>
        /// 길의 끝에서 지정 거리만큼 앞에 선 지점과 그 자리의 진행 방향입니다.
        /// 목표 바로 앞에 한 번에 선을 그을 때 씁니다. 길이 없으면 false입니다.
        /// </summary>
        public static bool TryStepBeforeEnd(Vector2 from, Vector2 to, float standoff,
            out Vector2 point, out Vector2 direction)
        {
            point = default;
            direction = default;

            if (!TryGetMap(out BakedDataSO baked, out Tilemap map))
                return false;

            int count = PathfindingEngine.CalculatePath(from, to, baked, map, 0.5f, PathBuffer);
            if (count == 0)
                return false;

            float total = 0f;
            Vector2 last = from;
            for (int i = 0; i < count; i++)
            {
                total += ((Vector2)PathBuffer[i] - last).magnitude;
                last = PathBuffer[i];
            }

            return Walk(from, to, count, Mathf.Max(0f, total - standoff), out point, out direction);
        }

        private static bool Walk(Vector2 from, Vector2 to, int count, float distance,
            out Vector2 point, out Vector2 direction)
        {
            point = default;
            direction = default;

            Vector2 previous = from;
            float remaining = distance;

            for (int i = 0; i < count; i++)
            {
                Vector2 next = PathBuffer[i];
                Vector2 segment = next - previous;
                float length = segment.magnitude;

                if (length < 0.0001f)
                    continue;

                if (remaining <= length)
                {
                    direction = segment / length;
                    point = previous + direction * remaining;
                    return true;
                }

                remaining -= length;
                previous = next;
            }

            // 길이 지정 거리보다 짧다 — 끝점에 서고 마지막 구간 방향을 본다
            Vector2 last = count >= 2 ? (Vector2)PathBuffer[count - 1] - (Vector2)PathBuffer[count - 2] : to - from;
            direction = last.sqrMagnitude < 0.0001f ? (to - from).normalized : last.normalized;
            point = PathBuffer[count - 1];
            return direction.sqrMagnitude > 0.0001f;
        }

        private static bool TryGetMap(out BakedDataSO baked, out Tilemap map)
        {
            PathBaker baker = PathBaker.Instance;
            baked = baker != null ? baker.BakedData : null;
            map = baker != null ? baker.BaseTilemap : null;
            return baked != null && map != null;
        }
    }
}
