using System;
using UnityEngine;

namespace Code.Commandable
{
    /// <summary>
    /// 체력 값과 그 변화를 쥡니다. 통지는 Changed로 넘겨 소유자가 자기 이름으로 다시 쏩니다 —
    /// 구독자는 명령 대상을 보고 있지 이 값 그릇을 보고 있지 않습니다.
    /// </summary>
    public sealed class CommandableHealth
    {
        /// <summary>(직전 체력, 지금 체력)입니다.</summary>
        public Action<int, int> Changed;

        public int Current { get; set; }
        public int Max { get; set; }

        /// <summary>
        /// 정원 체력으로 채웁니다.
        /// </summary>
        public void Reset(int max)
        {
            Max = max;
            Current = max;
            Changed?.Invoke(0, Current);
        }

        /// <summary>
        /// 피해를 반영합니다. 여기서는 통지하지 않습니다 — 파생 클래스가 피해량을 손본 뒤
        /// 소유자가 사망 처리와 함께 한 번에 알려야 순서가 어긋나지 않습니다.
        /// </summary>
        public void Subtract(int damage)
            => Current = Mathf.Clamp(Current - damage, 0, Max);

        /// <summary>
        /// 체력을 채웁니다.
        /// </summary>
        public void Add(int amount)
        {
            int lastHealth = Current;
            Current = Mathf.Clamp(Current + amount, 0, Max);
            Changed?.Invoke(lastHealth, Current);
        }

        /// <summary>
        /// 최대치를 바꾸고 지금 비율을 유지합니다. 0 이하이거나 같은 값이면 무시합니다.
        /// </summary>
        public void SetMax(int newValue)
        {
            if (newValue <= 0 || newValue == Max)
                return;

            float ratio = Max > 0 ? (float)Current / Max : 1f;
            int lastHealth = Current;

            Max = newValue;
            Current = Mathf.Clamp(Mathf.RoundToInt(newValue * ratio), 1, newValue);
            Changed?.Invoke(lastHealth, Current);
        }
    }
}
