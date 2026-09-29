using Code.Core.Bus;
using Code.Military.Data;
using Code.Military.Squads;
using UnityEngine;

namespace Code.Military.Commanders
{
    /// <summary>
    /// 공세선을 그립니다. 그은 선은 도착 전선이라 막대는 거기에만 서고, 화살표는 분대 무리에서
    /// 출발해 그 전선으로 휘어 들어갑니다 — 분대 진격 화살표와 같은 생김새입니다.
    /// 지휘관에 붙으면 그의 선을 따라가고, 홀로 만들면 드래그 미리보기가 됩니다.
    /// 씬 루트에 두고 월드 좌표로 그립니다.
    /// </summary>
    public class BattleLineView : MonoBehaviour
    {
        private const int CurveSamples = 24;
        private const float OriginMoveThresholdSqr = 0.0225f; // 0.15 유닛 — 무리가 이만큼 움직여야 화살표를 다시 찍는다

        private readonly BattleFront _front = new();
        private readonly Vector2[] _frontPoints = new Vector2[BattleFront.MaxPoints];
        private readonly Vector2[] _curve = new Vector2[ArrowStripBuilder.MaxSamples];

        private Commander _commander;
        private SquadMarkerStyleSO _style;
        private BattleLineMeshes _meshes;

        private BattleLine _shown;
        private float _shownAdvance = float.NaN;
        private Color _shownColor;
        private Vector2 _shownOrigin;
        private bool _shownHasOrigin;
        private bool _visible;
        private bool _arrowVisible;

        public static BattleLineView Attach(Commander commander)
        {
            BattleLineView view = Create($"__BattleLine {commander.name}");
            view._commander = commander;
            return view;
        }

        /// <summary>
        /// 홀로 서는 표시입니다. 드래그 미리보기가 Show/Hide로 직접 부립니다.
        /// </summary>
        public static BattleLineView Create(string viewName)
        {
            GameObject go = new(viewName);
            BattleLineView view = go.AddComponent<BattleLineView>();
            view._style = SquadMarkerStyleSO.Load();
            view._meshes = new BattleLineMeshes(go.transform, view._style);
            view.SetVisible(false);
            return view;
        }

        private void OnDestroy()
            => _meshes?.Dispose();

        private void LateUpdate()
        {
            if (_commander == null)
            {
                if (!ReferenceEquals(_commander, null))
                    Destroy(gameObject); // 지휘관이 파괴됐다
                return;
            }

            bool visible = _commander.HasLine
                           && (_commander.Owner == Owner.Player || _commander.IsVisible);

            if (!visible)
            {
                Hide();
                return;
            }

            BattleLine line = _commander.Line;
            bool hasOrigin = _front.Build(_commander.Roster.Squads, line.Along, _frontPoints) > 0;
            Show(line, _commander.LineAdvance, _commander.Color, _front.Centroid, hasOrigin);
        }

        /// <summary>
        /// 전선 막대와 출발점에서 전선으로 드는 화살표를 그립니다. 눈에 띄게 달라졌을 때만 정점을 다시 찍습니다.
        /// </summary>
        public void Show(in BattleLine line, float advance, Color color, Vector2 origin, bool hasOrigin)
        {
            bool lineChanged = !_visible || !line.Same(_shown) || _shownColor != color
                               || Mathf.Abs(advance - _shownAdvance) > 0.01f;
            bool originChanged = hasOrigin != _shownHasOrigin
                                 || (hasOrigin && (origin - _shownOrigin).sqrMagnitude > OriginMoveThresholdSqr);

            SetVisible(true);
            if (!lineChanged && !originChanged)
                return;

            Vector2 normal = line.Normal;
            Vector2 front = line.Center + normal * advance;

            if (lineChanged)
            {
                _shown = line;
                _shownAdvance = advance;
                _shownColor = color;

                _meshes.Bar.SetPositionAndRotation(front,
                    Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.right, line.Along)));
                _meshes.WriteBar(line.Length, _style.LineWidth, _style.LineOutlineWidth, color, _style.LineEdgeColor);
            }

            _shownOrigin = origin;
            _shownHasOrigin = hasOrigin;
            WriteArrow(origin, hasOrigin, front, normal, color);
        }

        /// <summary>
        /// 출발점에서 전선 중앙으로 드는 2차 베지어입니다. 전선에 수직으로 들어오도록 제어점을 전선 뒤에 둡니다.
        /// 출발점이 없거나(미리보기에 분대가 없음) 이미 닿았으면 그리지 않습니다.
        /// </summary>
        private void WriteArrow(Vector2 origin, bool hasOrigin, Vector2 front, Vector2 normal, Color color)
        {
            Vector2 delta = front - origin;
            float distance = delta.magnitude;

            if (!hasOrigin || distance < _style.MinLength)
            {
                SetArrowVisible(false);
                return;
            }

            SetArrowVisible(true);
            _meshes.Arrow.position = origin;

            Vector2 end = delta;
            Vector2 control = end - normal * (distance * 0.5f);
            float length = 0f;

            for (int i = 0; i < CurveSamples; i++)
            {
                float t = i / (float)(CurveSamples - 1);
                float u = 1f - t;
                _curve[i] = 2f * u * t * control + t * t * end;
                if (i > 0)
                    length += Vector2.Distance(_curve[i - 1], _curve[i]);
            }

            // 짧은 화살표까지 같은 굵기면 코앞의 전선을 가리키는 화살표가 뭉툭한 덩어리로 보인다
            float scale = Mathf.Lerp(_style.ShortWidthScale, 1f, Mathf.Clamp01(length / _style.FullWidthLength));
            float tail = _style.LineArrowWidth * scale;
            float head = tail * _style.HeadWidthScale;
            float headLength = Mathf.Min(head * _style.HeadLengthScale, length * _style.HeadLengthCap);
            float neckY = length - headLength;

            ArrowDimensions dim = new(tail, tail * _style.NeckWidthScale, head,
                neckY, Mathf.Min(_style.FadeLength, neckY * _style.FadeCap), length);

            _meshes.WriteArrow(_curve, CurveSamples, dim, _style.LineOutlineWidth * scale, color, _style.LineEdgeColor);
        }

        public void Hide()
        {
            if (_visible)
                SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            _meshes.SetBarVisible(visible);
            SetArrowVisible(visible && _arrowVisible);
        }

        private void SetArrowVisible(bool visible)
        {
            _arrowVisible = visible;
            _meshes.SetArrowVisible(visible);
        }
    }
}
