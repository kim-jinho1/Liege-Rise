using Code.Objectives;
using Code.UI.Components;
using TMPro;
using UnityEngine;

namespace Code.UI.Sequence
{
    /// <summary>
    /// 지금 무엇을 해야 하는지 화면에 계속 띄워 둡니다.
    /// 대사는 지나가지만 목표는 남아야, 대사를 놓친 사람도 따라올 수 있습니다.
    /// </summary>
    public class ObjectiveHUD : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI objectiveText;

        [Header("진행도")]
        [SerializeField] private GameObject progressGroup; // 셀 것이 있는 목표에서만 켜진다. "병영을 지으세요"에 0/1을 띄우면 방해다
        [SerializeField] private ProgressBar progressBar;
        [SerializeField] private TextMeshProUGUI progressText;

        private ConditionTracker _tracker;

        /// <summary>튜토리얼 강조가 감쌀 칸입니다. 컴포넌트가 붙은 루트가 아니라 실제로 보이는 패널입니다.</summary>
        public RectTransform PanelRect
            => panel != null ? panel.transform as RectTransform : transform as RectTransform;

        private void Start()
        {
            if (panel == null || objectiveText == null)
            {
                // 배선이 빠지면 목표가 통째로 안 보인다. 조용히 넘어가면 원인을 찾는 데 한참 걸린다
                Debug.LogWarning($"[{name}] 패널이나 목표 텍스트가 배선되지 않아 목표 표시를 끕니다.", this);
                enabled = false;
                return;
            }

            // 매니저가 _Core에 있어 이 컴포넌트보다 늦게 깨어날 수 있으므로 Start에서 붙는다
            if (ObjectiveManager.Instance != null)
                ObjectiveManager.Instance.ActiveChanged += HandleActiveChanged;

            HandleActiveChanged();
        }

        private void OnDestroy()
        {
            if (ObjectiveManager.Instance != null)
                ObjectiveManager.Instance.ActiveChanged -= HandleActiveChanged;

            Unbind();
        }

        private void HandleActiveChanged()
        {
            Unbind();

            ObjectiveManager manager = ObjectiveManager.Instance;
            ObjectiveSO objective = manager != null ? manager.Active : null;

            string line = objective != null ? objective.Description : null;
            if (string.IsNullOrWhiteSpace(line))
            {
                // 설명이 비면 띄울 말이 없다. 제목은 에디터용 이름이라 그대로 보여 주지 않는다
                panel.SetActive(false);
                return;
            }

            panel.SetActive(true);
            objectiveText.SetText(line);

            _tracker = manager.ActiveTracker;
            if (_tracker != null)
                _tracker.OnProgress += RefreshProgress;

            RefreshProgress();
        }

        private void RefreshProgress()
        {
            if (progressGroup == null)
                return;

            bool show = _tracker is { HasProgress: true };
            progressGroup.SetActive(show);

            if (!show || progressBar == null || progressText == null)
                return;

            int current = Mathf.Clamp(_tracker.Current, 0, _tracker.Target);
            progressBar.SetMaxProgress(_tracker.Target);
            progressBar.SetProgress(current);
            progressText.text = TextFormat.ObjectiveProgress(current, _tracker.Target);
        }

        private void Unbind()
        {
            if (_tracker != null)
                _tracker.OnProgress -= RefreshProgress;

            _tracker = null;
        }
    }
}
