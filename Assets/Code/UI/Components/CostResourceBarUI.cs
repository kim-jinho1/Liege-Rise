using Code.Economics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Components
{
    public class CostResourceBarUI : MonoBehaviour, IUIElement<ResourceAmount>
    {
        [Header("UI Elements")]
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI count;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;

        private const string ColonFormat = ": {0}";

        private void Awake()
        {
            Debug.Assert(icon != null || count != null, $"[{name}] icon or count is null!", this);
        }

        public void EnableFor(ResourceAmount cost)
        {
            icon.sprite = cost.Resource.Icon;
            count.SetText(ColonFormat, cost.Amount);
            SetColor(normalColor);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 자원 아이콘과 수량 텍스트의 색을 일괄 변경합니다.
        /// 소속 아이템의 활성/비활성 상태에 맞춰 회색 처리할 때 사용합니다.
        /// </summary>
        public void SetColor(Color color)
        {
            icon.color = color;
            count.color = color;
        }

        public void Disable()
        {
            icon.sprite = null;
            count.SetText(string.Empty);
            gameObject.SetActive(false);
        }
    }
}
