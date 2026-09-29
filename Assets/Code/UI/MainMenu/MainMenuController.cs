using Code.Audio;
using Code.Core;
using Code.Progression;
using Code.Stages;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.MainMenu
{
    /// <summary>
    /// 메인 메뉴의 버튼(새 게임/이어하기/설정/나가기) 동작을 연결합니다.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("Panels")]
        [SerializeField] private GameObject stageSelectPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject overlay;

        [Header("Data")]
        // 이어하기가 갈 스테이지를 찾는 레지스트리
        [SerializeField] private StageDatabaseSO stageDatabase;

        private void Awake()
        {
            if (newGameButton == null || settingsButton == null || exitButton == null)
                return;

            newGameButton.onClick.AddListener(OpenStageSelect);
            settingsButton.onClick.AddListener(OpenSettings);
            exitButton.onClick.AddListener(QuitGame);

            if (continueButton == null)
                return;

            continueButton.onClick.AddListener(ContinueGame);
            continueButton.interactable = ProgressService.HasAnyProgress;
        }

        private void Start()
        {
            CloseAllPanels();
        }

        private void OnDestroy()
        {
            if (newGameButton == null || settingsButton == null || exitButton == null)
                return;

            newGameButton.onClick.RemoveListener(OpenStageSelect);
            settingsButton.onClick.RemoveListener(OpenSettings);
            exitButton.onClick.RemoveListener(QuitGame);

            if (continueButton != null)
                continueButton.onClick.RemoveListener(ContinueGame);
        }

        /// <summary>
        /// 스테이지 선택 패널을 엽니다.
        /// </summary>
        public void OpenStageSelect()
        {
            MenuAudio.Instance.ButtonClick();
            if (stageSelectPanel == null || overlay == null)
                return;

            stageSelectPanel.SetActive(true);
            overlay.SetActive(true);
        }

        /// <summary>
        /// 다음 진행 가능한 스테이지로 바로 이동합니다. 없으면 스테이지 선택 패널을 엽니다.
        /// </summary>
        public void ContinueGame()
        {
            StageSO next = stageDatabase != null ? stageDatabase.GetNextPlayableStage() : null;
            if (next == null)
            {
                OpenStageSelect();
                return;
            }

            MenuAudio.Instance.ButtonClick();
            SceneLoader.Load(next);
        }

        /// <summary>
        /// 설정 패널을 엽니다.
        /// </summary>
        public void OpenSettings()
        {
            MenuAudio.Instance.ButtonClick();
            if (settingsPanel == null || overlay == null)
                return;

            settingsPanel.SetActive(true);
            overlay.SetActive(true);
        }

        /// <summary>
        /// 모든 하위 패널을 닫습니다.
        /// </summary>
        public void CloseAllPanels()
        {
            if (stageSelectPanel != null)
                stageSelectPanel.SetActive(false);

            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            if (overlay != null)
                overlay.SetActive(false);
        }

        /// <summary>
        /// 게임을 종료합니다(에디터에서는 플레이 모드를 중지합니다).
        /// </summary>
        public void QuitGame()
        {
            MenuAudio.Instance.ButtonClick();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
