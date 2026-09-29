using Code.Economics;
using Code.Effects;
using Code.FogSystem;
using Code.Navigation.Baking;
using Code.Sequences;
using Code.Units.Pawns;
using UnityEngine;

namespace Code.Environments
{
    /// <summary>
    /// 맵에 배치되는 자원 오브젝트입니다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ResourceObject : MonoBehaviour, IHideable, IGuideHighlightable
    {
        [Header("Resource Object")]
        [SerializeField] private ResourceObjectSO resourceObject;
        [SerializeField] private Transform[] gatherPoints;

        private ResourceObjectManager _resourceObjectManager;

        public ResourceSO ResourceSO => resourceObject.ResourceSO;

        public int RemainingAmount => _remainingAmount;

        /// <summary>
        /// 동시에 붙을 수 있는 일꾼 수입니다. 채집 지점 수와 같습니다.
        /// </summary>
        public int MaxGatherers => _points.Capacity;
        public int MaxAmount => resourceObject.MaxAmount;

        public bool IsDepleted => _remainingAmount <= 0;

        public bool IsAvailable
            => !IsDepleted && _points.HasFreePoint();

        public Vector3 SpawnPosition { get; private set; }

        public Vector3 GuidePosition => transform.position;
        public bool IsGuideAlive => !IsDepleted;

        private GatherPointSet _points;
        private ResourceVisuals _visuals;
        private bool _depleteHandled;

        private bool _hovered;
        private bool _highlighted;

        private Collider2D _collider;
        private SelectionOutlineEffect _outlineEffect;

        private int _remainingAmount;
        private bool _isObstacleRegistered;
        private bool _fogRegistered;

        public Transform Transform => this == null ? null : transform;

        public bool IsVisible { get; private set; } = true;

        public event IHideable.VisibilityChangedEvent OnVisibilityChanged;

        /// <summary>
        /// 시야 밖에서는 겉모습 변화를 멈춰 마지막으로 본 모습으로 남깁니다. 렌더러는 끄지 않습니다.
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            if (isVisible == IsVisible)
                return;

            IsVisible = isVisible;
            OnVisibilityChanged?.Invoke(this, isVisible);
            _visuals.SetVisible(isVisible, IsDepleted);
        }

        private void Awake()
        {
            Debug.Assert(resourceObject != null, $"[{name}] ResourceObjectSO is null!", this);
            Debug.Assert(gatherPoints is { Length: > 0 }, $"[{name}] GatherPoints are not set!", this);

            _points = new GatherPointSet(gatherPoints, transform);

            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            // 선택용 박스는 아래에서 붙으므로, 통행을 막는 콜라이더는 그 전에 잡아야 한다
            _collider = GetComponent<Collider2D>();
            _resourceObjectManager = ResourceObjectManager.Instance;
            _remainingAmount = resourceObject.MaxAmount;
            SpawnPosition = transform.position;

            _outlineEffect = gameObject.AddComponent<SelectionOutlineEffect>();
            // 자원은 SetSelected 하나로만 켜므로 호버 배율이 쓰일 일이 없다 — 흐려지지 않게 1을 준다
            _outlineEffect.Initialize(GetComponentsInChildren<SpriteRenderer>(), Color.white, 1f);

            _visuals = new ResourceVisuals(gameObject, spriteRenderer, GetComponent<Animator>(), resourceObject, Retire);
        }

        private void Start()
        {
            if (_resourceObjectManager != null)
                _resourceObjectManager.Register(this);

            RegisterObstacle();

            // 안개가 없는 씬에서는 등록할 곳이 없다. 그때는 늘 최신 모습으로 두면 된다.
            FogVisibilityManager fog = FogVisibilityManager.Instance;
            if (fog == null)
                return;

            fog.Register(this);
            _fogRegistered = true;
        }

        private void OnDestroy()
        {
            if (_resourceObjectManager != null)
                _resourceObjectManager.Unregister(this);

            UnregisterObstacle();
            UnregisterFromFog();
        }

        /// <summary>
        /// 해당 일꾼이 지점을 예약했고 자원이 남아 있으면 true입니다.
        /// </summary>
        public bool CanGather(Pawn pawn)
            => !IsDepleted && _points.Holds(pawn);

        /// <summary>
        /// 채집 1회분을 차감하고 실제 획득량을 반환합니다. 고갈 처리는 1회만 실행됩니다.
        /// </summary>
        public int Collect()
        {
            int actual = Mathf.Min(ResourceSO.AmountPerGather, _remainingAmount);
            _remainingAmount -= actual;

            if (IsDepleted && !_depleteHandled)
            {
                _depleteHandled = true;
                Deplete();
            }

            return actual;
        }

        /// <summary>
        /// 마우스 호버 상태를 설정합니다. 채집 가능한 자원만 외곽선을 표시합니다.
        /// </summary>
        public void SetHovered(bool hovered)
        {
            // 고갈된 자원은 호버 상태 자체를 갖지 않는다.
            _hovered = hovered && !IsDepleted;
            RefreshOutline();
        }

        /// <summary>
        /// 시퀀스가 짚어 주는 외곽선입니다. 호버와 따로 기억합니다.
        /// </summary>
        public void SetGuideHighlight(bool on)
        {
            _highlighted = on;
            RefreshOutline();
        }

        /// <summary>
        /// fromPosition에서 가장 가까운 빈 채집 지점을 예약하고 인덱스를 반환합니다.
        /// 고갈됐거나 빈자리가 없으면 -1입니다.
        /// </summary>
        public int ReserveGatherPoint(Pawn pawn, Vector3 fromPosition)
            => IsDepleted ? -1 : _points.Reserve(pawn, fromPosition);

        /// <summary>
        /// 인덱스에 해당하는 채집 지점의 월드 좌표를 반환합니다.
        /// </summary>
        public Vector3 GetGatherPoint(int index)
            => _points.PositionOf(index);

        /// <summary>
        /// 해당 일꾼의 지점 예약을 해제합니다. 예약이 없으면 아무 일도 하지 않습니다.
        /// </summary>
        public void ReleaseGatherPoint(Pawn pawn)
            => _points.Release(pawn);

        private void RefreshOutline()
        {
            if (_outlineEffect != null)
                _outlineEffect.SetSelected((_hovered || _highlighted) && !IsDepleted);
        }


        /// <summary>
        /// 자원이 차지하는 셀을 경로 그래프에 통행 불가로 등록합니다.
        /// </summary>
        private void RegisterObstacle()
        {
            if (_isObstacleRegistered || _collider == null)
                return;

            PathBaker baker = PathBaker.Instance;
            if (baker == null)
                return;

            baker.RegisterObstacle(_collider);
            _isObstacleRegistered = true;
        }

        /// <summary>
        /// 등록했던 자원 셀을 다시 통행 가능하게 해제합니다.
        /// </summary>
        private void UnregisterObstacle()
        {
            if (!_isObstacleRegistered || _collider == null)
                return;

            // 씬을 내릴 때는 베이커가 먼저 파괴돼 있을 수 있다. 그때는 등록도 같이 사라진다
            PathBaker baker = PathBaker.Instance;
            if (baker != null)
                baker.UnregisterObstacle(_collider);

            _isObstacleRegistered = false;
        }

        /// <summary>
        /// 고갈을 처리합니다. 게임 로직은 즉시, 겉모습은 보고 있을 때만 반영합니다.
        /// </summary>
        private void Deplete()
        {
            _outlineEffect.SetSelected(false);
            UnregisterObstacle();
            _visuals.Deplete(IsVisible);
        }

        /// <summary>
        /// 고갈 스프라이트가 없는 자원을 맵에서 치웁니다.
        /// </summary>
        private void Retire()
        {
            UnregisterFromFog();
            gameObject.SetActive(false);
        }

        private void UnregisterFromFog()
        {
            if (!_fogRegistered || FogVisibilityManager.Instance == null)
                return;

            FogVisibilityManager.Instance.Unregister(this);
            _fogRegistered = false;
        }
    }
}
