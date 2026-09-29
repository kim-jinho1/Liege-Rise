using Code.Bootstraps;
using Code.Core;
using Code.Players;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Code.UI.Alerts
{
    /// <summary>
    /// 화면에 잠깐 떴다 사라지는 경보 배너입니다. 클릭하면 경보가 가리키는 곳으로 카메라가 이동합니다.
    /// 이벤트를 직접 듣지 않습니다 — 무엇을 언제 알릴지는 부르는 쪽이 정합니다.
    /// </summary>
    public class AlertBannerUI : MonoBehaviour, IGameStartListener, IPointerClickHandler
    {
        [Header("Alert Banner Settings")]
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Alert Banner Duration")]
        [SerializeField, Min(0.5f)] private float duration = 4f;

        private readonly DelayedAction _hideDelay = new();

        private CameraController _cameraController;
        private Vector3 _focusPosition;
        private bool _hasFocus;

        /// <summary>
        /// CameraController가 스테이지 씬에 있으므로 늦게 초기화합니다.
        /// </summary>
        public int InitOrder => 1000;

        /// <summary>
        /// 스테이지가 올라온 뒤 카메라 컨트롤러를 찾습니다.
        /// </summary>
        public void OnGameStart()
            => _cameraController = FindAnyObjectByType<CameraController>();

        private void Awake()
            => Hide();

        private void OnDestroy()
            => _hideDelay.Dispose();

        /// <summary>
        /// 배너를 띄웁니다. focusPosition은 클릭했을 때 카메라가 갈 월드 좌표입니다.
        /// </summary>
        public void Show(string message, Vector3 focusPosition)
        {
            _focusPosition = focusPosition;
            _hasFocus = true;
            Display(message);
        }

        /// <summary>
        /// 카메라 이동 없이 배너만 띄웁니다.
        /// </summary>
        public void Show(string message)
        {
            _hasFocus = false;
            Display(message);
        }

        /// <summary>
        /// 배너를 즉시 숨깁니다.
        /// </summary>
        public void Hide()
        {
            _hideDelay.Cancel();

            if (root != null)
                root.SetActive(false);
        }

        /// <summary>
        /// 배너를 클릭하면 경보 지점으로 카메라를 옮기고 배너를 닫습니다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_hasFocus || _cameraController == null)
                return;

            _cameraController.JumpTo(_focusPosition);
            Hide();
        }

        /// <summary>
        /// 배너를 화면에 띄웁니다. 새 경보가 오면 표시 시간이 처음부터 다시 시작됩니다.
        /// </summary>
        private void Display(string message)
        {
            if (root == null || messageText == null)
                return;

            messageText.SetText(message);
            root.SetActive(true);

            // 새 경보가 오면 표시 시간이 처음부터 다시 간다
            _hideDelay.Restart(duration, Hide);
        }
    }
}
