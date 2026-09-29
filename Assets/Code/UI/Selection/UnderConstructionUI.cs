using System;
using System.Threading;
using Code.Buildings.Construction;
using Code.UI.Components;
using TMPro;
using UnityEngine;

namespace Code.UI.Selection
{
    /// <summary>
    /// 건설 중인 건물을 선택했을 때 건설 진행도를 표시합니다.
    /// 아이콘·이름·체력은 공용 정보 패널(GeneralInfoUI)이 전담합니다.
    /// </summary>
    public class UnderConstructionUI : MonoBehaviour, IUIElement<ConstructionDummy>
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject underConstructionPane;
        [SerializeField] private ProgressBar progressBar;
        [SerializeField] private TextMeshProUGUI progressText;

        private CancellationTokenSource _cts;

        private const string ProgressFormat = "{0:0} %";

        private void Awake()
        {
            Debug.Assert(underConstructionPane != null, "Under Construction Pane is not assigned.");
            Debug.Assert(progressBar != null, "Progress Bar is not assigned.");
            Debug.Assert(progressText != null, "Progress Text is not assigned.");
        }

        /// <summary>
        /// 대상 건설 더미의 진행도 표시를 시작합니다.
        /// </summary>
        public void EnableFor(ConstructionDummy dummy)
        {
            underConstructionPane.SetActive(true);
            progressBar?.SetMaxProgress(1f);

            CancelProgress();
            RenderProgress(ResolveProgress(dummy));

            _cts = new CancellationTokenSource();
            AnimateAsync(dummy, _cts.Token);
        }

        /// <summary>
        /// 진행도 표시를 중단하고 패널을 숨깁니다.
        /// </summary>
        public void Disable()
        {
            CancelProgress();
            underConstructionPane.SetActive(false);
        }

        private void OnDestroy()
        {
            CancelProgress();
        }

        private void CancelProgress()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async void AnimateAsync(ConstructionDummy dummy, CancellationToken token)
        {
            while (!token.IsCancellationRequested && dummy != null && dummy.ProgressData.Progress < 1f)
            {
                RenderProgress(ResolveProgress(dummy));

                try
                {
                    await Awaitable.NextFrameAsync(token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 현재 더미의 진행도를 반환합니다. 진행률은 러너가 누적 갱신하는 저장값이 기준입니다.
        /// </summary>
        private static float ResolveProgress(ConstructionDummy dummy)
        {
            return Mathf.Clamp01(dummy.ProgressData.Progress);
        }

        /// <summary>
        /// 진행도 바, 퍼센트 텍스트, 남은 시간 텍스트를 갱신합니다.
        /// 남은 시간은 현재 건설 속도(작업 폰 수)를 반영하며, 일시정지 시 1배속 기준으로 표시합니다.
        /// </summary>
        private void RenderProgress(float progress)
        {
            progressBar?.SetProgress(progress);
            progressText?.SetText(ProgressFormat, progress * 100f);
        }
    }
}
