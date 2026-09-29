using Code.Animations;
using Code.Units.Combat;
using UnityEngine;

namespace Code.Buildings.Logic.EconomyBuilding
{
    /// <summary>
    /// 울타리 안을 천천히 오가는 양 한 마리입니다. 처음 놓인 자리 주변만 돌아다닙니다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SheepView : MonoBehaviour
    {
        [SerializeField] private UnitAnimator unitAnimator;
        [SerializeField] private ParameterSO moveParameter;
        [SerializeField] private Vector2 roamSize = new(0.6f, 0.3f); // 처음 놓인 자리를 중심으로 오갈 범위
        [SerializeField] private float moveSpeed = 0.25f;
        [SerializeField] private Vector2 restRange = new(1.5f, 4f); // 목적지에 닿은 뒤 쉬는 시간
        [SerializeField] private bool spriteFacesLeft; // 원본 스프라이트가 왼쪽을 보고 있으면 켠다
        [SerializeField] private ParameterSO grazeParameter;
        [SerializeField, Range(0f, 1f)] private float grazeChance = 0.5f; // 쉴 때 풀을 뜯을 확률
        [SerializeField] private float avoidRadius = 0.7f; // 일꾼과 이만큼은 떨어져 있는다

        [Header("연출")]
        [SerializeField] private float vanishDuration = 0.28f; // 잡혀 사라지는 데 걸리는 시간
        [SerializeField] private float appearDuration = 0.22f; // 새로 자란 양이 나타나는 시간

        // 울타리(Visual)가 그룹 안에서 0이라, 양은 항상 그보다 커야 가려지지 않는다
        private const int DepthBase = 100;
        private const float DepthPrecision = 100f;

        private SpriteRenderer _renderer;
        private HitFlashEffect _hitFlash;
        private SpriteRenderer[] _flashTargets;
        private Vector3 _baseScale;
        private Vector2 _home;
        private Vector2 _target;
        private float _restTimer;
        private bool _moving;
        private Vector2 _penHalf = new(99f, 99f); // 우리 범위를 못 받았으면 묶지 않는다
        private bool _busy; // 나타나거나 사라지는 중에는 걷지 않는다
        private int _playId;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();

            // 유닛이 맞을 때와 같은 흰색 번쩍임을 쓴다. 배선 없이 붙인다
            _hitFlash = gameObject.AddComponent<HitFlashEffect>();
            _flashTargets = new[] { _renderer };

            _baseScale = transform.localScale;
            _home = transform.localPosition;
            _target = _home;
            ApplyDepth();
        }

        private void OnEnable()
        {
            // 막 자란 양은 제자리에서 한 박자 쉬었다가 움직인다
            _moving = false;
            _restTimer = Random.Range(restRange.x, restRange.y);
            SetPose(false, false);
        }

        /// <summary>
        /// 양을 제자리에서 부풀리며 등장시킵니다. 이미 나와 있으면 모습만 되돌립니다.
        /// </summary>
        public void Appear()
        {
            _playId++;

            if (!gameObject.activeSelf)
            {
                transform.localPosition = _home;
                gameObject.SetActive(true);
                PlayAppear();
                return;
            }

            _busy = false;
            RestoreLook();
        }

        /// <summary>
        /// 양이 잡혀 납작해지며 사라집니다. 연출이 끝나야 오브젝트가 꺼집니다.
        /// </summary>
        public void Vanish()
        {
            if (!gameObject.activeSelf)
                return;

            _hitFlash.Flash(_flashTargets);
            PlayVanish();
        }

        /// <summary>
        /// 일꾼이 가까이 오면 그 반대쪽으로 비켜섭니다. 우리 안이 좁아 두면 계속 겹칩니다.
        /// </summary>
        public void KeepAwayFrom(Vector3 worldPoint)
        {
            if (_busy || transform.parent == null)
                return;

            Vector2 threat = transform.parent.InverseTransformPoint(worldPoint);
            Vector2 current = transform.localPosition;
            Vector2 away = current - threat;

            if (away.sqrMagnitude > avoidRadius * avoidRadius)
                return;

            // 완전히 겹쳤으면 방향이 없다. 아무 쪽으로나 비킨다
            away = away.sqrMagnitude < 0.0001f ? Random.insideUnitCircle.normalized : away.normalized;

            _target = ClampToPen(current + away * avoidRadius);
            _moving = true;
            SetPose(true, false);
        }

        /// <summary>
        /// 이 양이 벗어나면 안 되는 우리 안쪽 범위를 지정합니다(부모 로컬 기준 반경).
        /// </summary>
        public void SetPenBounds(Vector2 half)
        {
            _penHalf = half;

            // 프리팹에서 우리 밖에 놓인 양은 집 자체를 안으로 들인다. 안 그러면 목적지가 영영 벽 밖이다
            _home = ClampToPen(_home);
            _target = ClampToPen(_target);
        }

        private void Update()
        {
            if (_busy)
                return;

            if (_moving)
                StepToward();
            else
                Rest();
        }

        /// <summary>
        /// 우리 안쪽으로 묶습니다. 일꾼을 피해 물러나다 울타리를 뚫고 나가면 안 됩니다.
        /// </summary>
        private Vector2 ClampToPen(Vector2 point)
            => new(
                Mathf.Clamp(point.x, -_penHalf.x, _penHalf.x),
                Mathf.Clamp(point.y, -_penHalf.y, _penHalf.y));

        private void StepToward()
        {
            Vector2 current = transform.localPosition;
            Vector2 next = ClampToPen(Vector2.MoveTowards(current, _target, moveSpeed * Time.deltaTime));

            transform.localPosition = next;
            ApplyDepth();

            float dx = next.x - current.x;
            if (!Mathf.Approximately(dx, 0f))
                _renderer.flipX = spriteFacesLeft ? dx > 0f : dx < 0f;

            if (next != _target)
                return;

            _moving = false;
            _restTimer = Random.Range(restRange.x, restRange.y);

            // 멈춰 선 김에 가끔 풀을 뜯는다
            SetPose(false, Random.value < grazeChance);
        }

        private void Rest()
        {
            _restTimer -= Time.deltaTime;
            if (_restTimer > 0f)
                return;

            _target = ClampToPen(_home + new Vector2(
                Random.Range(-roamSize.x, roamSize.x),
                Random.Range(-roamSize.y, roamSize.y)) * 0.5f);

            _moving = true;
            SetPose(true, false);
        }

        private async void PlayAppear()
        {
            int id = ++_playId;
            _busy = true;

            try
            {
                float elapsed = 0f;
                while (elapsed < appearDuration && id == _playId)
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);

                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / appearDuration);

                    SetLook(Mathf.Lerp(0.4f, 1f, t), Mathf.Lerp(1.3f, 1f, t), t);
                }
            }
            catch (System.OperationCanceledException)
            {
            }
            finally
            {
                if (id == _playId && this != null)
                {
                    _busy = false;
                    RestoreLook();
                }
            }
        }

        private async void PlayVanish()
        {
            int id = ++_playId;
            _busy = true;
            _moving = false;
            SetPose(false, false);

            try
            {
                float elapsed = 0f;
                while (elapsed < vanishDuration && id == _playId)
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);

                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / vanishDuration);

                    // 납작해지며 옅어진다 — 잡히는 장면을 대신하는 최소한의 신호다
                    SetLook(Mathf.Lerp(1f, 1.35f, t), Mathf.Lerp(1f, 0.25f, t), 1f - t);
                }
            }
            catch (System.OperationCanceledException)
            {
            }
            finally
            {
                if (id == _playId && this != null)
                {
                    RestoreLook();
                    _busy = false;
                    gameObject.SetActive(false);
                }
            }
        }

        private void SetLook(float scaleX, float scaleY, float alpha)
        {
            transform.localScale = new Vector3(_baseScale.x * scaleX, _baseScale.y * scaleY, _baseScale.z);

            Color color = _renderer.color;
            color.a = alpha;
            _renderer.color = color;
        }

        private void RestoreLook()
            => SetLook(1f, 1f, 1f);

        private void SetPose(bool moving, bool grazing)
        {
            if (unitAnimator == null)
                return;

            if (moveParameter != null)
                unitAnimator.SetParameter(moveParameter, moving);

            if (grazeParameter != null)
                unitAnimator.SetParameter(grazeParameter, grazing);
        }

        /// <summary>
        /// 아래에 선 양이 위로 오도록 그룹 안 순서를 다시 매깁니다.
        /// </summary>
        private void ApplyDepth()
            => _renderer.sortingOrder =
                Mathf.Max(1, DepthBase + Mathf.RoundToInt(-transform.localPosition.y * DepthPrecision));

        private void OnDrawGizmosSelected()
        {
            Vector3 center = Application.isPlaying && transform.parent != null
                ? transform.parent.TransformPoint(_home)
                : transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, new Vector3(roamSize.x, roamSize.y, 0f));
        }
    }
}
