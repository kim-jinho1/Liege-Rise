using Code.Commandable;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Components
{
    /// <summary>
    /// 분대 패널의 한 칸입니다. 아이콘과 체력바를 들고 있고 지휘관 칸도 같은 프리팹을 씁니다.
    /// </summary>
    public class SquadMemberSlotUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image icon;
        [SerializeField] private ProgressBar healthBar;
        [SerializeField] private Image commanderMark;   // 지휘관 칸 표식. 없어도 동작합니다

        /// <summary>
        /// 칸에 대상을 꽂습니다. 아이콘이 없으면 이미지를 숨겨 흰 사각형이 남지 않게 합니다.
        /// </summary>
        public void Bind(Sprite sprite, bool isCommander)
        {
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.enabled = sprite != null;
            }

            if (commanderMark != null)
                commanderMark.enabled = isCommander;
        }

        /// <summary>
        /// 체력바를 갱신합니다. 최대치도 매번 넣습니다 — Start 전이면 0이라 바가 빈 채로 굳습니다.
        /// </summary>
        public void SetHealth(AbstractCommandable target)
        {
            if (healthBar == null)
                return;

            if (target == null || target.IsDead)
            {
                healthBar.SetMaxProgress(1f);
                healthBar.SetProgress(0f);
                return;
            }

            healthBar.SetMaxProgress(target.MaxHealth);
            healthBar.SetProgress(target.CurrentHealth);
        }
    }
}
