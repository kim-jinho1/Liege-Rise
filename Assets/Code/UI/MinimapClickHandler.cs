using Code.Bootstraps;
using Code.Players;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Code.UI
{
    /// <summary>
    /// 미니맵 클릭/드래그로 카메라 타깃을 이동시킵니다.
    /// </summary>
    public class MinimapClickHandler : MonoBehaviour, IGameStartListener, IPointerDownHandler, IPointerUpHandler,
        IPointerExitHandler, IPointerMoveHandler
    {
        [SerializeField] private LayerMask floorMask;

        private Camera _minimapCamera;
        private CameraController _cameraController;
        private RectTransform _rectTransform;
        private bool _isMousedownOnMinimap;

        /// <summary>
        /// 매니저 초기화 이후 실행되도록 늦은 순서로 둡니다.
        /// </summary>
        public int InitOrder => 1000;

        private void Awake()
            => _rectTransform = GetComponent<RectTransform>();

        /// <summary>
        /// _Core와 스테이지가 모두 로드된 시점에 미니맵 카메라/카메라 컨트롤러를 해석합니다.
        /// </summary>
        public void OnGameStart()
        {
            if (MinimapCameraProvider.Instance != null)
                _minimapCamera = MinimapCameraProvider.Instance.Camera;

            _cameraController = FindAnyObjectByType<CameraController>();

            Debug.Assert(_minimapCamera != null && _cameraController != null,
                "Minimap camera or camera target could not be resolved at game start!");
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            _isMousedownOnMinimap = true;
            MoveVirtualCameraTarget(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            _isMousedownOnMinimap = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isMousedownOnMinimap = false;
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            MoveVirtualCameraTarget(eventData.position);
        }

        private void MoveVirtualCameraTarget(Vector2 mousePosition)
        {
            if (!_isMousedownOnMinimap || _minimapCamera == null || _cameraController == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, mousePosition, null,
                    out Vector2 localPoint))
                return;

            Rect rect = _rectTransform.rect;
            Vector2 viewportPoint = new(
                Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y));

            Ray cameraRay = _minimapCamera.ViewportPointToRay(viewportPoint);

            RaycastHit2D hit = Physics2D.GetRayIntersection(cameraRay, _minimapCamera.farClipPlane, floorMask);

            // 타깃이 Rigidbody2D라 Transform에 직접 쓰면 다음 프레임 HandlePanning이 옛 좌표로 되돌린다.
            if (hit.collider != null)
                _cameraController.JumpTo(hit.point);
        }
    }
}
