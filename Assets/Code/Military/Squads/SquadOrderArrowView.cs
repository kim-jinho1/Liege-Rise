using Code.Commandable.Interfaces;
using Code.Core.Bus;
using Code.Military.Combat.FSM;
using Code.Military.Data;
using Code.Rendering;
using UnityEngine;

namespace Code.Military.Squads
{
    /// <summary>
    /// 분대의 진격 방향을 목적지까지 곧게 뻗는 화살표로 그립니다.
    /// 분대 쪽 자루가 두껍고 앞으로 갈수록 좁아지다가 끝에서 화살촉이 벌어졌다 뾰족하게 모입니다.
    /// 꼬리는 알파가 0으로 빠져 분대 밑에서 자연스럽게 배어 나옵니다.
    /// 생김새는 전부 SquadMarkerStyleSO가 쥡니다.
    /// </summary>
    public class SquadOrderArrowView : MonoBehaviour
    {
        private const string PivotName = "__SquadOrderArrow";
        private const string FillName = "__SquadOrderArrowFill";
        private const string OutlineName = "__SquadOrderArrowEdge";

        private const int FillClearVerts = 2; // 꼬리 두 점만 투명
        private const int EdgeClearVerts = 4; // 테두리는 페이드 구간이 끝난 뒤부터 그린다.
                                              // 반투명한 본체 밑에 깔리면 꼬리가 탁해진다

        // 분대가 걸어가는 동안 길이는 매 프레임 조금씩 줄어든다. 이만큼 변해야 메시를 다시 찍는다
        private const float RebuildThreshold = 0.05f;

        private readonly ArrowMeshBuilder _builder = new();

        private Squad _squad;
        private SquadStateMachine _machine;
        private SquadMarkerStyleSO _style;
        private Transform _pivot;
        private Mesh _fillMesh;
        private Mesh _edgeMesh;
        private MeshRenderer _fillRenderer;
        private MeshRenderer _edgeRenderer;
        private bool _attacking;

        private float _shownLength = -1f;
        private float _shownTailWidth = -1f;
        private bool _shownAttacking;

        /// <summary>
        /// 분대에 진격 화살표를 붙입니다.
        /// </summary>
        public static void Attach(Squad squad, SquadStateMachine machine)
        {
            if (squad == null || machine == null)
                return;

            SquadOrderArrowView view = squad.gameObject.AddComponent<SquadOrderArrowView>();
            view._squad = squad;
            view._machine = machine;
            view._style = SquadMarkerStyleSO.Load();
            view.Build();
        }

        private void OnDestroy()
        {
            if (_fillMesh != null)
                Destroy(_fillMesh);

            if (_edgeMesh != null)
                Destroy(_edgeMesh);
        }

        private void LateUpdate()
        {
            if (!ShouldShow() || !TryGetAimPoint(out Vector3 tip))
            {
                Hide();
                return;
            }

            Vector3 tail = transform.position;
            tip.z = tail.z;

            Vector3 delta = tip - tail;
            float length = delta.magnitude;

            if (length < _style.MinLength)
            {
                Hide();
                return;
            }

            // 화살표는 자기 좌표계에서 +Y로 뻗는다. 방향은 회전이 전담해 정점 계산이 1차원으로 줄어든다
            _pivot.SetPositionAndRotation(tail,
                Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, delta)));

            Reshape(length);
            SetVisible(true);
        }

        /// <summary>
        /// 적 분대는 시야에 든 동안 늘 그립니다 — 어디로 몰려오는지 화면에 없으면 대비할 방법이 없습니다.
        /// 안개 너머까지 그리면 반대로 위치가 새어 나가므로 가시성을 반드시 거칩니다.
        /// 아군은 선택했을 때만 그립니다. 전 부대의 화살표가 늘 깔리면 지금 내린 명령이 묻힙니다.
        /// </summary>
        private bool ShouldShow()
            => _squad.Owner == Owner.Player ? _squad.IsSelected : SquadVisibility.IsVisibleToPlayer(_squad);

        /// <summary>
        /// 화살표가 가리킬 지점입니다. 명령을 받지 않은 상태면 false입니다.
        /// </summary>
        private bool TryGetAimPoint(out Vector3 point)
        {
            switch (_machine.Current)
            {
                case SquadStateType.Moving:
                case SquadStateType.Advancing:
                    _attacking = false;
                    point = _machine.Destination;
                    return true;

                // 공격은 Destination을 갱신하지 않는다 — 목표를 직접 따라가야 적을 가리킨다.
                // EngageTarget까지 보는 것은 적 분대 때문이다: 출격한 것만 명령을 받고
                // 나머지는 센서로 붙어 CommandedTarget이 비어 있다
                case SquadStateType.Approaching:
                case SquadStateType.Engaging:
                    _attacking = true;
                    return TryTarget(_machine.CommandedTarget, out point)
                           || TryTarget(_machine.EngageTarget, out point);

                default:
                    _attacking = false;
                    point = default;
                    return false;
            }
        }

        private static bool TryTarget(IDamageable target, out Vector3 point)
        {
            if (target == null || target.Transform == null || target.IsDead)
            {
                point = default;
                return false;
            }

            point = target.Transform.position;
            return true;
        }

        /// <summary>
        /// 길이·굵기·명령이 눈에 띄게 달라졌을 때만 정점을 다시 찍습니다.
        /// 자리와 방향은 매 프레임 갱신되므로 이걸 건너뛰어도 화살표는 분대를 따라옵니다.
        /// </summary>
        private void Reshape(float length)
        {
            // 짧은 명령까지 같은 굵기면 코앞을 가리키는 화살표가 뭉툭한 덩어리로 보인다
            float scale = Mathf.Lerp(_style.ShortWidthScale, 1f,
                Mathf.Clamp01(length / _style.FullWidthLength));
            float tail = TailWidth() * scale;

            if (_shownAttacking == _attacking
                && Mathf.Abs(length - _shownLength) < RebuildThreshold
                && Mathf.Abs(tail - _shownTailWidth) < RebuildThreshold)
                return;

            _shownAttacking = _attacking;
            _shownLength = length;
            _shownTailWidth = tail;

            float head = tail * _style.HeadWidthScale;
            float headLength = Mathf.Min(head * _style.HeadLengthScale, length * _style.HeadLengthCap);
            float neckY = length - headLength;

            ArrowDimensions dimensions = new(tail, tail * _style.NeckWidthScale, head,
                neckY, Mathf.Min(_style.FadeLength, neckY * _style.FadeCap), length);

            // 테두리도 같은 배율로 얇아진다 — 폭만 줄이면 짧은 화살표가 테두리에 잡아먹힌다
            _builder.Write(_fillMesh, dimensions, 0f,
                _attacking ? _style.AttackFill : _style.MoveFill, FillClearVerts);
            _builder.Write(_edgeMesh, dimensions, _style.OutlineWidth * scale,
                _attacking ? _style.AttackEdge : _style.MoveEdge, EdgeClearVerts);
        }

        private void Build()
        {
            GameObject pivot = new(PivotName);
            pivot.transform.SetParent(transform, false);
            _pivot = pivot.transform;

            // 테두리를 한 칸 아래에 깔면 부풀린 부분만 가장자리로 삐져나온다
            _edgeMesh = NewMesh(OutlineName);
            _edgeRenderer = AddSurface(pivot, OutlineName, _style.ArrowSortingOrder - 1, _edgeMesh);

            _fillMesh = NewMesh(FillName);
            _fillRenderer = AddSurface(pivot, FillName, _style.ArrowSortingOrder, _fillMesh);

            SetVisible(false);
        }

        private static Mesh NewMesh(string meshName)
            => new() { name = meshName, hideFlags = HideFlags.DontSave };

        private MeshRenderer AddSurface(GameObject parent, string childName, int order, Mesh mesh)
        {
            GameObject child = new(childName);
            child.transform.SetParent(parent.transform, false);
            RenderLayers.PlaceOnMarkerLayer(child, gameObject);

            child.AddComponent<MeshFilter>().sharedMesh = mesh;

            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = MarkerMaterial.Shared;
            renderer.sortingLayerName = RenderLayers.EntitySorting;
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        // 다시 나타날 때는 반드시 새로 찍어야 한다 — 숨은 동안 길이가 얼마든 변했을 수 있다
        private void Hide()
        {
            SetVisible(false);
            _shownLength = -1f;
        }

        private void SetVisible(bool visible)
        {
            _fillRenderer.enabled = visible;
            _edgeRenderer.enabled = visible;
        }

        /// <summary>
        /// 대형이 넓게 퍼질수록 자루가 두꺼워집니다.
        /// </summary>
        private float TailWidth()
        {
            float spacing = _squad.CurrentFormation != null ? _squad.CurrentFormation.Spacing : 1f;
            float spread = spacing * Mathf.Max(1, _squad.AliveCount - 1);
            return Mathf.Clamp(spread * _style.TailWidthPerSpread, _style.MinTailWidth, _style.MaxTailWidth);
        }
    }
}
