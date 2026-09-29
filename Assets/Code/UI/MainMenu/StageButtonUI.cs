using System;
using Code.Audio;
using Code.Stages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.MainMenu
{
    /// <summary>
    /// 영지 목록의 한 행입니다. 누르면 씬을 로드하지 않고 선택만 알립니다 —
    /// 실제 출정은 상세 패널(StageDetailUI)이 맡습니다.
    /// </summary>
    public class StageButtonUI : MonoBehaviour, IUIElement<StageSO, Action<StageSO>>
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private Image thumbnail;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject selectMark;
        [SerializeField] private Button button;

        [Header("Selection")]
        [SerializeField] private Image frame;
        [SerializeField] private Color normalFrameColor = Color.white;
        [SerializeField] private Color selectedFrameColor = new Color(1f, 0.94f, 0.72f);

        private StageSO _stage;
        private Action<StageSO> _onSelected;

        /// <summary>
        /// 행을 스테이지 데이터로 채우고, 눌렀을 때 부를 선택 콜백을 물립니다.
        /// </summary>
        public void EnableFor(StageSO stage, Action<StageSO> onSelected)
        {
            _stage = stage;
            _onSelected = onSelected;

            StageStatus status = StageStatuses.Of(stage);
            bool locked = status == StageStatus.Locked;

            // 잠긴 영지도 이름과 지도를 보여 준다 — 목록은 캠페인의 전모를 알리는 자리이고,
            // 잠겼다는 것은 자물쇠와 상태 줄이 이미 말한다
            if (nameText != null)
                nameText.SetText(stage.DisplayName);

            if (stateText != null)
                stateText.SetText(TextFormat.StageState(status));

            if (thumbnail != null)
            {
                Sprite sprite = stage.ListThumbnail != null ? stage.ListThumbnail : stage.Thumbnail;
                thumbnail.enabled = sprite != null;
                if (sprite != null)
                    thumbnail.sprite = sprite;
            }

            if (lockOverlay != null)
                lockOverlay.SetActive(locked);

            SetSelected(false);

            if (button == null)
                return;

            // 잠긴 행도 누를 수는 있게 둡니다 — 상세가 해금 조건을 알려 주는 편이 친절합니다
            button.interactable = true;
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        /// <summary>
        /// 이 행이 지금 선택된 영지인지 표시합니다.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (selectMark != null)
                selectMark.SetActive(selected);

            if (frame != null)
                frame.color = selected ? selectedFrameColor : normalFrameColor;
        }

        /// <summary>
        /// 이 행이 담고 있는 스테이지입니다. 비어 있으면 null입니다.
        /// </summary>
        public StageSO Stage => _stage;

        /// <summary>
        /// 행을 비활성화하고 클릭 리스너를 해제합니다.
        /// </summary>
        public void Disable()
        {
            _stage = null;
            _onSelected = null;

            if (button != null)
            {
                button.interactable = false;
                button.onClick.RemoveListener(HandleClick);
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            if (_stage == null)
                return;

            // MonoSingleton은 자동 생성하지 않습니다. 메뉴 밖에서 열려도 클릭은 살아 있어야 합니다.
            if (MenuAudio.Instance != null)
                MenuAudio.Instance.ButtonClick();

            _onSelected?.Invoke(_stage);
        }
    }
}
