using Code.Commandable;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Components
{
    /// <summary>
    /// 선택된 대상의 공용 정보(아이콘·이름·체력·체력바)를 표시하는 패널입니다.
    /// 씬에 하나만 존재하며, RuntimeUI가 선택 대상에 따라 Bind/Unbind를 호출합니다.
    /// 각 고유 패널(건설 중·유닛 생산 등)은 이 정보를 직접 표시하지 않습니다.
    /// </summary>
    public class GeneralInfoUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private ProgressBar healthBar;

        private AbstractCommandable _target;
        private const string HealthFormat = "{0} / {1}";

        private GameObject Panel
            => panel != null ? panel : gameObject;

        /// <summary>
        /// 대상을 연결하고 아이콘·이름·체력 표시를 시작합니다.
        /// 기존 대상이 있으면 먼저 구독을 해제합니다.
        /// </summary>
        public void Bind(AbstractCommandable target)
        {
            Unsubscribe();
            if (target == null)
                return;

            _target = target;
            _target.OnHealthUpdated += HandleHealthUpdated;

            AbstractCommandableSO so = target.AbstractCommandableSO;
            SetIcon(so != null ? so.Icon : null);
            nameText?.SetText(so != null ? so.Name : string.Empty);

            Refresh(target.CurrentHealth, target.MaxHealth);

            Panel.SetActive(true);
        }

        /// <summary>
        /// 구독을 해제하고 표시 내용을 비운 뒤 패널을 숨깁니다.
        /// </summary>
        public void Unbind()
        {
            Unsubscribe();

            SetIcon(null);
            nameText?.SetText(string.Empty);
            healthText?.SetText(string.Empty);
            if (healthBar != null)
                healthBar.SetProgress(0f);

            Panel.SetActive(false);
        }

        private void OnDestroy()
            => Unsubscribe();

        private void Unsubscribe()
        {
            if (_target != null)
                _target.OnHealthUpdated -= HandleHealthUpdated;

            _target = null;
        }

        private void HandleHealthUpdated(AbstractCommandable commandable, int lastHealth, int newHealth)
            => Refresh(newHealth, commandable.MaxHealth);

        private void Refresh(int current, int max)
        {
            if (healthText != null)
                healthText.SetText(HealthFormat, current, max);

            if (healthBar == null)
                return;

            // 최대치도 매번 넣는다. Bind 시점에 Start 전이면 MaxHealth가 0이라 바가 빈 채로 굳는다
            healthBar.SetMaxProgress(max);
            healthBar.SetProgress(current);
        }

        /// <summary>
        /// 아이콘을 설정합니다. 스프라이트가 없으면 이미지를 숨깁니다.
        /// </summary>
        private void SetIcon(Sprite icon)
        {
            if (iconImage == null)
                return;

            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
    }
}
