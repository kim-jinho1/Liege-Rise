using System;
using UnityEngine;

namespace Code.Military.Stats
{
    /// <summary>
    /// 스탯 하나에 걸 보정을 인스펙터에서 정의하는 항목입니다.
    /// 대형처럼 켜고 끄는 효과가 여러 스탯을 한 번에 건드릴 때 배열로 씁니다.
    /// </summary>
    [Serializable]
    public class StatEffect
    {
        [field: SerializeField] public StatSO Stat { get; private set; }

        // 방향 배율을 낮추는 것이 기본 용도라 뺄셈이 읽기 쉽다 — 측면 1.5에 -0.4면 1.1
        [field: SerializeField] public ModifierOperator Operator { get; private set; } = ModifierOperator.Additive;
        [field: SerializeField] public float Value { get; private set; }

        /// <summary>
        /// 이 보정을 스탯 시트에 적용합니다. 스탯이 비어 있으면 아무것도 하지 않습니다.
        /// </summary>
        public void ApplyTo(StatSheet sheet, object source)
        {
            if (Stat == null || sheet == null)
                return;

            sheet.Get(Stat)?.AddModifier(new StatModifier(Operator, Value, source));
        }
    }
}
