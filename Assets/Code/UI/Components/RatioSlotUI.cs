using TMPro;
using UnityEngine;

namespace Code.UI.Components
{
    /// <summary>
    /// 수치 텍스트(현재/최대) 슬롯 UI입니다.
    /// </summary>
    public class RatioSlotUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI valueText;

        private const string ValueFormat = "{0} / {1}";

        private FloatingDeltaText _delta;

        /// <summary>
        /// 현재값과 최대값으로 수치 텍스트와 슬라이더를 갱신합니다.
        /// </summary>
        public void SetValue(int current, int max)
        {
            if (valueText == null)
                return;

            valueText.SetText(ValueFormat, current, max);
        }

        /// <summary>
        /// 변화량을 슬롯 위에 잠깐 띄웁니다. 자원 슬롯과 같은 표시를 씁니다.
        /// </summary>
        public void ShowDelta(int amount)
        {
            _delta ??= new FloatingDeltaText(valueText);
            _delta.Show(amount);
        }

        private void Update()
            => _delta?.Tick(Time.deltaTime);
    }
}
