using Code.Players.Cursors;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Sequences.Guide
{
    public enum GuideButton
    {
        None,
        Left,
        Right,
    }

    /// <summary>
    /// 안내 손의 겉모습입니다. 자기 오버레이 캔버스를 만들어 포인터·마우스 그림·클릭 링·끌기 상자·키캡을 그립니다.
    /// 좌표는 전부 스크린 픽셀입니다(캔버스에 스케일러가 없어 1픽셀 = 1단위).
    /// </summary>
    public sealed partial class GuideCursorView : MonoBehaviour
    {
        private const int SortingOrder = 5000;
        private const float PointerScale = 2f;
        private const float RingSeconds = 0.35f;
        private const float EdgeThickness = 2f;
        private static readonly Vector2 MouseOffset = new(52f, -52f); // 포인터 끝 기준. 오른쪽 아래
        private const float KeyScale = 1.6f;

        private static readonly Color PointerColor = new(1f, 1f, 1f, 0.9f);
        private static readonly Color BodyColor = new(1f, 1f, 1f, 0.95f);
        private static readonly Color ButtonIdleColor = new(1f, 1f, 1f, 0.2f);
        private static readonly Color ButtonPressedColor = new(1f, 0.55f, 0.15f, 1f);
        private static readonly Color LineColor = new(1f, 0.9f, 0.3f, 0.9f);
        private static readonly Color KeyColor = new(0.12f, 0.12f, 0.12f, 1f);

        private RectTransform _root;
        private RectTransform _pointer;
        private RectTransform _mouse;
        private RectTransform _keycap;
        private Image _ring;
        private Image _left;
        private Image _right;
        private Image _line;
        private TextMeshProUGUI _keyText;
        private Image _keyArrow;
        private readonly RectTransform[] _edges = new RectTransform[4];
        private float _ringT = -1f;

        public Vector2 Position { get; private set; }

        /// <summary>
        /// 오버레이 캔버스를 owner 밑에 만들어 돌려줍니다. 스테이지가 내려가면 같이 사라집니다.
        /// </summary>
        public static GuideCursorView Create(Transform owner, CursorSetSO cursors)
        {
            GameObject go = new("GuideCursor", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(owner, false);

            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            GuideCursorView view = go.AddComponent<GuideCursorView>();
            view.Build(cursors);
            return view;
        }

        private void Build(CursorSetSO cursors)
        {
            _root = (RectTransform)transform;

            for (int i = 0; i < _edges.Length; i++)
                _edges[i] = MakeImage("Edge", _root, GuideCursorArt.Solid(), Vector2.one, Vector2.zero, LineColor).rectTransform;

            _line = MakeImage("Line", _root, GuideCursorArt.Solid(), new Vector2(1f, 4f), new Vector2(0f, 0.5f), LineColor);
            _ring = MakeImage("Ring", _root, GuideCursorArt.Ring(64, 5), new Vector2(64f, 64f), new Vector2(0.5f, 0.5f), LineColor);

            _pointer = MakePointer(cursors);

            // 포인터의 피벗이 핫스팟이라 자식으로 두면 기준이 그림 모서리가 된다. 루트에 두고 SetPosition이 같이 옮긴다
            _mouse = MakeImage("Mouse", _root, GuideCursorArt.Mouse(MousePart.Body), new Vector2(32f, 44f) * 1.4f,
                new Vector2(0.5f, 0.5f), BodyColor).rectTransform;
            _left = MakeImage("Left", _mouse, GuideCursorArt.Mouse(MousePart.LeftButton), _mouse.sizeDelta, new Vector2(0.5f, 0.5f), ButtonIdleColor);
            _right = MakeImage("Right", _mouse, GuideCursorArt.Mouse(MousePart.RightButton), _mouse.sizeDelta, new Vector2(0.5f, 0.5f), ButtonIdleColor);
            CenterInParent(_left.rectTransform);
            CenterInParent(_right.rectTransform);

            _keycap = MakeImage("Keycap", _root, GuideCursorArt.RoundedBox(48, 8), new Vector2(48f, 48f), new Vector2(0.5f, 0.5f), BodyColor).rectTransform;
            _keyText = MakeKeyText(_keycap);
            _keyArrow = MakeImage("Arrow", _keycap, GuideCursorArt.ArrowRight(32), new Vector2(28f, 28f), new Vector2(0.5f, 0.5f), KeyColor);
            CenterInParent(_keyArrow.rectTransform);

            HideDrag();
            HideKey();
            SetButton(GuideButton.None, false);
            _ring.gameObject.SetActive(false);
        }

        private RectTransform MakePointer(CursorSetSO cursors)
        {
            Sprite sprite = null;
            Vector2 size = new(32f, 32f);
            if (cursors != null && cursors.TryGet(CursorKind.Default, out Texture2D texture, out Vector2 hotspot))
            {
                sprite = GuideCursorArt.Pointer(texture, hotspot);
                size = new Vector2(texture.width, texture.height);
            }

            Image image = MakeImage("Pointer", _root, sprite, size * PointerScale, Vector2.zero, PointerColor);
            if (sprite != null)
                image.rectTransform.pivot = sprite.pivot / size;

            return image.rectTransform;
        }

        public void SetPosition(Vector2 screen)
        {
            Position = screen;
            _pointer.anchoredPosition = screen;
            _mouse.anchoredPosition = screen + MouseOffset;
        }

        public void SetShown(bool shown)
            => gameObject.SetActive(shown);

        /// <summary>
        /// 마우스 그림에서 눌러야 할 버튼을 칠합니다. None이면 그림을 숨깁니다.
        /// </summary>
        public void SetButton(GuideButton button, bool pressed)
        {
            _mouse.gameObject.SetActive(button != GuideButton.None);
            _left.color = button == GuideButton.Left && pressed ? ButtonPressedColor : ButtonIdleColor;
            _right.color = button == GuideButton.Right && pressed ? ButtonPressedColor : ButtonIdleColor;
        }

        /// <summary>
        /// 포인터 끝에서 링이 퍼집니다.
        /// </summary>
        public void PlayRing()
        {
            _ring.rectTransform.anchoredPosition = Position;
            _ring.gameObject.SetActive(true);
            _ringT = 0f;
        }

        public void ShowDragBox(Vector2 a, Vector2 b)
        {
            Vector2 min = Vector2.Min(a, b);
            Vector2 max = Vector2.Max(a, b);
            Vector2 size = max - min;

            Place(_edges[0], new Vector2(min.x, max.y - EdgeThickness), new Vector2(size.x, EdgeThickness));
            Place(_edges[1], min, new Vector2(size.x, EdgeThickness));
            Place(_edges[2], min, new Vector2(EdgeThickness, size.y));
            Place(_edges[3], new Vector2(max.x - EdgeThickness, min.y), new Vector2(EdgeThickness, size.y));
            _line.gameObject.SetActive(false);
        }

        public void ShowDragLine(Vector2 a, Vector2 b)
        {
            Vector2 delta = b - a;
            _line.rectTransform.anchoredPosition = a;
            _line.rectTransform.sizeDelta = new Vector2(delta.magnitude, 4f);
            _line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            _line.gameObject.SetActive(true);
            foreach (RectTransform edge in _edges)
                edge.gameObject.SetActive(false);
        }

        public void HideDrag()
        {
            _line.gameObject.SetActive(false);
            foreach (RectTransform edge in _edges)
                edge.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_ringT < 0f)
                return;

            _ringT += Time.unscaledDeltaTime / RingSeconds;
            if (_ringT >= 1f)
            {
                _ringT = -1f;
                _ring.gameObject.SetActive(false);
                return;
            }

            _ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.6f, _ringT);
            Color color = LineColor;
            color.a *= 1f - _ringT;
            _ring.color = color;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.gameObject.SetActive(true);
        }

        private static void CenterInParent(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 좌하 앵커의 Image. anchoredPosition이 곧 스크린 픽셀이 됩니다.
        /// </summary>
        private static Image MakeImage(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 pivot, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false; // 진짜 클릭을 가로채면 안 된다

            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            return image;
        }

        private static TextMeshProUGUI MakeKeyText(RectTransform keycap)
        {
            GameObject go = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(keycap, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 22f;
            text.color = KeyColor;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }
    }
}
