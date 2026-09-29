using System;
using UnityEngine;

namespace Code.MapGeneration
{
    /// <summary>
    /// 구역 구성 표의 항목 하나입니다. 가중치가 높을수록 자주 뽑힙니다.
    /// </summary>
    [Serializable]
    public struct ZoneOption
    {
        // 놓을 프리팹과 배치 규칙. countRange.x가 보장 최소치, countRange.y가 상한이다
        public SpawnEntry entry;

        // 추가 수량을 나눌 때의 가중치. 0이면 보장 최소치만 놓이고 더 뽑히지 않는다
        [Min(0)] public int weight;

        // 체크하면 구역 전체가 아니라 중심의 ClusterRadius 안에서만 자리를 뽑는다. 적 진지를 한데 모을 때 쓴다
        public bool clustered;
    }

    /// <summary>
    /// 구역 하나에 무엇을 얼마나 뿌릴지 정하는 랜덤 표입니다.
    /// 총 개수와 종류 구성이 매 판 달라지지만, 한 종류로만 채워지는 일은 없습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ZoneComposition", menuName = "SO/Map/ZoneComposition", order = 1)]
    public class ZoneCompositionSO : ScriptableObject
    {
        // 이 구역에 놓을 총 개수 범위 (x = 최소, y = 최대)
        [field: SerializeField] public Vector2Int TotalRange { get; private set; } = new(6, 10);

        // 반드시 함께 등장해야 할 서로 다른 종류의 수. 2면 한 자원으로만 채워지는 일이 없다
        [field: SerializeField, Min(1)] public int MinDistinctKinds { get; private set; } = 2;

        // clustered 항목이 뭉칠 반경. 중심은 구역 앵커 자신이다. 0이면 뭉치지 않고 구역 전체에 흩어진다
        [field: SerializeField, Min(0f)] public float ClusterRadius { get; private set; } = 5f;

        // 뽑을 후보 목록
        [field: SerializeField] public ZoneOption[] Options { get; private set; } = Array.Empty<ZoneOption>();

        /// <summary>
        /// 이번 판에 놓을 종류별 개수를 굴립니다. 반환 배열은 Options와 같은 순서·길이입니다.
        /// 서로 다른 종류를 MinDistinctKinds개 먼저 확보한 뒤 항목별 보장 최소치를 채우고,
        /// 남은 수량을 가중치로 분배합니다. 종류 확보를 먼저 하므로 한 항목이 총량을 독식하지 못합니다.
        /// </summary>
        public int[] Roll(System.Random random)
        {
            int[] counts = new int[Options.Length];
            if (Options.Length == 0)
                return counts;

            int remaining = ResolveTotal(random);
            remaining = ReserveDistinctKinds(counts, remaining, random);
            remaining = FillGuaranteedMinimums(counts, remaining);
            DistributeByWeight(counts, remaining, random);
            return counts;
        }

        /// <summary>
        /// 이번 판의 총 개수를 뽑습니다.
        /// </summary>
        private int ResolveTotal(System.Random random)
        {
            int min = Mathf.Max(0, TotalRange.x);
            int max = Mathf.Max(min, TotalRange.y);
            return min == max ? min : random.Next(min, max + 1);
        }

        /// <summary>
        /// 서로 다른 종류를 MinDistinctKinds개까지 1개씩 먼저 확보하고 남은 수량을 반환합니다.
        /// 가중치가 0인 항목도 여기서는 후보에 들어갑니다 — 종류 다양성이 가중치보다 우선입니다.
        /// </summary>
        private int ReserveDistinctKinds(int[] counts, int remaining, System.Random random)
        {
            int target = Mathf.Min(MinDistinctKinds, CountValidOptions());

            for (int reserved = 0; reserved < target && remaining > 0; reserved++)
            {
                int index = PickIndex(random, i =>
                    counts[i] == 0 && Options[i].entry.IsValid ? Mathf.Max(1, Options[i].weight) : 0);

                if (index < 0)
                    break;

                counts[index] = 1;
                remaining--;
            }

            return remaining;
        }

        /// <summary>
        /// 항목별 보장 최소치(countRange.x)를 상한 안에서 채우고 남은 수량을 반환합니다.
        /// </summary>
        private int FillGuaranteedMinimums(int[] counts, int remaining)
        {
            for (int i = 0; i < Options.Length && remaining > 0; i++)
            {
                SpawnEntry entry = Options[i].entry;
                if (!entry.IsValid)
                    continue;

                int minimum = Mathf.Min(Mathf.Max(0, entry.countRange.x), entry.countRange.y);
                while (counts[i] < minimum && remaining > 0)
                {
                    counts[i]++;
                    remaining--;
                }
            }

            return remaining;
        }

        /// <summary>
        /// 남은 수량을 가중치에 따라 상한까지 나눠 담습니다.
        /// </summary>
        private void DistributeByWeight(int[] counts, int remaining, System.Random random)
        {
            while (remaining > 0)
            {
                int index = PickIndex(random, i =>
                    Options[i].entry.IsValid && counts[i] < Options[i].entry.countRange.y
                        ? Options[i].weight
                        : 0);

                if (index < 0)
                    return;

                counts[index]++;
                remaining--;
            }
        }

        /// <summary>
        /// 가중치 함수가 매긴 값에 비례해 항목 하나를 뽑습니다. 뽑을 것이 없으면 -1을 반환합니다.
        /// </summary>
        private int PickIndex(System.Random random, Func<int, int> weightOf)
        {
            int total = 0;
            for (int i = 0; i < Options.Length; i++)
                total += Mathf.Max(0, weightOf(i));

            if (total <= 0)
                return -1;

            int roll = random.Next(total);
            for (int i = 0; i < Options.Length; i++)
            {
                int weight = Mathf.Max(0, weightOf(i));
                if (weight == 0)
                    continue;

                roll -= weight;
                if (roll < 0)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 실제로 배치 가능한 항목 수입니다.
        /// </summary>
        private int CountValidOptions()
        {
            int valid = 0;
            foreach (ZoneOption option in Options)
                if (option.entry.IsValid)
                    valid++;

            return valid;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Roll은 어떤 잘못된 값에도 예외 없이 조용히 굴러가므로, 값이 무력화되는 경우를 여기서 알립니다.
        /// 총량 상한이 옵션 합보다 크게 적힌 오타가 아무 신호 없이 오래 살아 있었습니다.
        /// </summary>
        private void OnValidate()
        {
            if (Options == null || Options.Length == 0)
                return;

            int capSum = 0;
            int minSum = 0;

            foreach (ZoneOption option in Options)
            {
                if (!option.entry.IsValid)
                    continue;

                capSum += Mathf.Max(0, option.entry.countRange.y);
                minSum += Mathf.Min(Mathf.Max(0, option.entry.countRange.x), option.entry.countRange.y);
            }

            if (TotalRange.y > capSum)
                Debug.LogWarning($"[{name}] 총량 상한 {TotalRange.y}가 옵션 상한 합계 {capSum}보다 큽니다. " +
                                 $"초과분은 조용히 버려져 실제로는 최대 {capSum}개만 놓입니다.", this);

            if (TotalRange.x < minSum)
                Debug.LogWarning($"[{name}] 총량 하한 {TotalRange.x}가 옵션 보장 최소치 합계 {minSum}보다 작습니다. " +
                                 "목록 뒤쪽 옵션의 보장 최소치가 무시될 수 있습니다.", this);

            if (ClusterRadius <= 0f && HasClusteredOption())
                Debug.LogWarning($"[{name}] 뭉칠 항목이 있는데 Cluster Radius가 0입니다. " +
                                 "뭉치지 않고 구역 전체에 흩어집니다.", this);

            WarnOnOverlappingClumps();

            int validOptions = CountValidOptions();
            if (MinDistinctKinds > validOptions)
                Debug.LogWarning($"[{name}] 최소 종류 수 {MinDistinctKinds}가 쓸 수 있는 옵션 {validOptions}개보다 큽니다. " +
                                 $"{validOptions}로 잘려 적용되므로 종류 보장이 성립하지 않습니다.", this);
        }

        /// <summary>
        /// 무리 반경이 무리 사이 거리보다 크면 씨앗이 남의 무리 안까지 들어가 종류가 섞입니다.
        /// </summary>
        private void WarnOnOverlappingClumps()
        {
            foreach (ZoneOption option in Options)
            {
                SpawnEntry entry = option.entry;
                if (!entry.IsValid || !entry.IsClumped || entry.clumpRadius < entry.minSpacing)
                    continue;

                Debug.LogWarning($"[{name}] {entry.prefab.name}의 무리 반경 {entry.clumpRadius}가 " +
                                 $"무리 사이 거리 {entry.minSpacing}보다 작지 않습니다. " +
                                 "무리끼리, 또 다른 종류와 섞일 수 있습니다.", this);
            }
        }

        private bool HasClusteredOption()
        {
            foreach (ZoneOption option in Options)
                if (option.clustered && option.entry.IsValid)
                    return true;

            return false;
        }
#endif
    }
}
