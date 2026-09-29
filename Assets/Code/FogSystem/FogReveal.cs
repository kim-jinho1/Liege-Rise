using UnityEngine;

namespace Code.FogSystem
{
    /// <summary>
    /// 지정 지점을 잠시 밝힙니다. 시퀀스가 카메라를 옮겨 무언가를 보여 줄 때 씁니다.
    /// 안개 카메라가 찍는 시야 조각을 그 자리에 잠깐 놓는 방식이라, 탐색 기록도 함께 남습니다.
    /// </summary>
    public static class FogReveal
    {
        // 유닛·건물이 달고 다니는 것과 같은 시야 조각. 읽는 쪽에 인스펙터 칸이 없어 Resources에 둔다
        private const string PrefabResourcePath = "Vision";

        private static GameObject _prefab;
        private static bool _warned;

        /// <summary>
        /// 지정 좌표를 seconds 동안 반경 radius만큼 밝힙니다.
        /// </summary>
        public static async Awaitable AtAsync(Vector3 position, float radius, float seconds)
        {
            GameObject prefab = LoadPrefab();
            if (prefab == null || radius <= 0f || seconds <= 0f)
                return;

            position.z = 0f;
            GameObject spot = Object.Instantiate(prefab, position, Quaternion.identity);
            spot.name = "__FogReveal";

            // 시야 조각은 지름으로 그려진다 — 반경을 그대로 넣으면 절반만 밝아진다
            spot.transform.localScale = Vector3.one * (radius * 2f);

            await Awaitable.WaitForSecondsAsync(seconds);

            if (spot != null)
                Object.Destroy(spot);
        }

        private static GameObject LoadPrefab()
        {
            if (_prefab != null)
                return _prefab;

            _prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (_prefab == null && !_warned)
            {
                _warned = true;
                Debug.LogWarning($"Resources/{PrefabResourcePath} 을 찾지 못해 시퀀스가 지점을 밝히지 못합니다.");
            }

            return _prefab;
        }
    }
}
