using Code.Core.Bus;
using Code.Economics;
using Code.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Components
{
    /// <summary>
    /// 자원 슬롯 UI를 나타내는 컴포넌트입니다.
    /// </summary>
    public class ResourceSlotUI : MonoBehaviour, IUIElement<UserSupplies, Owner>
    {
        [Header("Resource Slot Settings")]
        [SerializeField] private ResourceSO supplySO;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI valueText;

        public ResourceSO SupplySO => supplySO;

        private bool _discovered;
        private FloatingDeltaText _delta;

        /// <summary>
        /// SO의 아이콘으로 슬롯을 초기화하고 초기 수치를 표시합니다.
        /// </summary>
        public void EnableFor(UserSupplies userSupplies, Owner owner)
        {
            if (supplySO == null)
                return;

            if (icon != null && supplySO.Icon != null)
                icon.sprite = supplySO.Icon;

            _discovered = false;
            UpdateValue(userSupplies.GetCurrentAmount(owner, supplySO));
        }

        /// <summary>
        /// 슬롯의 수치를 갱신합니다.
        /// </summary>
        public void UpdateValue(int amount)
        {
            if (!_discovered)
            {
                _discovered = true;
                gameObject.SetActive(true);
            }

            if (valueText != null)
                valueText.SetText(amount.ToString());
        }

        /// <summary>
        /// 변화량을 슬롯 위에 잠깐 띄웁니다.
        /// </summary>
        public void ShowDelta(int amount)
        {
            _delta ??= new FloatingDeltaText(valueText);
            _delta.Show(amount);
        }

        private void Update()
            => _delta?.Tick(Time.deltaTime);

        /// <summary>
        /// 슬롯을 초기 상태로 리셋합니다.
        /// </summary>
        public void Disable()
        {
            if (icon != null)
                icon.sprite = null;

            if (valueText != null)
                valueText.SetText(string.Empty);
        }
    }
}
