using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Components
{
    /// <summary>
    /// 수치 텍스트 위에 변화량을 잠깐 띄우는 표시입니다.
    /// 원본 텍스트를 런타임에 복제해 폰트·크기·정렬을 그대로 물려받습니다.
    /// </summary>
    public class FloatingDeltaText
    {
        private const float Duration = 0.9f;
        private const float Rise = 28f;
        private const float MergeWindow = 0.3f;
        private const string CloneName = "FloatingDelta";

        private static readonly Color GainColor = new(0.45f, 0.92f, 0.45f, 1f);
        private static readonly Color SpendColor = new(0.95f, 0.42f, 0.42f, 1f);

        private readonly TextMeshProUGUI _source;

        private TextMeshProUGUI _label;
        private RectTransform _rect;
        private Vector2 _origin;
        private float _elapsed;
        private bool _running;
        private int _pending;
        private int _shown;

        /// <summary>
        /// 복제 원본이 될 수치 텍스트를 받습니다.
        /// </summary>
        public FloatingDeltaText(TextMeshProUGUI source)
            => _source = source;

        /// <summary>
        /// 변화량을 예약합니다. 한 프레임에 여러 번 들어오면 합쳐서 한 번만 띄웁니다.
        /// </summary>
        public void Show(int amount)
            => _pending += amount;

        /// <summary>
        /// 표시를 한 프레임 진행합니다. 소유한 MonoBehaviour가 매 프레임 불러 줍니다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_pending != 0)
                Commit();

            if (!_running)
                return;

            _elapsed += deltaTime;
            float t = _elapsed / Duration;

            if (t >= 1f)
            {
                _running = false;
                _label.gameObject.SetActive(false);
                return;
            }

            _rect.anchoredPosition = _origin + new Vector2(0f, Rise * t);

            Color color = _label.color;
            color.a = 1f - t * t; // 끝에 가까울수록 빠르게 옅어진다
            _label.color = color;
        }

        private void Commit()
        {
            int amount = _pending;
            _pending = 0;

            if (_source == null)
                return;

            // 방금 띄운 표시가 아직 생생하면 거기에 더한다. 연달아 죽는 것이 -1 -1로 흩어지지 않는다.
            if (_running && _elapsed <= MergeWindow)
                amount += _shown;

            EnsureLabel();
            if (_label == null)
                return;

            if (amount == 0)
            {
                _running = false;
                _label.gameObject.SetActive(false);
                return;
            }

            _shown = amount;
            _label.color = amount > 0 ? GainColor : SpendColor;
            _label.SetText(amount > 0 ? "+" + amount : amount.ToString());
            _label.gameObject.SetActive(true);

            _rect.anchoredPosition = _origin;
            _elapsed = 0f;
            _running = true;
        }

        /// <summary>
        /// 표시용 텍스트를 1회 만들어 둡니다.
        /// </summary>
        private void EnsureLabel()
        {
            if (_label != null)
                return;

            RectTransform sourceRect = _source.rectTransform;

            // 첫 표시가 활성화와 같은 프레임에 오면 원본 사각형이 아직 0이다. 먼저 재게 한다.
            if (sourceRect.parent is RectTransform parentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);

            _label = Object.Instantiate(_source, sourceRect.parent);
            _label.name = CloneName;
            _label.raycastTarget = false;

            // 레이아웃 그룹이 이 복제본까지 배치하면 슬롯 전체가 밀린다.
            if (!_label.TryGetComponent(out LayoutElement layoutElement))
                layoutElement = _label.gameObject.AddComponent<LayoutElement>();

            layoutElement.ignoreLayout = true;

            // 레이아웃에서 빠지는 순간 복제본의 사각형이 프리팹 값(폭 0)으로 돌아간다.
            // 폭 0이면 TMP가 글자마다 줄을 바꿔 세로로 쌓이므로, 원본이 실제로 차지한 사각형을 박아 준다.
            _rect = _label.rectTransform;
            _rect.anchorMin = sourceRect.anchorMin;
            _rect.anchorMax = sourceRect.anchorMax;
            _rect.pivot = sourceRect.pivot;
            _rect.sizeDelta = sourceRect.sizeDelta;
            _rect.anchoredPosition = sourceRect.anchoredPosition;

            _origin = _rect.anchoredPosition;
            _label.gameObject.SetActive(false);
        }
    }
}
