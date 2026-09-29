using Code.Core;
using Code.UI.WorldSpace;
using UnityEngine;

namespace Code.UI.Components
{
    /// <summary>
    /// 월드 오브젝트가 공유하는 툴팁 싱글톤입니다. Canvas 위에 배치합니다.
    /// </summary>
    public class WorldTooltip : MonoSingleton<WorldTooltip>
    {
        [field: SerializeField] public float HoverDelay { get; private set; } = 0.5f;
        [field: SerializeField] public Vector2 Offset { get; private set; } = new(1.5f, -1.0f);
        [SerializeField] private ResourceTooltip tooltip;

        /// <summary>
        /// 툴팁 데이터를 설정하고 지정된 월드 좌표에 표시합니다.
        /// </summary>
        public void Show(TooltipData data, Vector2 worldPosition)
        {
            tooltip.EnableFor(data, worldPosition);
        }

        /// <summary>
        /// 표시 중인 툴팁의 수량만 실시간으로 갱신합니다.
        /// </summary>
        public void UpdateCount(int current, int max)
        {
            tooltip.UpdateCount(current, max);
        }

        /// <summary>
        /// 툴팁을 숨깁니다.
        /// </summary>
        public void Hide()
        {
            tooltip.Disable();
        }
    }
}
