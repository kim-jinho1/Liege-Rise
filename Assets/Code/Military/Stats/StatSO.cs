using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Military.Stats
{
    [CreateAssetMenu(fileName = "Stat", menuName = "Military/Stat", order = 20)]
    public class StatSO : ScriptableObject, ICloneable
    {
        [field: Header("Stat")]
        [field: SerializeField] public string DisplayName { get; private set; } = "스탯";
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public bool IsPercent { get; private set; } // UI에서 백분율로 표기할지 여부

        [Header("Value")]
        [SerializeField] private float baseValue = 1f;
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue = float.MaxValue;

        public delegate void ValueChangedHandler(StatSO stat, float currentValue, float previousValue);

        /// <summary>
        /// 최종값이 변경되었을 때 호출되는 이벤트 핸들러입니다.
        /// </summary>
        public event ValueChangedHandler OnValueChanged;

        private readonly List<StatModifier> _modifiers = new();
        private float _cachedValue;
        private bool _isDirty = true;

        /// <summary>
        /// 보정이 완료된 최종값입니다. 변경이 없으면 캐시된 값이 반환됩니다.
        /// </summary>
        public float Value
        {
            get
            {
                if (!_isDirty)
                    return _cachedValue;

                float additive = 0f;
                float multiplier = 1f;

                foreach (StatModifier modifier in _modifiers)
                {
                    if (modifier.ModifierOp == ModifierOperator.Additive)
                        additive += modifier.Value;
                    else
                        multiplier *= modifier.Value;
                }

                _cachedValue = Mathf.Clamp((baseValue + additive) * multiplier, minValue, maxValue);
                _isDirty = false;
                return _cachedValue;
            }
        }

        public float BaseValue
        {
            get => baseValue;
            set
            {
                float previous = Value;
                baseValue = value;
                _isDirty = true;
                RaiseIfChanged(previous);
            }
        }

        /// <summary>
        /// 보정을 추가합니다.
        /// </summary>
        public void AddModifier(StatModifier modifier)
        {
            float previous = Value;
            _modifiers.Add(modifier);
            _isDirty = true;
            RaiseIfChanged(previous);
        }

        /// <summary>
        /// 지정한 출처가 건 보정을 모두 제거합니다.
        /// </summary>
        public void RemoveBySource(object source)
        {
            float previous = Value;
            bool removed = false;

            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_modifiers[i].Source, source))
                    continue;

                _modifiers.RemoveAt(i);
                removed = true;
            }

            if (!removed)
                return;

            _isDirty = true;
            RaiseIfChanged(previous);
        }

        public object Clone()
            => Instantiate(this);

        private void RaiseIfChanged(float previous)
        {
            float current = Value;
            if (!Mathf.Approximately(current, previous))
                OnValueChanged?.Invoke(this, current, previous);
        }
    }
}
