using Code.Buildings.Interfaces;
using Code.Commandable.Interfaces;
using Code.Core.Bus;
using UnityEngine;

namespace Code.Military.Combat
{
    /// <summary>
    /// 분대전용 센서입니다. 분대가 감지할 수 있는 범위 내에서 가장 가까운 적 유닛 또는 건물을 감지합니다.
    /// </summary>
    public class SquadSensor : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private float tickInterval = 0.2f;
        [SerializeField] private int maxHits = 32;

        private const float LoseTargetRangeFactor = 1.5f;
        private const int SpreadBuckets = 8;

        // 새 후보가 현재 목표보다 이만큼 가까워야 갈아탄다(0.64 = 거리 80%). 흔들림 방지용 여유.
        private const float SwitchAdvantage = 0.64f;

        /// <summary>
        /// 감지된 타겟
        /// </summary>
        public IDamageable Target { get; private set; }

        /// <summary>
        /// 감지 범위
        /// </summary>
        public float Range { get; private set; }

        private Owner _owner = Owner.Invalid;
        private Transform _targetTransform;

        private Collider2D[] _hits;
        private ContactFilter2D _filter;
        private float _timer;

        private const int MaxBuffer = 256; // 난전에서 버퍼가 모자라면 여기까지 스스로 넓힌다

        private static int _spawnCounter;

        private void Awake()
        {
            _hits = new Collider2D[maxHits];
            _filter = new ContactFilter2D();
            _filter.SetLayerMask(targetLayers);
            _filter.useTriggers = false; // Do not include triggers in the filter
        }

        /// <summary>
        /// 스쿼드 센서를 초기화합니다. 스쿼드가 생성될 때 한 번만 호출되어야 합니다.
        /// </summary>
        public void Setup(Owner owner, float range)
        {
            _owner = owner;
            Range = range;

            // 분대마다 첫 스캔 프레임을 어긋나게 해 같은 프레임에 몰리지 않도록 한다
            _timer = tickInterval * (_spawnCounter++ % SpreadBuckets) / SpreadBuckets;
        }

        /// <summary>
        /// 플레이어 명령 등으로 현재 목표를 강제로 해제합니다.
        /// </summary>
        public void ClearTarget()
            => SetTarget(null);

        private void Update()
        {
            if(_owner == Owner.Invalid)
                return;

            _timer -= Time.deltaTime;
            if(_timer > 0)
                return;

            _timer += tickInterval;
            Scan();
        }

        /// <summary>
        /// 현재 범위 내에서 가장 가까운 적 유닛 또는 건물을 감지합니다. 유닛이 건물보다 항상 우선합니다.
        /// </summary>
        private void Scan()
        {
            // 목표를 물고 있어도 매번 다시 훑는다 — 예전엔 여기서 빠져나가 코앞의 적을 무시했다.
            bool hasTarget = IsTargetStillValid();

            Vector3 position = transform.position;
            Collider2D[] hits = _hits;
            int count = Physics2D.OverlapCircle(position, Range, _filter, hits);

            // 버퍼가 꽉 찼다는 것은 결과가 잘렸다는 뜻이다. OverlapCircle은 거리순으로 주지 않으므로
            // 코앞의 적이 잘려 나갈 수 있다 — 난전에서 분대가 멀쩡히 서서 노는 원인이다.
            // 다음 스캔부터 넓혀 본다. 한 번 넓힌 버퍼는 그대로 두어 매 틱 다시 재지 않는다.
            // 이번 결과는 hits가 계속 쥐고 있다 — _hits를 갈아치운 뒤 읽으면 텅 빈 배열을 훑는다
            if (count == _hits.Length && _hits.Length < MaxBuffer)
                _hits = new Collider2D[Mathf.Min(MaxBuffer, _hits.Length * 2)];

            IDamageable bestUnit = null, bestBuilding = null;
            float unitSqr = float.MaxValue, buildingSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if(hits[i] == null || !hits[i].TryGetComponent(out IDamageable candidate))
                    continue;

                if(candidate.Owner == _owner || candidate.Owner == Owner.Invalid)
                    continue;

                // 사망 연출이 끝날 때까지 시체는 씬에 남는다. 체력을 안 보면 그걸 목표로 물어
                // 분대 상태가 잡았다 놨다를 반복하고, 그때마다 멤버의 공격이 취소된다.
                if (candidate.CurrentHealth <= 0)
                    continue;

                // 타워에 탄 사수는 건너뛴다 — 유닛이 건물보다 항상 우선이라 안 거르면 타워 대신 사수를 문다
                if (!candidate.IsTargetable)
                    continue;

                float sqr = (candidate.Transform.position - position).sqrMagnitude;
                if (candidate is IBuilding)
                {
                    if (sqr >= buildingSqr)
                        continue;

                    buildingSqr = sqr;
                    bestBuilding = candidate;
                }
                else
                {
                    if (sqr >= unitSqr)
                        continue;

                    unitSqr = sqr;
                    bestUnit = candidate;
                }
            }

            IDamageable best = bestUnit ?? bestBuilding;   // 유닛이 건물보다 항상 우선
            float bestSqr = bestUnit != null ? unitSqr : buildingSqr;

            if (best == null)
            {
                // 물고 있는 목표는 이탈 판정(Range × 1.5)이 놓아줄 때까지 유지한다.
                // 스캔 반경은 Range뿐이라 그 사이 거리에 있는 목표가 여기서 사라진다.
                if (!hasTarget)
                    SetTarget(null);

                return;
            }

            if (!hasTarget || best == Target)
            {
                SetTarget(best);
                return;
            }

            // 건물을 치던 중 유닛이 나타나면 거리와 무관하게 갈아탄다.
            if (bestUnit != null && Target is IBuilding)
            {
                SetTarget(best);
                return;
            }

            // 같은 급끼리는 눈에 띄게 가까울 때만 갈아탄다.
            // 근소한 차이로 흔들리면 목표가 바뀔 때마다 멤버 BT가 리셋돼 사격이 끊긴다.
            float currentSqr = (_targetTransform.position - position).sqrMagnitude;
            if (bestSqr < currentSqr * SwitchAdvantage)
                SetTarget(best);
        }

        /// <summary>
        /// 현재 목표를 설정합니다. null을 전달하면 목표를 해제합니다.
        /// </summary>
        private void SetTarget(IDamageable damageable)
        {
            Target = damageable;
            _targetTransform = damageable?.Transform;
        }

        /// <summary>
        /// 현재 목표가 여전히 유효한지 확인합니다. 유효하지 않으면 목표를 해제합니다.
        /// </summary>
        private bool IsTargetStillValid()
        {
            // _targetTransform 비교가 Unity의 파괴 감지 연산자를 탄다 (인터페이스 == null 은 못 잡는다)
            if (Target == null || _targetTransform == null || Target.CurrentHealth <= 0)
            {
                SetTarget(null);
                return false;
            }

            float loseRange = Range * LoseTargetRangeFactor;
            if ((_targetTransform.position - transform.position).sqrMagnitude > loseRange * loseRange)
            {
                SetTarget(null);
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private Color rangeColor = Color.green;
        [SerializeField] private Color targetLineColor = Color.red;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = rangeColor;
            Gizmos.DrawWireSphere(transform.position, Range);

            if (_targetTransform == null)
                return;

            Gizmos.color = targetLineColor;
            Gizmos.DrawLine(transform.position, _targetTransform.position);
        }
#endif
    }
}
