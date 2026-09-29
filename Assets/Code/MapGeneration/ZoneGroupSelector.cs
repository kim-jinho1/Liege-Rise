using System.Collections.Generic;
using UnityEngine;

namespace Code.MapGeneration
{
    /// <summary>
    /// 이번 판에 어느 구역 앵커를 쓰고 각각 무엇을 뿌릴지 정합니다.
    /// 그룹에 속하지 않은 앵커는 지금까지처럼 자기 Composition을 그대로 씁니다.
    /// 짝을 이룬 앵커는 함께 뽑히고 같은 구성을 받으므로 맵 대칭이 매 판 유지됩니다.
    /// </summary>
    public class ZoneGroupSelector
    {
        /// <summary>
        /// 이번 판에 실제로 채워질 앵커 수입니다.
        /// </summary>
        public int ActiveZoneCount { get; private set; }

        private readonly Dictionary<SpawnZoneAnchor, ZoneCompositionSO> _assigned = new(32);
        private readonly Dictionary<string, List<SpawnZoneAnchor>> _byPairKey = new(16);
        private readonly List<List<SpawnZoneAnchor>> _units = new(16);
        private readonly List<List<SpawnZoneAnchor>> _selected = new(16);

        /// <summary>
        /// 이번 판에 쓸 앵커와 구성을 확정합니다. 앵커 배열은 이름순으로 정렬돼 있어야
        /// 같은 시드가 같은 지도를 재현합니다.
        /// </summary>
        public void Resolve(SpawnZoneAnchor[] zones, ZoneGroupSO[] groups, System.Random random)
        {
            _assigned.Clear();

            foreach (SpawnZoneAnchor zone in zones)
                if (!BelongsToAnyGroup(zone, groups))
                    _assigned[zone] = zone.Composition;

            if (groups != null)
            {
                foreach (ZoneGroupSO group in groups)
                {
                    if (group == null || string.IsNullOrWhiteSpace(group.Key))
                        continue;

                    AssignGroup(group, zones, random);
                }
            }

            ActiveZoneCount = CountAssigned();
        }

        /// <summary>
        /// 이 앵커가 이번 판에 쓰이는지, 쓰인다면 무엇을 뿌릴지 돌려줍니다.
        /// </summary>
        public bool TryGetComposition(SpawnZoneAnchor zone, out ZoneCompositionSO composition)
            => _assigned.TryGetValue(zone, out composition) && composition != null;

        /// <summary>
        /// 그룹 하나의 후보를 짝 단위로 섞어 활성 앵커를 뽑고, 앞쪽 단위부터 Primary를 배정합니다.
        /// </summary>
        private void AssignGroup(ZoneGroupSO group, SpawnZoneAnchor[] zones, System.Random random)
        {
            CollectUnits(group, zones);
            Shuffle(_units, random);

            _selected.Clear();
            int active = 0;

            foreach (List<SpawnZoneAnchor> unit in _units)
            {
                // 넘치는 단위는 건너뛰고 계속 본다. 여기서 멈추면 뒤에 남은 작은 단위가 마지막 칸을 못 채운다
                if (active + unit.Count > group.ActiveCount)
                    continue;

                _selected.Add(unit);
                active += unit.Count;
            }

            int budget = group.ResolvePrimaryCount(active);
            int primary = 0;

            foreach (List<SpawnZoneAnchor> unit in _selected)
            {
                // 짝을 쪼개 한쪽만 Primary로 만들면 적 진지가 한쪽에만 서서 대칭이 도로 깨진다
                bool usePrimary = primary + unit.Count <= budget;
                if (usePrimary)
                    primary += unit.Count;

                foreach (SpawnZoneAnchor zone in unit)
                    _assigned[zone] = usePrimary ? group.Primary : group.Secondary;
            }
        }

        /// <summary>
        /// 그룹의 후보 앵커를 선택 단위로 묶습니다. 단위는 짝 하나 또는 짝이 없는 앵커 하나입니다.
        /// 앵커 배열이 이름순이므로 첫 등장 순서로 담으면 단위 순서도 결정론적입니다 —
        /// 딕셔너리를 순회하면 그 순서가 깨져 같은 시드가 다른 지도를 냅니다.
        /// </summary>
        private void CollectUnits(ZoneGroupSO group, SpawnZoneAnchor[] zones)
        {
            _units.Clear();
            _byPairKey.Clear();

            foreach (SpawnZoneAnchor zone in zones)
            {
                if (!Matches(zone, group))
                    continue;

                if (string.IsNullOrWhiteSpace(zone.PairKey))
                {
                    _units.Add(new List<SpawnZoneAnchor>(1) { zone });
                    continue;
                }

                if (!_byPairKey.TryGetValue(zone.PairKey, out List<SpawnZoneAnchor> unit))
                {
                    unit = new List<SpawnZoneAnchor>(2);
                    _byPairKey[zone.PairKey] = unit;
                    _units.Add(unit);
                }

                unit.Add(zone);
            }

            WarnBrokenPairs();
        }

        /// <summary>
        /// 짝이 2개로 맞지 않는 이름표를 알립니다.
        /// BaseSlotPairing과 달리 버리지 않고 그대로 씁니다 — 기지는 반대편을 특정 못 하면
        /// 진영 배정 자체가 불가능하지만, 구역은 배치가 되므로 조용히 지도가 빈약해지는 쪽이 더 나쁩니다.
        /// </summary>
        private void WarnBrokenPairs()
        {
            foreach (List<SpawnZoneAnchor> unit in _units)
            {
                if (unit.Count == 2 || string.IsNullOrWhiteSpace(unit[0].PairKey))
                    continue;

                Debug.LogWarning($"[ZoneGroupSelector] PairKey '{unit[0].PairKey}'의 앵커가 {unit.Count}개입니다. " +
                                 "2개여야 대칭이 보장됩니다.", unit[0]);
            }
        }

        /// <summary>
        /// 구성이 배정돼 실제로 채워질 앵커 수입니다.
        /// </summary>
        private int CountAssigned()
        {
            int count = 0;
            foreach (KeyValuePair<SpawnZoneAnchor, ZoneCompositionSO> entry in _assigned)
                if (entry.Value != null)
                    count++;

            return count;
        }

        private static bool BelongsToAnyGroup(SpawnZoneAnchor zone, ZoneGroupSO[] groups)
        {
            if (groups == null || string.IsNullOrWhiteSpace(zone.GroupKey))
                return false;

            foreach (ZoneGroupSO group in groups)
                if (Matches(zone, group))
                    return true;

            return false;
        }

        private static bool Matches(SpawnZoneAnchor zone, ZoneGroupSO group)
            => group != null
               && !string.IsNullOrWhiteSpace(zone.GroupKey)
               && string.Equals(zone.GroupKey, group.Key, System.StringComparison.Ordinal);

        private static void Shuffle<T>(List<T> items, System.Random random)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int swap = random.Next(i + 1);
                (items[i], items[swap]) = (items[swap], items[i]);
            }
        }
    }
}
