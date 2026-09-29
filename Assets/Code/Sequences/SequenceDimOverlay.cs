using Code.Sequences.Guide;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Sequences
{
    /// <summary>
    /// 강조된 UI 칸만 남기고 화면을 어둡게 덮습니다. 자기 오버레이 캔버스에 띠 넉 장을 두어 구멍 둘레를 채웁니다.
    /// 클릭은 가로채지 않습니다 — 강조 중에 월드를 눌러야 하는 스텝(자원 HUD를 짚으며 나무 우클릭)이 있습니다.
    /// 좌표는 전부 스크린 픽셀입니다(캔버스에 스케일러가 없어 1픽셀 = 1단위).
    /// </summary>
    public sealed class SequenceDimOverlay : MonoBehaviour
    {
        private const int SortingOrder = 90; // HUD(≤10)·경보(50) 위, 대사창(100)·안내 손(5000) 아래

        private readonly Image[] _bands = new Image[4];
        private readonly Vector3[] _corners = new Vector3[4];
        private RectTransform _target;
        private float _padding;

        /// <summary>
        /// 오버레이 캔버스를 owner 밑에 만들어 돌려줍니다. 스테이지가 내려가면 같이 사라집니다.
        /// </summary>
        public static SequenceDimOverlay Create(Transform owner, Color color)
        {
            GameObject go = new("SequenceDim", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(owner, false);

            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            SequenceDimOverlay overlay = go.AddComponent<SequenceDimOverlay>();
            overlay.Build(color);
            overlay.Hide();
            return overlay;
        }

        /// <summary>
        /// 지정 칸 둘레를 어둡게 덮습니다. 칸이 움직이거나 크기가 바뀌면 매 프레임 따라갑니다.
        /// </summary>
        public void Show(RectTransform target, float padding)
        {
            _target = target;
            _padding = padding;
            gameObject.SetActive(true);
            Layout();
        }

        /// <summary>
        /// 덮개를 걷습니다.
        /// </summary>
        public void Hide()
        {
            _target = null;
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                Hide();
                return;
            }

            Layout();
        }

        private void Build(Color color)
        {
            for (int i = 0; i < _bands.Length; i++)
            {
                GameObject go = new("Band", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);

                Image image = go.GetComponent<Image>();
                image.sprite = GuideCursorArt.Solid();
                image.color = color;
                image.raycastTarget = false;

                RectTransform rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                _bands[i] = image;
            }
        }

        // 위·아래 띠는 화면 폭 전체, 좌·우 띠는 구멍 높이만큼만 — 넉 장이 겹치지 않아 알파가 두 번 쌓이지 않는다
        private void Layout()
        {
            _target.GetWorldCorners(_corners);
            float width = Screen.width;
            float height = Screen.height;
            float left = _corners[0].x - _padding;
            float bottom = _corners[0].y - _padding;
            float right = _corners[2].x + _padding;
            float top = _corners[2].y + _padding;

            Place(_bands[0], 0f, 0f, width, bottom);
            Place(_bands[1], 0f, top, width, height - top);
            Place(_bands[2], 0f, bottom, left, top - bottom);
            Place(_bands[3], right, bottom, width - right, top - bottom);
        }

        private static void Place(Image band, float x, float y, float width, float height)
        {
            RectTransform rect = band.rectTransform;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
        }
    }
}
