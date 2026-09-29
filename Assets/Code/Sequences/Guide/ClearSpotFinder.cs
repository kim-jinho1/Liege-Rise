using Code.Navigation.Baking;
using Code.Navigation.Graph;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Sequences.Guide
{
    /// <summary>
    /// 원하는 자리 근처에서 비어 있는 통행 칸을 찾습니다. 절차 배치 맵은 고정 오프셋 자리에 나무·농장이 서 있을 수 있습니다.
    /// </summary>
    public static class ClearSpotFinder
    {
        private const int MaxRing = 10;
        private const int Directions = 16;

        private static readonly Collider2D[] Buffer = new Collider2D[16];
        private static ContactFilter2D _filter;
        private static bool _filterReady;

        /// <summary>
        /// wanted에서 나선으로 넓혀 가며 첫 빈 칸을 돌려줍니다. 없으면 wanted 그대로입니다.
        /// </summary>
        public static Vector3 Find(Vector3 wanted, float clearRadius)
        {
            if (clearRadius <= 0f)
                return wanted;

            EnsureFilter();
            bool hasMap = TryGetMap(out BakedDataSO baked, out Tilemap map);

            for (int ring = 0; ring <= MaxRing; ring++)
            {
                int steps = ring == 0 ? 1 : Directions;
                for (int i = 0; i < steps; i++)
                {
                    float angle = i * (Mathf.PI * 2f / steps);
                    Vector3 candidate = wanted + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ring;

                    if (hasMap)
                    {
                        if (!baked.TryGetNode(map.WorldToCell(candidate), out NodeData node))
                            continue;

                        candidate = node.worldPosition;
                        candidate.z = wanted.z;
                    }

                    if (Physics2D.OverlapCircle(candidate, clearRadius, _filter, Buffer) == 0)
                        return candidate;
                }
            }

            return wanted;
        }

        private static void EnsureFilter()
        {
            if (_filterReady)
                return;

            _filterReady = true;
            _filter = ContactFilter2D.noFilter; // NoFilter() 메서드는 Unity 6에서 폐기됐다
            _filter.useTriggers = false; // 감지 센서 같은 큰 트리거가 전부 막아 버린다
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
