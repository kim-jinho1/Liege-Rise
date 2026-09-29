using System.Text;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Sequence;
using Code.GameEvents.System;
using Code.Managers;
using Code.Players;
using Code.Progression;
using Code.Sequences;
using Code.Stages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Results
{
    /// <summary>
    /// GameOverEvent를 구독하여 승패 패널을 표시하고, 재시작/다음 스테이지/메인 메뉴 동작을 연결합니다.
    /// </summary>
    public class GameOverResultUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private GameObject overlay;

        [Header("Buttons")]
        [SerializeField] private Button restartButton;          // 패배 패널
        [SerializeField] private Button mainMenuButton;         // 패배 패널
        [SerializeField] private Button nextStageButton;        // 승리 패널
        [SerializeField] private Button victoryRestartButton;   // 승리 패널. 없으면 그냥 안 뜬다
        [SerializeField] private Button victoryMainMenuButton;  // 승리 패널

        [Header("Result")]
        [SerializeField] private TextMeshProUGUI victoryStatsText;
        [SerializeField] private TextMeshProUGUI defeatStatsText;

        [Header("Result Text")]
        [SerializeField] private string timeLine = "경과 시간  {0}";
        [SerializeField] private string bestLine = "최고 기록  {0}";
        [SerializeField] private string newRecordLine = "신기록!";
        [SerializeField] private string killsLine = "적 처치  {0}";
        [SerializeField] private string razedLine = "적 건물 파괴  {0}";
        [SerializeField] private string lostLine = "잃은 분대  {0}";
        [SerializeField] private string gatheredLine = "모은 자원  {0}";

        [Header("Presentation")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
        [SerializeField, Min(0.8f)] private float popFrom = 0.92f;

        [Header("Data")]
        // 다음에 갈 스테이지를 찾는 레지스트리. 비워 두면 다음 스테이지 버튼이 뜨지 않습니다.
        [SerializeField] private StageDatabaseSO stageDatabase;

        private readonly StringBuilder _builder = new(160);
        private StageSO _nextStage;

        // 마무리 대사를 기다리는 동안만 살아 있는 구독. 값이 있으면 곧 띄울 승리 화면의 경과 시간이다
        private readonly BusSubscriptionScope _deferredWin = new();
        private float? _deferredElapsed;

        private void Awake()
        {
            Debug.Assert(victoryPanel != null, "Victory Panel reference is missing!");
            Debug.Assert(defeatPanel != null, "Defeat Panel reference is missing!");
            Debug.Assert(overlay != null, "Overlay reference is missing!");
            Debug.Assert(restartButton != null, "Restart Button reference is missing!");
            Debug.Assert(mainMenuButton != null, "Main Menu Button reference is missing!");

            defeatPanel.SetActive(false);
            victoryPanel.SetActive(false);

            if (nextStageButton != null)
            {
                nextStageButton.gameObject.SetActive(false);
                nextStageButton.onClick.AddListener(GoToNextStage);
            }

            restartButton.onClick.AddListener(RestartStage);
            mainMenuButton.onClick.AddListener(GoToMainMenu);
            // UnityEngine.Object에 ?. 를 쓰면 파괴된 대상을 살아 있다고 보고한다 — != null로 판정한다
            if (victoryRestartButton != null)
                victoryRestartButton.onClick.AddListener(RestartStage);

            if (victoryMainMenuButton != null)
                victoryMainMenuButton.onClick.AddListener(GoToMainMenu);
            Bus<GameOverEvent>.RegisterForAll(HandleGameOver);
        }

        private void OnDestroy()
        {
            if (nextStageButton != null)
                nextStageButton.onClick.RemoveListener(GoToNextStage);

            restartButton.onClick.RemoveListener(RestartStage);
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
            if (victoryRestartButton != null)
                victoryRestartButton.onClick.RemoveListener(RestartStage);

            if (victoryMainMenuButton != null)
                victoryMainMenuButton.onClick.RemoveListener(GoToMainMenu);
            Bus<GameOverEvent>.UnRegisterForAll(HandleGameOver);
            _deferredWin.Dispose();
        }

        /// <summary>
        /// 게임 오버 이벤트를 처리하여 승리 또는 패배 패널을 표시합니다.
        /// </summary>
        private void HandleGameOver(GameOverEvent evt)
        {
            bool won = evt.WinnerOwner == Owner.Player;

            // 클리어 기록(StageProgressRecorder)이 쓰는 값과 같은 시계를 읽는다.
            // Time.timeSinceLevelLoad는 스테이지가 additive로 얹혀 판마다 안 돌아간다
            float elapsed = RunStats.ElapsedSeconds;

            // 승패가 갈린 자리를 비춘다. 마지막 본거지가 화면 밖에서 무너지면 이긴 줄도 모른다
            if (won)
                FocusOnFallenBase();

            // 마무리 대사가 남아 있으면 그것부터 보여준다. 싸움은 여기서 이미 끝났으므로 시간은
            // 지금 멈춘다 — 대사를 읽는 동안 분대가 더 죽으면 이긴 판이 지는 것처럼 보인다.
            // 대사 타이핑은 unscaled로 도므로 멈춘 화면 위에서도 그대로 흐른다
            if (won && SequenceManager.Instance != null && SequenceManager.Instance.HasPendingSteps)
            {
                _deferredElapsed = elapsed;
                _deferredWin.Subscribe<AllObjectivesCompletedEvent>(Owner.Player, HandleSequenceFinished);
                Time.timeScale = 0f;
                return;
            }

            Show(won, elapsed);
        }

        /// <summary>
        /// 마지막으로 무너진 본거지를 비춥니다.
        /// </summary>
        private void FocusOnFallenBase()
        {
            if (HeadquartersBuildingManager.Instance == null)
                return;

            CameraController camera = FindAnyObjectByType<CameraController>();
            if (camera == null)
                return;

            camera.JumpTo(HeadquartersBuildingManager.Instance.LastFallenPosition);
        }

        /// <summary>
        /// 미뤄 둔 승리 화면을 마무리 대사가 끝난 뒤에 띄웁니다.
        /// </summary>
        private void HandleSequenceFinished(AllObjectivesCompletedEvent evt)
        {
            if (_deferredElapsed is not { } elapsed)
                return;

            _deferredElapsed = null;
            _deferredWin.Dispose();
            Show(true, elapsed);
        }

        /// <summary>
        /// 결과 패널을 채워 띄우고 게임 시간을 멈춥니다.
        /// </summary>
        private void Show(bool won, float elapsed)
        {
            GameObject panel = won ? victoryPanel : defeatPanel;
            if (panel == null)
                return;

            ShowNextStageButton(won);
            FillStats(won ? victoryStatsText : defeatStatsText, won, elapsed);

            overlay.SetActive(true);
            panel.SetActive(true);
            Time.timeScale = 0f;

            _ = PlayAsync(panel);
        }

        /// <summary>
        /// 이번 판의 기록과 전적을 한 덩어리로 채웁니다.
        /// </summary>
        private void FillStats(TextMeshProUGUI target, bool won, float elapsed)
        {
            if (target == null)
                return;

            _builder.Clear();
            _builder.AppendLine(string.Format(timeLine, TextFormat.Duration(elapsed)));

            if (won)
            {
                // 판정도 표시도 판 시작 시점의 기록으로 한다 — 지금 읽으면 방금 쓴 이번 판 기록이라 늘 신기록이 된다.
                // _Core는 한 번만 Awake하므로 여기서 직접 읽어 두면 두 번째 판부터 첫 판의 값에 굳는다
                float bestBefore = RunStats.BestBeforeRun;
                bool isRecord = bestBefore < 0f || elapsed < bestBefore;
                float best = isRecord ? elapsed : bestBefore;

                _builder.AppendLine(string.Format(bestLine, TextFormat.Duration(best)));

                if (isRecord)
                    _builder.AppendLine(newRecordLine);
            }

            _builder.AppendLine(string.Format(killsLine, RunStats.EnemyUnitsKilled));
            _builder.AppendLine(string.Format(razedLine, RunStats.EnemyBuildingsDestroyed));
            _builder.AppendLine(string.Format(lostLine, RunStats.SquadsLost));
            _builder.Append(string.Format(gatheredLine, RunStats.SuppliesGathered));

            target.SetText(_builder.ToString());
        }

        /// <summary>
        /// 패널을 흐림에서 띄우고 살짝 키웁니다. timeScale이 0이라 시간은 unscaled로 셉니다.
        /// </summary>
        private async Awaitable PlayAsync(GameObject panel)
        {
            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group == null)
                group = panel.AddComponent<CanvasGroup>();

            Transform panelTransform = panel.transform;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                float t = fadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeDuration);
                float eased = 1f - (1f - t) * (1f - t);

                group.alpha = eased;
                panelTransform.localScale = Vector3.one * Mathf.Lerp(popFrom, 1f, eased);

                await Awaitable.NextFrameAsync(destroyCancellationToken);
                elapsed += Time.unscaledDeltaTime;
            }

            group.alpha = 1f;
            panelTransform.localScale = Vector3.one;
        }

        /// <summary>
        /// 승리했고 다음에 갈 스테이지가 남아 있을 때만 다음 스테이지 버튼을 띄웁니다.
        /// </summary>
        private void ShowNextStageButton(bool won)
        {
            if (nextStageButton == null)
                return;

            // 목록 순서로 찾습니다. 클리어 기록(StageProgressRecorder)과 구독 순서가 엇갈려도 안전합니다.
            _nextStage = won && stageDatabase != null
                ? stageDatabase.GetStageAfter(SceneLoader.CurrentStage)
                : null;
            nextStageButton.gameObject.SetActive(_nextStage != null);
        }

        /// <summary>
        /// 게임 시간을 복구하고 현재 스테이지를 다시 로드합니다.
        /// </summary>
        private void RestartStage()
        {
            Time.timeScale = 1f;
            SceneLoader.ReloadCurrent();
        }

        /// <summary>
        /// 게임 시간을 복구하고 다음 스테이지로 이동합니다.
        /// </summary>
        private void GoToNextStage()
        {
            if (_nextStage == null)
                return;

            Time.timeScale = 1f;
            SceneLoader.Load(_nextStage);
        }

        /// <summary>
        /// 게임 시간을 복구하고 메인 메뉴 씬으로 이동합니다. private인 것이 중요합니다.
        /// public이면 인스펙터의 OnClick에 물릴 수 있고, 실제로 승리 패널의 다음 전장·재도전
        /// 버튼에 이 호출이 딸려 들어가 어느 버튼을 눌러도 타이틀로 돌아갔습니다.
        /// private이면 그 인스펙터 호출은 해석되지 않아 조용히 무시됩니다.
        /// </summary>
        private void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SceneLoader.Load(SceneId.MainMenu);
        }
    }
}
