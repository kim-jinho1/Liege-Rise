using Code.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Sequences
{
    /// <summary>
    /// 스텝이 짚어 주는 UI 칸을 반투명 박스로 덮고 나머지 화면을 어둡게 가립니다. 대상은 UiTargetResolver가
    /// 매 프레임 다시 찾으므로 패널이 바뀌면(Q를 눌러 건설 메뉴가 열리면) 박스와 구멍이 그리로 옮겨 갑니다.
    /// </summary>
    public class SequenceUIHighlighter : MonoSingleton<SequenceUIHighlighter>
    {
        [SerializeField] private Color highlightColor = new(1f, 0.85f, 0.2f, 0.45f);
        [SerializeField] private Color dimColor = new(0f, 0f, 0f, 0.6f); // 강조 칸 밖을 덮는 색
        [SerializeField] private float pulseSpeed = 4f;     // 알파가 오르내리는 속도
        [SerializeField] private float pulseDepth = 0.6f;   // 최대 알파 대비 얼마나 꺼지는가
        [SerializeField] private float padding = 6f;

        private readonly UiTargetResolver _resolver = new();
        private UIElementHighlightType _key = UIElementHighlightType.None;
        private RectTransform _target;
        private Image _box;
        private SequenceDimOverlay _dim;
        private bool _dimEnabled;

        /// <summary>
        /// 지정 UI 칸을 켭니다. 이전 강조는 자동 해제됩니다.
        /// dim은 그 칸 밖을 어둡게 가릴지입니다 — 월드도 눌러야 하는 스텝(채집·건설)에서 켜면 화면이 방해가 됩니다.
        /// </summary>
        public void Show(UIElementHighlightType key, bool dim = false)
        {
            Clear();
            _key = key;
            _dimEnabled = dim;
            if (_key == UIElementHighlightType.None)
                return;

            Track();
        }

        private void Update()
        {
            if (_key == UIElementHighlightType.None)
                return;

            Track();
            Pulse();
        }

        /// <summary>
        /// 지금 화면의 대상으로 박스를 옮깁니다. 대상이 사라지면 숨기고, 다시 나타나면 다시 붙입니다.
        /// </summary>
        private void Track()
        {
            _resolver.TryResolve(_key, out RectTransform rect);
            if (rect == _target && (_box == null || _box.gameObject.activeSelf == (rect != null)))
                return;

            _target = rect;
            if (_target == null)
            {
                if (_box != null)
                    Detach();
                if (_dim != null)
                    _dim.Hide();
                return;
            }

            if (_box == null)
                _box = CreateBox();
            if (_dim == null && _dimEnabled)
                _dim = SequenceDimOverlay.Create(transform, dimColor);

            RectTransform boxRect = _box.rectTransform;
            boxRect.SetParent(_target, false);
            boxRect.anchorMin = Vector2.zero;
            boxRect.anchorMax = Vector2.one;
            boxRect.offsetMin = new Vector2(-padding, -padding);
            boxRect.offsetMax = new Vector2(padding, padding);
            boxRect.SetAsLastSibling();
            _box.gameObject.SetActive(true);
            if (_dimEnabled)
                _dim.Show(_target, padding);
        }

        private void Pulse()
        {
            if (_box == null || !_box.gameObject.activeSelf)
                return;

            float wave = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
            Color color = highlightColor;
            color.a = highlightColor.a * (1f - pulseDepth * wave);
            _box.color = color;
        }

        private Image CreateBox()
        {
            GameObject go = new("SequenceUIHighlight", typeof(RectTransform), typeof(Image));
            Image image = go.GetComponent<Image>();
            image.color = highlightColor;
            image.raycastTarget = false; // 아래 버튼이 눌려야 한다

            // 대상에 레이아웃 그룹이 붙어 있으면 박스가 칸 하나로 잡혀 앵커가 통째로 무시된다 —
            // 할 일 칸(VerticalLayoutGroup)에서 높이 0으로 눌려 아무것도 안 보였다
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            return image;
        }

        /// <summary>
        /// 박스를 대상에서 떼어 자기 밑에 숨겨 둡니다. 대상이 파괴될 때 같이 사라지지 않게 하기 위해서입니다.
        /// </summary>
        private void Detach()
        {
            _box.gameObject.SetActive(false);
            _box.rectTransform.SetParent(transform, false);
        }

        /// <summary>
        /// 현재 강조를 해제합니다.
        /// </summary>
        public void Clear()
        {
            _key = UIElementHighlightType.None;
            _target = null;
            if (_box != null)
                Detach();
            if (_dim != null)
                _dim.Hide();
        }

        protected override void OnDestroy()
        {
            // 박스가 _Core의 HUD 밑에 붙어 있으면 스테이지 씬이 내려가도 남는다
            if (_box != null)
                Destroy(_box.gameObject);

            base.OnDestroy();
        }
    }
}
