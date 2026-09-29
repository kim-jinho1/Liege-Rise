using UnityEngine;

namespace Code.MapGeneration
{
    /// <summary>
    /// 같은 이름표를 단 구역 앵커들을 후보로 묶고, 그중 몇 곳만 골라 씁니다.
    /// 앵커를 전부 채우면 매 판 같은 자리에 같은 것이 서지만, 골라 쓰면 판마다 지도가 달라집니다.
    /// 확장 지대(자원 + 적 진지)를 이 방식으로 흩습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ZoneGroup", menuName = "SO/Map/ZoneGroup", order = 3)]
    public class ZoneGroupSO : ScriptableObject
    {
        // SpawnZoneAnchor의 GroupKey와 같은 값을 넣는다
        [field: SerializeField] public string Key { get; private set; } = "Expansion";

        // 후보 앵커 중 실제로 쓸 곳의 수. 나머지는 비워 둔다
        [field: SerializeField, Min(0)] public int ActiveCount { get; private set; } = 6;

        // 적 진지가 붙은 구성. 먹으려면 싸워야 하는 자리다
        [field: SerializeField] public ZoneCompositionSO Primary { get; private set; }

        // 활성 앵커 중 Primary를 적용할 곳의 수. 나머지는 Secondary가 된다
        [field: SerializeField, Min(0)] public int PrimaryCount { get; private set; } = 4;

        // 적이 없는 구성. 플레이어가 편하게 확장할 자리다
        [field: SerializeField] public ZoneCompositionSO Secondary { get; private set; }

        /// <summary>
        /// 활성 앵커 중 몇 번째까지 Primary를 줄지입니다. 활성 수를 넘지 않습니다.
        /// </summary>
        public int ResolvePrimaryCount(int activeCount)
            => Mathf.Clamp(PrimaryCount, 0, activeCount);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Primary == null && Secondary == null)
                Debug.LogWarning($"[{name}] Primary와 Secondary가 모두 비어 있어 이 그룹은 아무것도 배치하지 않습니다.", this);

            if (PrimaryCount > ActiveCount)
                Debug.LogWarning($"[{name}] Primary Count({PrimaryCount})가 Active Count({ActiveCount})보다 큽니다. " +
                                 "활성 수만큼으로 잘려 적용됩니다.", this);

            if (string.IsNullOrWhiteSpace(Key))
                Debug.LogWarning($"[{name}] Key가 비어 있어 어느 앵커와도 묶이지 않습니다.", this);
        }
#endif
    }
}
