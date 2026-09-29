using System.Collections.Generic;
using System.Threading;
using Code.Buildings.Interfaces;
using Code.Commandable;
using Code.UI.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Selection
{
    /// <summary>
    /// 생산 중인 건물을 선택했을 때 대기 목록과 진행 중인 생산을 표시합니다.
    /// 아이콘·이름·체력은 공용 정보 패널(GeneralInfoUI)이 전담합니다.
    /// 시간 기반(유닛)·폰 도착 기반(분대) 차이는 IProductionDisplay가 흡수합니다.
    /// </summary>
    public class GenerateUnitUI : MonoBehaviour, IUIElement<IProductionDisplay>
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject generateUnitPanel;

        /// <summary>
        /// 지금 표시 중인지 여부입니다. 생산 정보가 없는 대상이면 EnableFor를 받고도 false입니다.
        /// </summary>
        public bool IsShowing => generateUnitPanel != null && generateUnitPanel.activeSelf;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private ProgressBar progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private BuildingQueueButtonUI[] queueButtons;

        private IProductionDisplay _production;
        private CancellationTokenSource _cts;

        private const string ProgressFormat = "{0:0} %";

        private void Awake()
        {
            // 큰 아이콘도 눌러서 취소한다. 대기 칸만 취소되고 진행 중인 것은 안 되면 왜 안 되는지 알 수 없다.
            // 프리팹에 버튼이 없어 여기서 붙인다 — 배선 없이 따라오게
            if (icon == null)
                return;

            icon.raycastTarget = true; // 씬의 아이콘은 레이캐스트가 꺼져 있어 버튼을 붙여도 클릭이 안 닿는다

            Button iconButton = icon.GetComponent<Button>();
            if (iconButton == null)
            {
                iconButton = icon.gameObject.AddComponent<Button>();
                iconButton.targetGraphic = icon;
            }

            iconButton.onClick.AddListener(CancelCurrent);
        }

        private void CancelCurrent()
            => _production?.CancelCurrent();

        /// <summary>
        /// 생산 정보를 표시하고 진행바 갱신을 시작합니다.
        /// </summary>
        public void EnableFor(IProductionDisplay production)
        {
            Unsubscribe();
            _production = production;
            if (_production == null)
            {
                Disable();
                return;
            }

            _production.ProductionChanged += HandleProductionChanged;
            generateUnitPanel.SetActive(true);
            progressBar?.SetMaxProgress(1f);
            Refresh();
        }

        /// <summary>
        /// UI를 비활성화하고 진행바 갱신을 중단합니다.
        /// </summary>
        public void Disable()
        {
            Unsubscribe();
            CancelProgress();
            ResetProgress();

            _production = null;
            generateUnitPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            CancelProgress();
        }

        private void Unsubscribe()
        {
            if (_production != null)
                _production.ProductionChanged -= HandleProductionChanged;
        }

        private void HandleProductionChanged()
        {
            if (_production != null)
                Refresh();
        }

        /// <summary>
        /// 대기 버튼과 진행 표시를 현재 생산 상태에 맞춰 갱신합니다.
        /// 생산 중이 아니면 진행 표시를 초기화하고 숨깁니다.
        /// </summary>
        private void Refresh()
        {
            SetupQueueButtons();

            if (_production.CurrentItem != null)
            {
                SetProductionVisible(true);
                StartProgressAsync();
            }
            else
            {
                CancelProgress();
                ResetProgress();
                SetProductionVisible(false);
            }
        }

        private void SetProductionVisible(bool visible)
        {
            icon.gameObject.SetActive(visible);
            nameText?.gameObject.SetActive(visible);
            progressBar?.gameObject.SetActive(visible);
        }

        private void StartProgressAsync()
        {
            CancelProgress();
            _cts = new CancellationTokenSource();

            UnlockableSO current = _production.CurrentItem;
            nameText?.SetText(current != null ? current.Name : string.Empty);
            icon.sprite = current != null ? current.Icon : null;

            UpdateProgressAsync(_cts.Token);
        }

        private void CancelProgress()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>
        /// 진행바와 진행 텍스트를 초기 상태(0%)로 되돌리고 대상 정보를 비웁니다.
        /// </summary>
        private void ResetProgress()
        {
            progressBar?.SetProgress(0f);
            progressText?.SetText(ProgressFormat, 0f);
            nameText?.SetText(string.Empty);
            icon.sprite = null;
        }

        private async void UpdateProgressAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _production?.CurrentItem != null)
            {
                float progress = Mathf.Clamp01(_production.Progress);
                progressBar?.SetProgress(progress);
                progressText?.SetText(ProgressFormat, progress * 100f);

                try
                {
                    await Awaitable.NextFrameAsync(token);
                }
                catch (System.OperationCanceledException)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 대기 목록을 버튼에 채웁니다. 버튼 클릭 시 해당 주문을 취소합니다.
        /// </summary>
        private void SetupQueueButtons()
        {
            IReadOnlyList<UnlockableSO> queued = _production.QueuedItems;
            int count = queued?.Count ?? 0;

            int i = 0;
            for (; i < count && i < queueButtons.Length; i++)
            {
                int index = i;
                queueButtons[i].gameObject.SetActive(true);
                queueButtons[i].EnableFor(queued[i], () => _production.CancelQueued(index));
            }

            for (; i < queueButtons.Length; i++)
            {
                queueButtons[i].gameObject.SetActive(true);
                queueButtons[i].Disable();
            }
        }
    }
}
