using Code.Commandable;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Selection
{
    /// <summary>
    /// 선택 패널을 건물 옆(오른쪽, 자리 없으면 왼쪽)에 붙여 두고, 선택을 놓으면 원래 자리로 돌립니다.
    /// 캔버스를 옮기지 않고 좌표만 바꾸므로 컴포넌트를 빼면 원래 하단 UI로 돌아갑니다.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class SelectionPanelFollower : MonoBehaviour
    {
        [Header("Target Panel")]
        [SerializeField] private RectTransform panel; // 옮길 패널. 비우면 자기 RectTransform을 쓴다

        [Header("Placement")]
        [SerializeField] private float horizontalGap = 32f; // 건물 옆면과 패널 사이 간격 (픽셀, 1920x1080 기준)
        [SerializeField] private float verticalOffset; // 건물 중심에서 위아래로 밀 거리 (픽셀)

        private const float ResizeThreshold = 1f; // px

        private float CanvasScale
            => _canvas != null ? _canvas.scaleFactor : 1f;

        private RectTransform _panel;
        private Canvas _canvas;
        private Camera _camera;

        private AbstractCommandable _target;
        private Collider2D _targetCollider;

        private Vector2 _homeAnchoredPosition;
        private bool _isDisplaced;
        private bool _placeOnRight;

        // 붙일 때 한 번 재 둔다 — 매 프레임 재면 ContentSizeFitter의 미세한 오차가 떨림이 된다
        private Vector2 _placementOffset;
        private Vector2 _measuredSize;

        /// <summary>
        /// 패널을 건물 옆에 붙입니다. 월드에 고정되어 카메라를 밀면 건물과 함께 화면 밖으로 나갑니다.
        /// </summary>
        public void Follow(AbstractCommandable target)
        {
            EnsureInitialized();

            if (_panel == null || target == null)
            {
                Release();
                return;
            }

            _target = target;
            _targetCollider = target.GetComponent<Collider2D>();
            _isDisplaced = true;

            // 크기를 먼저 확정해야 좌우를 제대로 고른다
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);

            Camera cam = ResolveCamera();
            if (cam == null)
                return;

            GetSideAnchors(cam, out Vector3 right, out Vector3 left);

            // 오른쪽에 통째로 들어가면 오른쪽. 고를 때 한 번만 판단한다 —
            // 매 프레임 다시 보면 카메라를 밀 때 패널이 건물 좌우로 튀어 다닌다
            _placeOnRight = right.x + (horizontalGap + _panel.rect.width) * CanvasScale <= Screen.width;

            Measure();
            Place(_placeOnRight ? right : left);
        }

        /// <summary>
        /// 패널을 원래 자리로 되돌립니다.
        /// </summary>
        public void Release()
        {
            _target = null;
            _targetCollider = null;
            if (!_isDisplaced)
                return;

            _isDisplaced = false;
            if (_panel != null)
                _panel.anchoredPosition = _homeAnchoredPosition;
        }

        private void LateUpdate()
        {
            if (!_isDisplaced)
                return;

            // 대상이 파괴됐다
            if (_target == null)
            {
                Release();
                return;
            }

            Reposition();
        }

        /// <summary>
        /// 패널과 캔버스를 초기화합니다.
        /// Awake에서 호출하면 _Core가 상주 씬이라 맵 씬의 카메라가 없으므로 LateUpdate에서 호출합니다.
        /// </summary>
        private void EnsureInitialized()
        {
            if (_panel != null)
                return;

            _panel = panel != null ? panel : GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            if (_panel != null)
                _homeAnchoredPosition = _panel.anchoredPosition;
        }

        /// <summary>
        /// 패널의 크기와 피벗을 고려해 건물 옆면에서 패널 원점까지의 화면 오프셋을 구합니다.
        /// </summary>
        private void Measure()
        {
            float scale = CanvasScale;

            _measuredSize = _panel.rect.size * scale;
            float halfWidth = _measuredSize.x * 0.5f;

            // 피벗이 가운데가 아닐 수 있다
            Vector2 pivotShift = new(
                (0.5f - _panel.pivot.x) * _measuredSize.x,
                (0.5f - _panel.pivot.y) * _measuredSize.y);

            float offsetX = (horizontalGap * scale + halfWidth) * (_placeOnRight ? 1f : -1f);

            _placementOffset = new Vector2(
                offsetX - pivotShift.x, verticalOffset * scale - pivotShift.y);
        }

        // 카메라가 움직인 만큼 다시 붙인다
        private void Reposition()
        {
            Camera cam = ResolveCamera();
            if (cam == null)
                return;

            // 내용이 실제로 바뀌었을 때만 다시 잰다
            if (Vector2.Distance(_panel.rect.size * CanvasScale, _measuredSize) > ResizeThreshold)
                Measure();

            GetSideAnchors(cam, out Vector3 right, out Vector3 left);
            Place(_placeOnRight ? right : left);
        }

        private void Place(Vector3 anchor)
        {
            _panel.position = new Vector3(
                anchor.x + _placementOffset.x,
                anchor.y + _placementOffset.y,
                _panel.position.z);
        }

        /// <summary>
        /// 건물의 좌우 화면 좌표를 구합니다. Collider2D가 없으면 건물 중심을 기준으로 합니다.
        /// </summary>
        private void GetSideAnchors(Camera cam, out Vector3 right, out Vector3 left)
        {
            Vector3 position = _target.transform.position;
            if (_targetCollider == null)
            {
                right = left = cam.WorldToScreenPoint(position);
                return;
            }

            Bounds bounds = _targetCollider.bounds;
            right = cam.WorldToScreenPoint(new Vector3(bounds.max.x, bounds.center.y, position.z));
            left = cam.WorldToScreenPoint(new Vector3(bounds.min.x, bounds.center.y, position.z));
        }

        /// <summary>
        /// 씬의 카메라를 구합니다. _Core는 상주 씬이라 Awake 때는 맵 씬의 카메라가 없으므로 LateUpdate에서 호출합니다.
        /// </summary>
        private Camera ResolveCamera()
        {
            // _Core는 상주 씬이라 Awake 때는 맵 씬의 카메라가 없다
            if (_camera == null)
                _camera = Camera.main;

            return _camera;
        }
    }
}
