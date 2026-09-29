using System.Text;
using Code.Audio;
using Code.Core;
using Code.Progression;
using Code.Stages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.MainMenu
{
    /// <summary>
    /// 영지 선택의 상세 패널입니다. 목록에서 고른 영지 하나를 크게 보여 주고, 출정을 실행합니다.
    /// </summary>
    public class StageDetailUI : MonoBehaviour, IUIElement<StageSO>
    {
        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI prerequisiteText;
        [SerializeField] private TextMeshProUGUI bestTimeText;
        [SerializeField] private TextMeshProUGUI launchLabel;

        [Header("References")]
        [SerializeField] private Image thumbnail;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private Button launchButton;

        [Header("Data")]
        [SerializeField] private StageDatabaseSO stageDatabase; // 선행 영지 이름을 찾기 위한 목록. 없으면 SceneId를 그대로 적습니다

        [Header("Text")]
        [SerializeField] private string prerequisitePrefix = "선행 전장  ";
        [SerializeField] private string noPrerequisiteText = "없음";
        [SerializeField] private string bestTimeFormat = "최고 기록  {0}";
        [SerializeField] private string comingSoonText = "아직 준비 중인 전장입니다.";
        [SerializeField] private string checkUnlockText = "해금 조건을 확인하세요.";
        [SerializeField] private string lockedByPrerequisiteFormat = "{0}을(를) 완료하면 해금됩니다.";

        private readonly StringBuilder _builder = new(64);

        private StageSO _stage;

        private void Awake()
        {
            if (launchButton != null)
                launchButton.onClick.AddListener(HandleLaunch);
        }

        private void OnDestroy()
        {
            if (launchButton != null)
                launchButton.onClick.RemoveListener(HandleLaunch);
        }

        /// <summary>
        /// 패널을 지정 영지로 채웁니다.
        /// </summary>
        public void EnableFor(StageSO stage)
        {
            _stage = stage;
            gameObject.SetActive(stage != null);

            if (stage == null)
                return;

            StageStatus status = StageStatuses.Of(stage);
            bool locked = status == StageStatus.Locked;

            // 이름과 지도는 잠겨도 보여 준다. 목록과 같은 이유다 — StageButtonUI 참고
            if (nameText != null)
                nameText.SetText(stage.DisplayName);

            if (stateText != null)
                stateText.SetText(TextFormat.StageState(status));

            if (descriptionText != null)
                descriptionText.SetText(BuildDescription(stage, locked));

            if (prerequisiteText != null)
                prerequisiteText.SetText(BuildPrerequisiteLine(stage));

            if (bestTimeText != null)
                bestTimeText.SetText(BuildBestTimeLine(stage, locked));

            if (thumbnail != null)
            {
                thumbnail.enabled = stage.Thumbnail != null;
                if (stage.Thumbnail != null)
                    thumbnail.sprite = stage.Thumbnail;
            }

            if (lockOverlay != null)
                lockOverlay.SetActive(locked);

            if (launchLabel != null)
                launchLabel.SetText(TextFormat.StageLaunch(status));

            if (launchButton != null)
                launchButton.interactable = !locked;
        }

        /// <summary>
        /// 패널을 비웁니다.
        /// </summary>
        public void Disable()
        {
            _stage = null;
            gameObject.SetActive(false);
        }

        private string BuildPrerequisiteLine(StageSO stage)
        {
            _builder.Clear();
            _builder.Append(prerequisitePrefix);

            if (stage.PrerequisiteStages == null || stage.PrerequisiteStages.Length == 0)
            {
                _builder.Append(noPrerequisiteText);
                return _builder.ToString();
            }

            for (int i = 0; i < stage.PrerequisiteStages.Length; i++)
            {
                if (i > 0)
                    _builder.Append(" · ");

                _builder.Append(NameOf(stage.PrerequisiteStages[i]));
            }

            return _builder.ToString();
        }

        private string BuildBestTimeLine(StageSO stage, bool locked)
        {
            float best = locked ? -1f : ProgressService.GetBestSeconds(stage.Scene);
            return string.Format(bestTimeFormat, TextFormat.Duration(best));
        }

        /// <summary>
        /// 설명과 잠금 사유를 한 칸에 담습니다.
        /// 예전에는 잠기면 사유가 설명을 통째로 덮어, 준비 중인 영지는 무엇을 하는 곳인지 끝내 알 수 없었습니다.
        /// 설명은 어느 상태에서나 보여 줍니다 — 다음에 무엇이 오는지가 잠금 화면의 존재 이유입니다.
        /// </summary>
        private string BuildDescription(StageSO stage, bool locked)
        {
            if (!locked)
                return stage.Description;

            string reason = BuildLockReason(stage);
            if (string.IsNullOrEmpty(stage.Description))
                return reason;

            return $"{stage.Description}\n\n{reason}";
        }

        private string BuildLockReason(StageSO stage)
        {
            if (stage.IsLocked)
                return comingSoonText;

            _builder.Clear();

            foreach (SceneId prerequisite in stage.PrerequisiteStages)
            {
                if (ProgressService.IsStageCleared(prerequisite))
                    continue;

                if (_builder.Length > 0)
                    _builder.Append(" · ");

                _builder.Append(NameOf(prerequisite));
            }

            return _builder.Length == 0
                ? checkUnlockText
                : string.Format(lockedByPrerequisiteFormat, _builder);
        }

        private string NameOf(SceneId scene)
        {
            if (stageDatabase == null)
                return scene.ToString();

            foreach (StageSO candidate in stageDatabase.Stages)
                if (candidate != null && candidate.Scene == scene)
                    return candidate.DisplayName;

            return scene.ToString();
        }

        private void HandleLaunch()
        {
            if (_stage == null || StageStatuses.Of(_stage) == StageStatus.Locked)
                return;

            if (MenuAudio.Instance != null)
                MenuAudio.Instance.ButtonClick();

            SceneLoader.Load(_stage);
        }
    }
}
