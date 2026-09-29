using System;
using Code.Commandable;
using Code.Economics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Components
{
    /// <summary>
    /// 건설 항목 호버 시 이름·역할·필요 자원을 표시하는 툴팁입니다.
    /// 크기는 레이아웃 그룹이 결정하므로 RectTransform 크기를 그대로 사용합니다.
    /// </summary>
    public class BuildTooltip : TooltipBase
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private CostResourceBarUI[] resourceBars;

        protected override Vector2 ContentSize
            => Rect.rect.size;

        /// <summary>
        /// 대상의 이름·역할·필요 자원을 표시합니다.
        /// costColor로 자원 표기 색을 지정해 구매 불가 상태를 나타냅니다.
        /// </summary>
        public void SetContent(UnlockableSO target, Color costColor)
        {
            if (target == null)
                return;

            SetContent(target.Name, target.Description, target.Cost, costColor);
        }

        /// <summary>
        /// 이름·역할·필요 자원을 직접 받아 표시합니다.
        /// 커맨드에는 대응하는 UnlockableSO가 없는 것이 많아(대형·건설 취소 등) 이 경로를 씁니다.
        /// </summary>
        public void SetContent(string targetName, string description, ResourceCostSO cost, Color costColor)
        {
            nameText?.SetText(targetName ?? string.Empty);
            descriptionText?.SetText(description ?? string.Empty);

            ResourceAmount[] costs = cost != null ? cost.Costs : Array.Empty<ResourceAmount>();
            for (int i = 0; i < resourceBars.Length; i++)
            {
                if (i < costs.Length && costs[i].Resource != null && costs[i].Amount > 0)
                {
                    resourceBars[i].EnableFor(costs[i]);
                    resourceBars[i].SetColor(costColor);
                }
                else
                {
                    resourceBars[i].Disable();
                }
            }

            // ContentSize가 레이아웃 결과를 참조하므로 배치 전에 즉시 재계산한다.
            LayoutRebuilder.ForceRebuildLayoutImmediate(Rect);
        }
    }
}
