using UnityEngine;
using UnityEngine.UI;

namespace Code.UI
{
    /// <summary>
    /// 메인 카메라가 지금 비추고 있는 영역을 미니맵 위에 테두리 사각형으로 표시합니다.
    /// 좌표 변환은 AssaultMinimapUI와 같은 경로(미니맵 카메라 뷰포트 → 컨테이너 로컬)를 씁니다.
    /// </summary>
    public class MinimapViewportUI : MonoBehaviour
    {
        [Header("Minimap")]
        [SerializeField] private RectTransform markerRoot; // 미니맵 이미지와 같은 영역을 덮는 컨테이너. 좌표 계산의 기준이다

        [Header("Frame")]
        [SerializeField] private RectTransform viewportFrame;
        [SerializeField] private Vector2 minSize = new(6f, 6f); // 너무 줄어들면 테두리가 한 점으로 뭉친다

        private Camera _minimapCamera;
        private Camera _mainCamera;

        private void Awake()
        {
            if (viewportFrame == null)
                return;

            // 로컬 좌표 계산이 중앙 기준이므로 앵커도 중앙으로 고정한다
            viewportFrame.anchorMin = viewportFrame.anchorMax = viewportFrame.pivot = new Vector2(0.5f, 0.5f);

            // 테두리가 커서를 먹으면 미니맵 클릭으로 카메라를 옮길 수 없다
            foreach (Graphic graphic in viewportFrame.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        private void LateUpdate()
        {
            if (markerRoot == null || viewportFrame == null)
                return;

            if (!EnsureCameras())
            {
                SetFrameVisible(false);
                return;
            }

            // 카메라가 비추는 월드 사각형. 직교 카메라라 z는 결과에 영향을 주지 않는다.
            Vector3 bottomLeft = _mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, _mainCamera.nearClipPlane));
            Vector3 topRight = _mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, _mainCamera.nearClipPlane));

            Vector3 minView = _minimapCamera.WorldToViewportPoint(bottomLeft);
            Vector3 maxView = _minimapCamera.WorldToViewportPoint(topRight);

            Rect rect = markerRoot.rect;
            Vector2 size = new(
                Mathf.Max(Mathf.Abs(maxView.x - minView.x) * rect.width, minSize.x),
                Mathf.Max(Mathf.Abs(maxView.y - minView.y) * rect.height, minSize.y));

            Vector2 center = new(
                ((minView.x + maxView.x) * 0.5f - 0.5f) * rect.width,
                ((minView.y + maxView.y) * 0.5f - 0.5f) * rect.height);

            viewportFrame.sizeDelta = size;
            viewportFrame.anchoredPosition = center;
            SetFrameVisible(true);
        }

        /// <summary>
        /// 미니맵 카메라와 메인 카메라를 확보합니다. 둘 다 이 컴포넌트보다 늦게 설 수 있습니다.
        /// </summary>
        private bool EnsureCameras()
        {
            if (_minimapCamera == null && MinimapCameraProvider.Instance != null)
                _minimapCamera = MinimapCameraProvider.Instance.Camera;

            // 메인 카메라는 스테이지 씬에 있어 판이 바뀌면 갈린다 — 없어지면 다시 찾는다
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            return _minimapCamera != null && _mainCamera != null;
        }

        /// <summary>
        /// 테두리 사각형의 표시 여부를 설정합니다.
        /// </summary>
        private void SetFrameVisible(bool visible)
        {
            if (viewportFrame.gameObject.activeSelf != visible)
                viewportFrame.gameObject.SetActive(visible);
        }
    }
}
