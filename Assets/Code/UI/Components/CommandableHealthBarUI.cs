using Code.Commandable;
using UnityEngine;

namespace Code.UI.Components
{
    /// <summary>
    /// 유닛 오브젝트 위에 WorldSpace로 표시되는 체력바입니다.
    /// 부모 오브젝트의 AbstractCommandable을 자동으로 찾아 초기화합니다.
    /// </summary>
    public class CommandableHealthBarUI : MonoBehaviour
    {
        [SerializeField] private ProgressBar healthBarUI;

        private AbstractCommandable _commandable;

        private void Start()
        {
            _commandable = GetComponentInParent<AbstractCommandable>();
            // AbstractCommandable.Start()에서 OnHealthUpdated를 발행하므로 구독 먼저 등록
            _commandable.OnHealthUpdated += UpdateHealthBar;

            // 선택 이벤트가 아니라 외곽선 신호를 본다 — 분대원은 개별 선택되지 않는다
            _commandable.OnSelectionVisualChanged += SetVisible;
            UpdateHealthBar(_commandable, _commandable.MaxHealth, _commandable.CurrentHealth);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_commandable != null)
            {
                _commandable.OnHealthUpdated -= UpdateHealthBar;
                _commandable.OnSelectionVisualChanged -= SetVisible;
            }
        }

        private void SetVisible(bool visible)
            => gameObject.SetActive(visible);

        private void UpdateHealthBar(AbstractCommandable commandable, int lastHealth, int newHealth)
        {
            healthBarUI.SetMaxProgress(commandable.MaxHealth);
            healthBarUI.SetProgress(newHealth);
        }
    }
}
