using System;
using UnityEngine;

namespace Code.MapGeneration
{
    /// <summary>
    /// 기지 하나의 구성입니다. 본부 건물은 앵커 중앙에, 위성은 앵커 반경 안에 흩뿌립니다.
    /// 플레이어 기지와 적 기지가 같은 구조를 쓰고 프리팹 목록만 다릅니다.
    /// </summary>
    [CreateAssetMenu(fileName = "BaseComposition", menuName = "SO/Map/BaseComposition", order = 2)]
    public class BaseCompositionSO : ScriptableObject
    {
        // 앵커 중앙에 놓을 본부 건물 프리팹. Owner는 프리팹에 설정된 값을 그대로 쓴다
        [field: SerializeField] public GameObject CorePrefab { get; private set; }

        // 본부 건물의 건설 판정 타일 크기. BuildingSO의 Size와 같은 값을 넣는다
        [field: SerializeField] public Vector2Int CoreFootprint { get; private set; } = new(4, 3);

        // 본부 주변에 흩뿌릴 것들. 간격 판정이 중심점 기준이라 minSpacing이 본부 크기의 절반보다 작으면 겹친다
        [field: SerializeField] public SpawnEntry[] Satellites { get; private set; } = Array.Empty<SpawnEntry>();

        /// <summary>
        /// 본부 건물의 건설 판정 크기입니다. 미설정 시 1x1로 간주합니다.
        /// </summary>
        public Vector2Int SafeCoreFootprint
            => new(Mathf.Max(1, CoreFootprint.x), Mathf.Max(1, CoreFootprint.y));

#if UNITY_EDITOR
        /// <summary>
        /// 간격 판정은 본부의 크기가 아니라 중심점 하나와의 거리로 이뤄집니다.
        /// 그래서 minSpacing이 본부 크기의 절반보다 작으면 위성이 본부 스프라이트 위에 겹칩니다.
        /// </summary>
        private void OnValidate()
        {
            if (Satellites == null)
                return;

            float halfCore = Mathf.Max(SafeCoreFootprint.x, SafeCoreFootprint.y) * 0.5f;

            foreach (SpawnEntry satellite in Satellites)
            {
                if (!satellite.IsValid)
                    continue;

                // 무리 구성원은 씨앗에서 반경만큼 더 다가올 수 있으므로 그만큼 뺀 값이 실제 최단 거리다
                float closest = satellite.IsClumped
                    ? satellite.minSpacing - satellite.clumpRadius
                    : satellite.minSpacing;

                if (closest >= halfCore)
                    continue;

                Debug.LogWarning($"[{name}] 위성 '{satellite.prefab.name}'이 본부에서 최대 {closest}까지 다가올 수 있어 " +
                                 $"본부 크기의 절반({halfCore})보다 가깝습니다. 본부 위에 겹칠 수 있습니다.", this);
            }
        }
#endif
    }
}
