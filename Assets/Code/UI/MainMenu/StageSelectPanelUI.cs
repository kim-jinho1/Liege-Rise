using System.Collections.Generic;
using Code.Audio;
using Code.Stages;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.MainMenu
{
    /// <summary>
    /// 영지 선택 패널입니다. 목록 행을 생성하고, 고른 영지를 상세 패널에 넘깁니다.
    /// </summary>
    public class StageSelectPanelUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private StageDatabaseSO stageDatabase;

        [Header("References")]
        [SerializeField] private StageButtonUI stageButtonPrefab;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private StageDetailUI detailPanel;
        [SerializeField] private Button backButton;

        [Header("UI Elements")]
        [SerializeField] private GameObject overlay;

        private readonly List<StageButtonUI> _spawned = new();

        private void Awake()
        {
            if (backButton != null)
                backButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            Build();
            Refresh();
        }

        private void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(Close);
        }

        /// <summary>
        /// 영지 행을 한 번만 생성합니다.
        /// </summary>
        private void Build()
        {
            if (_spawned.Count > 0 || stageButtonPrefab == null || contentRoot == null || stageDatabase == null)
                return;

            // 씬에 놓인 행은 레이아웃을 눈으로 맞추려고 둔 미리보기다. 진짜 행은 여기서 만든다
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);

            foreach (StageSO stage in stageDatabase.Stages)
            {
                if (stage == null)
                    continue;

                StageButtonUI row = Instantiate(stageButtonPrefab, contentRoot);
                row.gameObject.SetActive(true);
                _spawned.Add(row);
            }
        }

        /// <summary>
        /// 행의 잠금 상태를 다시 계산하고 선택을 되살립니다. 클리어 직후 돌아왔을 때
        /// 자물쇠가 그대로 남지 않도록 패널을 열 때마다 호출합니다.
        /// </summary>
        private void Refresh()
        {
            if (stageDatabase == null)
                return;

            int index = 0;
            foreach (StageSO stage in stageDatabase.Stages)
            {
                if (stage == null || index >= _spawned.Count)
                    continue;

                _spawned[index].EnableFor(stage, Select);
                index++;
            }

            Select(PickDefault());
        }

        /// <summary>
        /// 처음 열었을 때 보여 줄 영지입니다. 이어서 할 곳이 없으면 목록의 첫 영지입니다.
        /// </summary>
        private StageSO PickDefault()
        {
            StageSO next = stageDatabase.GetNextPlayableStage();
            if (next != null)
                return next;

            foreach (StageSO stage in stageDatabase.Stages)
                if (stage != null)
                    return stage;

            return null;
        }

        /// <summary>
        /// 선택을 바꾸고 상세 패널을 갱신합니다.
        /// </summary>
        private void Select(StageSO stage)
        {
            foreach (StageButtonUI row in _spawned)
                row.SetSelected(row.Stage != null && row.Stage == stage);

            if (detailPanel != null)
                detailPanel.EnableFor(stage);
        }

        /// <summary>
        /// 영지 선택 패널을 닫습니다.
        /// </summary>
        private void Close()
        {
            if (MenuAudio.Instance != null)
                MenuAudio.Instance.ButtonClick();

            gameObject.SetActive(false);

            if (overlay != null)
                overlay.SetActive(false);
        }
    }
}
