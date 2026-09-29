using TMPro;
using UnityEngine;

namespace Code.UI.Components
{
    /// <summary>
    /// 텍스트 한 덩어리를 표시하는 단순 툴팁입니다.
    /// 크기는 텍스트의 선호 크기에 여백을 더해 직접 계산합니다.
    /// </summary>
    public class Tooltip : TooltipBase
    {
        [SerializeField] private TextMeshProUGUI tooltipText;

        [Header("Padding")]
        [SerializeField] private float horizontalPadding = 50f; // 텍스트 좌우 여백
        [SerializeField] private float verticalPadding = 8f; // 텍스트 상하 여백

        private Vector2 _cachedSize;

        protected override Vector2 ContentSize
            => _cachedSize;

        /// <summary>
        /// 텍스트를 설정하고 비활성 상태에서도 크기를 즉시 계산합니다.
        /// </summary>
        public void SetText(string text)
        {
            if (tooltipText == null)
                return;

            tooltipText.SetText(text);
            tooltipText.ForceMeshUpdate(true);
            Vector2 preferred = tooltipText.GetPreferredValues(float.MaxValue, float.MaxValue);
            _cachedSize = new Vector2(preferred.x + horizontalPadding, preferred.y + verticalPadding);
            Rect.sizeDelta = _cachedSize;
        }
    }
}
