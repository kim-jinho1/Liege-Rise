using Code.Bootstraps;
using Code.Core;
using Code.Core.Bus;
using Code.Core.Input;
using Code.GameEvents.System;
using Code.Players;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Pause
{
    /// <summary>
    /// 인게임 일시정지 메뉴를 관리합니다. ESC로 정지/재개를 토글하고 Time.timeScale을 제어하며
    /// 재개·설정·재시작·메인 메뉴 동작을 연결합니다.
    /// 건설 패널이 열려 있거나 게임오버 상태일 때는 일시정지를 무시합니다.
    /// </summary>
    public class PauseMenuController : MonoBehaviour, IGameStartListener
    {
        [Header("Input")]
        [SerializeField] private InputReader inputReader;

        [Header("Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject overlay;
        [SerializeField] private GameObject settingsPanel;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        private bool _isPaused;
        private bool _buildPanelOpen;
        private bool _gameOver;

        // 스테이지 씬에 있어 씬 간 참조로는 연결할 수 없으므로 게임 시작 시점에 해석합니다.
        private SelectionController _selectionController;

        public int InitOrder => 1000;

        /// <summary>
        /// 스테이지 씬이 로드된 뒤 선택 컨트롤러를 찾아 연결합니다.
        /// _Core의 Awake 시점에는 스테이지가 아직 없으므로 여기서 해석해야 합니다.
        /// </summary>
        public void OnGameStart()
            => _selectionController = FindAnyObjectByType<SelectionController>();

        private void Awake()
        {
            inputReader.OnEscapeKeyPressed += HandleEscape;
            Bus<BuildPanelToggledEvent>.OnEvent[Owner.Player] += HandleBuildPanelToggled;
            Bus<GameOverEvent>.RegisterForAll(HandleGameOver);

            if (resumeButton != null)
                resumeButton.onClick.AddListener(HandleResume);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(HandleOpenSettings);

            if (restartButton != null)
                restartButton.onClick.AddListener(HandleRestartStage);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(HandleMainMenu);

            pausePanel.SetActive(false);
            if (overlay != null)
                overlay.SetActive(false);

            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            inputReader.OnEscapeKeyPressed -= HandleEscape;
            Bus<BuildPanelToggledEvent>.OnEvent[Owner.Player] -= HandleBuildPanelToggled;
            Bus<GameOverEvent>.UnRegisterForAll(HandleGameOver);

            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(HandleResume);

            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(HandleOpenSettings);

            if (restartButton != null)
                restartButton.onClick.RemoveListener(HandleRestartStage);

            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveListener(HandleMainMenu);

            // 일시정지 중 파괴(씬 전환)되는 경우를 대비해 시간 배율을 복구합니다.
            Time.timeScale = 1f;
        }

        private void HandleBuildPanelToggled(BuildPanelToggledEvent evt)
            => _buildPanelOpen = evt.IsOpen;

        private void HandleGameOver(GameOverEvent evt)
            => _gameOver = true;

        /// <summary>
        /// ESC 입력을 우선순위에 따라 처리합니다.
        /// 일시정지 중이면 재개, 건설 패널 열림·게임오버면 무시,
        /// 선택된 유닛/건물이 있으면 선택 해제, 그 외에는 일시정지합니다.
        /// </summary>
        private void HandleEscape()
        {
            if (_isPaused)
            {
                HandleResume();
                return;
            }

            if (_buildPanelOpen || _gameOver)
                return;

            // 선택된 유닛/건물이 있으면 먼저 선택을 해제하고(건물 선택 패널도 함께 닫힘),
            // 아무것도 선택되지 않은 상태에서 ESC를 한 번 더 눌러야 일시정지됩니다.
            if (_selectionController != null && _selectionController.HasSelection)
            {
                _selectionController.DeselectAllUnits();
                return;
            }

            Pause();
        }

        /// <summary>
        /// 게임을 일시정지하고 메뉴를 표시합니다.
        /// </summary>
        private void Pause()
        {
            _isPaused = true;
            Time.timeScale = 0f;

            if (overlay != null)
                overlay.SetActive(true);

            pausePanel.SetActive(true);
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        /// <summary>
        /// 게임을 재개하고 메뉴를 숨깁니다.
        /// </summary>
        private void HandleResume()
        {
            _isPaused = false;
            Time.timeScale = 1f;

            pausePanel.SetActive(false);
            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            if (overlay != null)
                overlay.SetActive(false);
        }

        /// <summary>
        /// 설정 패널을 열고 일시정지 메뉴를 숨깁니다.
        /// </summary>
        private void HandleOpenSettings()
        {
            if (settingsPanel == null)
                return;

            pausePanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        /// <summary>
        /// 설정 패널을 닫고 일시정지 메뉴로 돌아갑니다(설정 패널의 뒤로가기 버튼에 연결).
        /// </summary>
        public void ReturnToPause()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            if (overlay != null)
                overlay.SetActive(true);

            pausePanel.SetActive(true);
        }

        /// <summary>
        /// 시간 배율을 복구하고 현재 스테이지를 다시 로드합니다.
        /// </summary>
        private void HandleRestartStage()
        {
            Time.timeScale = 1f;
            SceneLoader.ReloadCurrent();
        }

        /// <summary>
        /// 시간 배율을 복구하고 메인 메뉴 씬으로 이동합니다.
        /// </summary>
        private void HandleMainMenu()
        {
            Time.timeScale = 1f;
            SceneLoader.Load(SceneId.MainMenu);
        }
    }
}
