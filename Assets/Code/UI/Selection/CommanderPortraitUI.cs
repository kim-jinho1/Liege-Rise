using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Code.UI.Selection
{
    /// <summary>
    /// 지휘관 초상 한 장입니다. 생김새는 프리팹이 전부 쥡니다 — 여기서는 칸을 채우기만 합니다.
    /// 좌클릭은 그 지휘관을 고르고, 우클릭은 지금 고른 분대를 그 밑으로 넣습니다.
    /// </summary>
    public sealed class CommanderPortraitUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("칸")]
        [SerializeField] private Image portrait;      // 지휘관 초상. 그림이 없으면 꺼진다
        [SerializeField] private Image flag;          // 깃발 색만 바뀐다. 흰 그림을 넣을 것
        [SerializeField] private TextMeshProUGUI ordinalText;
        [SerializeField] private TextMeshProUGUI countText;

        [Header("분대 수")]
        [SerializeField] private string countFormat = "{0} / {1}";
        [SerializeField] private Color countColor = new(0.93f, 0.94f, 0.92f, 1f);
        [SerializeField] private Color countFullColor = new(0.85f, 0.45f, 0.38f, 1f); // 더 못 받는 상태

        /// <summary>좌클릭 — 고르기입니다.</summary>
        public event Action Picked;

        /// <summary>우클릭 — 고른 분대를 배속입니다.</summary>
        public event Action Assigned;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                Picked?.Invoke();
            else if (eventData.button == PointerEventData.InputButton.Right)
                Assigned?.Invoke();
        }

        /// <summary>초상 그림입니다. 없으면 칸을 비웁니다.</summary>
        public void SetPortrait(Sprite icon)
        {
            if (portrait == null)
                return;

            portrait.sprite = icon;
            portrait.enabled = icon != null;
        }

        /// <summary>깃발 색과 번호입니다. 월드의 깃발과 같은 색이라 어느 부대인지 바로 이어집니다.</summary>
        public void SetFlag(Color color, int ordinal)
        {
            if (flag != null)
                flag.color = color;

            if (ordinalText != null)
                ordinalText.SetText(ordinal.ToString());
        }

        /// <summary>거느린 분대 수입니다. 꽉 차면 색이 바뀌어 더 못 받는 것을 알립니다.</summary>
        public void SetCount(int current, int max)
        {
            if (countText == null)
                return;

            countText.SetText(countFormat, current, max);
            countText.color = current >= max ? countFullColor : countColor;
        }
    }
}
