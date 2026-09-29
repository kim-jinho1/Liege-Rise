using UnityEngine;

namespace Code.UI.Components
{
    /// <summary>
    /// 월드 툴팁에 표시할 데이터를 담는 구조체입니다.
    /// </summary>
    public readonly struct TooltipData
    {
        public readonly Sprite Icon;
        public readonly string Name;
        public readonly int Current;
        public readonly int Max;

        public TooltipData(Sprite icon, string name, int current, int max)
        {
            Icon = icon;
            Name = name;
            Current = current;
            Max = max;
        }

        public float NormalizedProgress
            => Max > 0 ? (float)Current / Max : 0f;
    }
}
