using System.Collections.Generic;
using UnityEngine;

namespace Code.Units.Combat.Calculator
{
    /// <summary>
    /// 유닛 1기의 활성 버프 목록을 소유하고 공격력/공격 딜레이 배율을 재계산하는 컨트롤러입니다.
    /// 배율 계산 정책(합산 공식, 하한 클램프)은 전부 이 클래스 안에서만 관리합니다.
    /// </summary>
    public class UnitBuffCalculator
    {
        private const float MinAttackDelayMultiplier = 0.1f;

        private readonly List<BuffConfigSO> _activeBuffs = new();

        /// <summary>
        /// 공격력 배율입니다. 버프가 없으면 1입니다.
        /// </summary>
        public float DamageMultiplier { get; private set; } = 1f;

        /// <summary>
        /// 공격 딜레이 배율입니다. 버프가 없으면 1이며 하한은 0.1입니다.
        /// </summary>
        public float AttackDelayMultiplier { get; private set; } = 1f;

        /// <summary>
        /// 버프를 추가하고 활성 버프 전체로부터 배율을 재계산합니다.
        /// </summary>
        public void Add(BuffConfigSO config)
        {
            if (config == null)
                return;

            _activeBuffs.Add(config);
            Recalculate();
        }

        /// <summary>
        /// 버프를 제거하고 활성 버프 전체로부터 배율을 재계산합니다.
        /// </summary>
        public void Remove(BuffConfigSO config)
        {
            if (config == null || !_activeBuffs.Remove(config))
                return;

            Recalculate();
        }

        private void Recalculate()
        {
            float damage = 1f;
            float delay = 1f;

            foreach (BuffConfigSO buff in _activeBuffs)
            {
                damage += buff.AttackDamageBonus;
                delay -= 1f - buff.AttackDelayMultiplier;
            }

            DamageMultiplier = damage;
            AttackDelayMultiplier = Mathf.Max(MinAttackDelayMultiplier, delay);
        }
    }
}
