using Code.Core;
using UnityEngine;

namespace Code.Players
{
    /// <summary>
    /// 그리드 선택 박스를 표시하는 뷰 클래스입니다.
    /// </summary>
    public class GridSelectionBoxView : MonoSingleton<GridSelectionBoxView>
    {
        [Header("Corners")]
        [SerializeField] private Transform cornerTopLeft;
        [SerializeField] private Transform cornerTopRight;
        [SerializeField] private Transform cornerBottomLeft;
        [SerializeField] private Transform cornerBottomRight;

        [Header("Fill")]
        [SerializeField] private SpriteRenderer fillRenderer;

        [Header("Feel")]
        [SerializeField] private float smoothSpeed = 18f; // 스냅 속도
        [SerializeField] private float appearDuration = 0.16f; // 등장 속도
        [SerializeField] private float overshoot = 1.70158f; // 튕기는 정도

        private Transform[] _corners;
        private SpriteRenderer[] _cornerRenderers;
        private Vector3[] _cornerBaseScale;
        private float[] _cornerBaseAlpha;
        private float _fillBaseAlpha;

        private bool _visible;
        private float _appearT;
        private Vector3 _targetMin;
        private Vector3 _targetMax;

        protected override void Awake()
        {
            base.Awake();
            _corners = new[] { cornerTopLeft, cornerTopRight, cornerBottomLeft, cornerBottomRight };
            _cornerRenderers = new SpriteRenderer[_corners.Length];
            _cornerBaseScale = new Vector3[_corners.Length];
            _cornerBaseAlpha = new float[_corners.Length];

            for (int i = 0; i < _corners.Length; i++)
            {
                if (_corners[i] == null)
                    continue;

                _cornerBaseScale[i] = _corners[i].localScale;
                _cornerRenderers[i] = _corners[i].GetComponent<SpriteRenderer>();
                _cornerBaseAlpha[i] = _cornerRenderers[i] != null ? _cornerRenderers[i].color.a : 1f;
            }

            if (fillRenderer != null)
                _fillBaseAlpha = fillRenderer.color.a;

            ApplyActive(false);
        }

        /// <summary>
        /// 선택 박스를 표시합니다.
        /// </summary>
        public void Show(Vector3 worldMin, Vector3 worldMax)
        {
            _targetMin = worldMin;
            _targetMax = worldMax;

            if (!_visible)
            {
                _visible = true;
                _appearT = 0f;
                ApplyActive(true);
                SnapCornersToTargets();
            }
        }

        /// <summary>
        /// 선택 박스를 숨깁니다.
        /// </summary>
        public void Hide()
        {
            if (!_visible)
                return;

            _visible = false;
            _appearT = 0f;
            ApplyActive(false);
        }

        private void LateUpdate()
        {
            if (!_visible)
                return;

            _appearT += Time.deltaTime;
            float n = appearDuration > 0f ? Mathf.Clamp01(_appearT / appearDuration) : 1f;
            float scaleFactor = EaseOutBack(n);

            float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);

            for (int i = 0; i < _corners.Length; i++)
            {
                Transform corner = _corners[i];
                if (corner == null)
                    continue;

                corner.position = Vector3.Lerp(corner.position, CornerTarget(i), t);
                corner.localScale = _cornerBaseScale[i] * scaleFactor;

                if (_cornerRenderers[i] != null)
                    SetAlpha(_cornerRenderers[i], _cornerBaseAlpha[i] * n);
            }

            UpdateFill(n);
        }

        /// <summary>
        /// 지정된 인덱스에 해당하는 코너의 목표 위치를 반환합니다.
        /// </summary>
        private Vector3 CornerTarget(int index)
        {
            return index switch
            {
                0 => new Vector3(_targetMin.x, _targetMax.y, 0f),
                1 => new Vector3(_targetMax.x, _targetMax.y, 0f),
                2 => new Vector3(_targetMin.x, _targetMin.y, 0f),
                _ => new Vector3(_targetMax.x, _targetMin.y, 0f),
            };
        }

        private void SnapCornersToTargets()
        {
            for (int i = 0; i < _corners.Length; i++)
                if (_corners[i] != null)
                    _corners[i].position = CornerTarget(i);
        }

        /// <summary>
        /// 부드럽게 이동 중인 코너 위치를 기준으로 채움 스프라이트의 위치/크기/투명도를 갱신합니다.
        /// </summary>
        private void UpdateFill(float alphaFactor)
        {
            if (fillRenderer == null || cornerBottomLeft == null || cornerTopRight == null)
                return;

            Vector3 bl = cornerBottomLeft.position;
            Vector3 tr = cornerTopRight.position;
            Vector3 center = (bl + tr) * 0.5f;
            center.z = fillRenderer.transform.position.z;

            fillRenderer.transform.position = center;
            fillRenderer.transform.localScale = new Vector3(Mathf.Abs(tr.x - bl.x), Mathf.Abs(tr.y - bl.y), 1f);
            SetAlpha(fillRenderer, _fillBaseAlpha * alphaFactor);
        }

        private void ApplyActive(bool active)
        {
            foreach (Transform corner in _corners)
                if (corner != null)
                    corner.gameObject.SetActive(active);

            if (fillRenderer != null)
                fillRenderer.gameObject.SetActive(active);
        }

        private static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            Color rendererColor = renderer.color;
            rendererColor.a = alpha;
            renderer.color = rendererColor;
        }

        /// <summary>
        /// 등장 시 살짝 튕기는 EaseOutBack 보간 곡선입니다.
        /// </summary>
        private float EaseOutBack(float x)
        {
            float c1 = overshoot;
            float c3 = c1 + 1f;
            float p = x - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }
    }
}
