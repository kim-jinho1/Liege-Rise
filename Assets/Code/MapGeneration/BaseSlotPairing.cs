using System.Collections.Generic;
using UnityEngine;

namespace Code.MapGeneration
{
    /// <summary>
    /// 씬의 기지 슬롯을 PairKey로 묶어 한 쌍을 뽑고, 플레이어와 적을 무작위로 배정합니다.
    /// FindObjectsByType의 순서는 보장되지 않으므로 짝과 슬롯을 이름으로 정렬해
    /// 같은 시드가 같은 배치를 재현하도록 합니다.
    /// </summary>
    public static class BaseSlotPairing
    {
        /// <summary>
        /// 유효한 짝 하나를 무작위로 골라 플레이어·적 슬롯을 결정합니다.
        /// 같은 PairKey를 가진 슬롯이 정확히 2개인 짝만 유효합니다.
        /// </summary>
        public static bool TrySelect(IReadOnlyList<BaseSlotAnchor> slots, System.Random random,
            out BaseSlotAnchor player, out BaseSlotAnchor enemy)
        {
            player = null;
            enemy = null;

            List<List<BaseSlotAnchor>> pairs = CollectValidPairs(slots);
            if (pairs.Count == 0)
                return false;

            List<BaseSlotAnchor> chosen = pairs[random.Next(pairs.Count)];

            if (TryUseDeclaredSides(chosen, out player, out enemy))
                return true;

            bool flip = random.Next(2) == 0;

            player = chosen[flip ? 0 : 1];
            enemy = chosen[flip ? 1 : 0];
            return true;
        }

        /// <summary>
        /// 슬롯이 진영을 선언해 두었으면 그대로 씁니다. 튜토리얼처럼 매 판 같은 자리에서
        /// 시작해야 하는 맵을 시드가 아니라 씬의 선언으로 고정합니다.
        /// 한쪽만 선언해도 나머지는 자동으로 반대편이 됩니다.
        /// </summary>
        private static bool TryUseDeclaredSides(List<BaseSlotAnchor> pair, out BaseSlotAnchor player,
            out BaseSlotAnchor enemy)
        {
            player = null;
            enemy = null;

            foreach (BaseSlotAnchor slot in pair)
            {
                if (slot.FixedSide == SlotSide.Player)
                {
                    if (player != null)
                        return WarnConflict(slot.PairKey, "플레이어", out player, out enemy);

                    player = slot;
                }
                else if (slot.FixedSide == SlotSide.Enemy)
                {
                    if (enemy != null)
                        return WarnConflict(slot.PairKey, "적", out player, out enemy);

                    enemy = slot;
                }
            }

            if (player == null && enemy == null)
                return false;

            if (player == null)
                player = pair[0] == enemy ? pair[1] : pair[0];
            else if (enemy == null)
                enemy = pair[0] == player ? pair[1] : pair[0];

            return true;
        }

        /// <summary>
        /// 짝의 두 슬롯이 같은 진영을 선언한 경우입니다. 반대편이 없으므로 무작위로 되돌립니다.
        /// </summary>
        private static bool WarnConflict(string pairKey, string side, out BaseSlotAnchor player,
            out BaseSlotAnchor enemy)
        {
            player = null;
            enemy = null;
            Debug.LogWarning($"[BaseSlotPairing] PairKey '{pairKey}'의 슬롯 둘 다 {side} 진영으로 선언돼 있습니다. " +
                             "반대편이 없어 무작위 배정으로 되돌립니다.");
            return false;
        }

        /// <summary>
        /// PairKey별로 슬롯을 묶고, 슬롯이 정확히 2개인 짝만 이름순으로 반환합니다.
        /// 개수가 맞지 않는 짝은 경고를 남기고 버립니다 — 반대편을 특정할 수 없기 때문입니다.
        /// </summary>
        private static List<List<BaseSlotAnchor>> CollectValidPairs(IReadOnlyList<BaseSlotAnchor> slots)
        {
            Dictionary<string, List<BaseSlotAnchor>> grouped = new();

            foreach (BaseSlotAnchor slot in slots)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.PairKey))
                    continue;

                if (!grouped.TryGetValue(slot.PairKey, out List<BaseSlotAnchor> group))
                {
                    group = new List<BaseSlotAnchor>(2);
                    grouped[slot.PairKey] = group;
                }

                group.Add(slot);
            }

            List<string> keys = new(grouped.Keys);
            keys.Sort(string.CompareOrdinal);

            List<List<BaseSlotAnchor>> valid = new(keys.Count);
            foreach (string key in keys)
            {
                List<BaseSlotAnchor> group = grouped[key];
                if (group.Count != 2)
                {
                    Debug.LogWarning($"[BaseSlotPairing] PairKey '{key}'의 슬롯이 {group.Count}개입니다. " +
                                     "2개여야 하므로 이 짝을 건너뜁니다.");
                    continue;
                }

                group.Sort(CompareByName);
                valid.Add(group);
            }

            return valid;
        }

        /// <summary>
        /// 슬롯 순서를 결정론적으로 만듭니다. 이름이 같으면 위치로 가릅니다.
        /// </summary>
        private static int CompareByName(BaseSlotAnchor left, BaseSlotAnchor right)
        {
            int byName = string.CompareOrdinal(left.name, right.name);
            if (byName != 0)
                return byName;

            Vector3 a = left.Center;
            Vector3 b = right.Center;
            int byX = a.x.CompareTo(b.x);
            return byX != 0 ? byX : a.y.CompareTo(b.y);
        }
    }
}
