using Code.Commandable.Interfaces;
using Code.Core;
using Code.Navigation.Baking;
using Code.Navigation.Pathfinding;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Navigation
{
    /// <summary>
    /// PathfindingEngine을 사용하여 경로를 탐색하고, 유닛의 실제 이동을 제어하는 컴포넌트입니다.
    /// </summary>
    public class PathAgent : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BakedDataSO bakedData;
        [SerializeField] private Tilemap baseTilemap;
        [SerializeField] private SpriteRenderer flipTarget;

        [Header("Movement")]
        [SerializeField] private float speed = 5f;
        [SerializeField] private float stoppingDistance = 0.2f;
        [SerializeField] private float radius = 0.5f;
        [SerializeField] private bool updateRotation = true;
        [SerializeField] private int maxPathCount = 128;

        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        public float StoppingDistance
        {
            get => stoppingDistance;
            set => stoppingDistance = value;
        }

        public float Radius => radius;

        public bool UpdateRotation
        {
            get => updateRotation;
            set => updateRotation = value;
        }

        /// <summary>
        /// true일 경우 StoppingDistance를 무시하고 목표 지점까지 정확히 이동합니다.
        /// </summary>
        public bool IgnoreStoppingDistance { get; set; }

        /// <summary>
        /// 경로 요청이 예산 대기열에 올라간 뒤 아직 계산되지 않았으면 true입니다.
        /// 도착 판정은 이 값이 false가 된 뒤에만 성립합니다.
        /// </summary>
        public bool PathPending { get; private set; }

        public float RemainingDistance { get; internal set; } = float.MaxValue;

        /// <summary>
        /// true가 되면 이동 루프가 그 자리에서 빠져나옵니다.
        /// 다시 false로 돌려도 루프는 살아나지 않으므로, 멈추는 순간 목적지 캐시를 비워
        /// 재개 시 같은 자리라도 반드시 경로를 새로 깔게 합니다.
        /// (공격·건설·주둔·타워 탑승 액션이 전부 이 멈춤-재개 패턴을 씁니다.)
        /// </summary>
        public bool IsStopped
        {
            get => _isStopped;
            set
            {
                _isStopped = value;
                if (!value)
                    return;

                _lastDestination = Vector3.positiveInfinity;
                PathPending = false; // 멈추라는 명령이 대기 중인 경로 요청을 이긴다
            }
        }

        public Vector3 Velocity { get; internal set; }
        public bool IsOnPath { get; private set; }

        // 같은 목적지로 다시 들어온 요청을 걸러내기 위한 마지막 요청 지점.
        private const float RepathThresholdSqr = 0.25f; // 0.5 유닛

        // 실패한 목적지를 다시 시도하기까지 쉬는 시간.
        // 같은 성분 안에서 장애물에 막힌 경우는 A*가 끝까지 돌고 실패하므로 매 프레임 재시도가 곧 프레임 저하다.
        private const float FailureRetryDelay = 1.5f;

        private PathFollower _follower;
        private ISelectable _selectable;
        private Vector3 _lastDestination;
        private Vector3 _pendingDestination;
        private Vector3 _failedDestination = Vector3.positiveInfinity;
        private float _failureRetryTime;
        private bool _isStopped;

        private void Awake()
        {
            _follower = new PathFollower(this, maxPathCount);
            _selectable = GetComponent<ISelectable>();

            if (bakedData != null && baseTilemap != null)
                return;

            PathBaker baker = PathBaker.Instance;
            if (baker == null)
            {
                Debug.LogError($"PathAgent ({gameObject.name}): PathBaker 싱글톤을 찾을 수 없습니다.");
                return;
            }

            if (bakedData == null)
                bakedData = baker.BakedData;

            if (baseTilemap == null)
                baseTilemap = baker.BaseTilemap;
        }

        public Vector3[] CalculateVisualPath(Vector3 from, Vector3 to)
            => NavSampler.VisualPath(from, to, bakedData, baseTilemap, radius, maxPathCount);

        public bool SamplePosition(Vector3 worldPos, out Vector3 sampledPos, float maxRadius)
            => NavSampler.SamplePosition(worldPos, out sampledPos, maxRadius, bakedData, baseTilemap);

        public void SetDestination(Vector3 worldPos)
        {
            // 이미 사실상 같은 자리로 가고 있으면 A*를 다시 돌리지 않는다.
            // CalculatePath는 메인 스레드에서 끝까지 도는 데다 정적 버퍼를 lock으로 공유해
            // 에이전트끼리 줄을 선다 — 분대가 0.4초마다 전원 재계산하면 그대로 프레임이 튄다.
            if (IsOnPath && !IsStopped && (worldPos - _lastDestination).sqrMagnitude < RepathThresholdSqr)
                return;

            // 직전에 실패한 목적지는 쿨다운이 끝나기 전까지 다시 계산하지 않는다.
            if (Time.time < _failureRetryTime
                && (worldPos - _failedDestination).sqrMagnitude < RepathThresholdSqr)
                return;

            // 대기 중이면 목적지만 최신으로 갈아끼운다 — 대기열에 두 번 오르지 않는다.
            _pendingDestination = worldPos;
            if (PathPending)
                return;

            // 곧장 갈 수 있으면 A*를 아예 돌리지 않는다. 분대원 다섯이 대형 자리로 한 걸음 옮길 때마다
            // A*가 다섯 번 도는 것이 유닛이 많아질 때 프레임이 떨어지는 가장 큰 이유다
            if (TryStraightPath(worldPos))
                return;

            PathPending = true;
            PathRequestScheduler.Enqueue(this);
        }

        /// <summary>
        /// 목적지까지 뚫려 있으면 두 점 경로로 바로 출발합니다. 막혀 있으면 false — 대기열로 갑니다.
        /// </summary>
        private bool TryStraightPath(Vector3 worldPos)
        {
            if (!PathfindingEngine.CanWalkStraight(transform.position, worldPos, radius, bakedData, baseTilemap))
                return false;

            _failureRetryTime = 0f;
            _follower.FillStraight(transform.position, worldPos);
            BeginMove(worldPos);
            return true;
        }

        /// <summary>
        /// 대기 중이던 경로 요청을 실제로 계산해 이동을 시작합니다.
        /// 프레임당 예산을 쥔 PathRequestScheduler만 호출합니다.
        /// </summary>
        internal void ResolvePendingPath()
        {
            PathPending = false;

            Vector3 worldPos = _pendingDestination;
            int count = PathfindingEngine.CalculatePath(transform.position, worldPos, bakedData, baseTilemap,
                radius, _follower.Buffer);

            if (count > 0)
            {
                _failureRetryTime = 0f; // 성공했으니 실패 기록을 지운다
                _follower.Fill(count);
                BeginMove(worldPos);
                return;
            }

            // 같은 목적지로 곧장 다시 들어오는 재계산을 막는다.
            _lastDestination = worldPos;
            _failedDestination = worldPos;
            _failureRetryTime = Time.time + FailureRetryDelay;

            IsOnPath = false;
            RemainingDistance = float.MaxValue;
            Velocity = Vector3.zero;
            UnityRef.Alive(PathVisualizer.Instance)?.ClearUnitDestination(_selectable);
        }

        /// <summary>
        /// 깔아 둔 경로로 출발합니다. 남은 거리와 목적지 표시를 먼저 세운 뒤에 걷게 합니다 —
        /// 걷기는 첫 프레임 몫을 그 자리에서 처리하므로 순서가 뒤집히면 두 값이 한 프레임 뒤처집니다.
        /// </summary>
        private void BeginMove(Vector3 requestedDestination)
        {
            _lastDestination = requestedDestination;

            IsOnPath = true;
            IsStopped = false;

            Vector3 arrival = _follower.LastPoint;
            RemainingDistance = Vector3.Distance(transform.position, arrival);
            UnityRef.Alive(PathVisualizer.Instance)?.SetUnitDestination(_selectable, arrival);

            _follower.Begin();
        }

        public void Warp(Vector3 worldPos)
        {
            ResetPath();
            transform.position = SamplePosition(worldPos, out Vector3 snapped, Radius * 2f) ? snapped : worldPos;
        }

        public void ResetPath()
        {
            _follower.Cancel();
            PathPending = false; // 대기 중이던 요청은 여기서 버린다
            _lastDestination = Vector3.positiveInfinity; // 다음 명령은 같은 자리라도 반드시 다시 깐다
            IsOnPath = false;
            RemainingDistance = float.MaxValue;
            Velocity = Vector3.zero;
            UnityRef.Alive(PathVisualizer.Instance)?.ClearUnitDestination(_selectable);
        }

        /// <summary>
        /// 경로를 끝까지 걸었을 때 팔로워가 부릅니다.
        /// </summary>
        internal void NotifyArrived()
        {
            IsOnPath = false;
            RemainingDistance = 0f;
            Velocity = Vector3.zero;
            UnityRef.Alive(PathVisualizer.Instance)?.MarkUnitArrived(_selectable);
        }

        /// <summary>
        /// 지정한 월드 좌표를 바라보도록 스프라이트 좌우 반전을 즉시 설정합니다.
        /// 채집·공격 등 정지 상태에서 대상 방향으로 facing을 고정할 때 사용합니다.
        /// </summary>
        public void FaceTowards(Vector3 worldPosition)
            => FaceDirection(worldPosition - transform.position);

        internal void FaceDirection(Vector3 direction)
        {
            if (flipTarget != null && Mathf.Abs(direction.x) > 0.01f)
                flipTarget.flipX = direction.x < 0f;
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
            => _follower?.DrawGizmos();

#endif
    }
}
