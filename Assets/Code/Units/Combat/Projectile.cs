using Code.Commandable.Interfaces;
using Code.Units.Util;
using ObjectPool.RunTime;
using UnityEngine;

namespace Code.Units.Combat
{
    public class Projectile : MonoBehaviour, IPoolable
    {
        [field: SerializeField] public PoolingItemSO PoolType { get; private set; }
        [SerializeField] private float offset;

        [Header("Arc Settings")]
        [SerializeField] private float arcHeight = 2f; // 포물선 최대 높이
        [SerializeField] private float minFlightDuration = 0.3f; // 최소 비행 시간(초)

        public GameObject GameObject => gameObject;
        protected virtual bool DeferPoolReturn => false;

        protected int Damage { get; private set; }
        private IDamageable _target;
        private float _speed;
        private bool _hit;
        private Pool _pool;
        private bool _isSplash;
        private float _splashRadius;
        private ContactFilter2D _splashFilter;

        // 포물선 계산용 변수
        private Vector3 _startPosition;
        private float _elapsedTime;
        private float _flightDuration;

        public void SetUpPool(Pool pool)
            => _pool = pool;

        public virtual void ResetItem()
        {
            _target = null;
            _hit = false;
            _elapsedTime = 0f;
        }

        public void Init(IDamageable target, float speed, int damage,
            bool isSplash = false, float splashRadius = 0f, LayerMask damageableLayers = default)
        {
            _target = target;
            _speed = speed;
            Damage = damage;
            _isSplash = isSplash;
            _splashRadius = splashRadius;
            if (isSplash)
            {
                _splashFilter = new ContactFilter2D { useTriggers = false };
                _splashFilter.SetLayerMask(damageableLayers);
            }


            // 시작 지점과 비행 시간 기록
            _startPosition = transform.position;
            float distance = Vector3.Distance(_startPosition, _target.Transform.position);
            _flightDuration = Mathf.Max(distance / _speed, minFlightDuration);
        }

        private void Update()
        {
            if (_hit)
                return;

            if (_target?.Transform == null)
            {
                ReturnToPool();
                return;
            }

            Vector3 targetPos = _target.Transform.position;

            _elapsedTime += Time.deltaTime;
            float progress = _elapsedTime / _flightDuration;

            if (progress >= 1.0f)
            {
                Hit();
                return;
            }

            Vector3 nextBasePos = Vector3.Lerp(_startPosition, targetPos, progress);

            float height = Mathf.Sin(progress * Mathf.PI) * arcHeight;
            Vector3 nextPosWithArc = new Vector3(nextBasePos.x, nextBasePos.y + height, nextBasePos.z);

            Vector3 moveDir = nextPosWithArc - transform.position;
            if (moveDir != Vector3.zero)
            {
                float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle + offset);
            }

            transform.position = nextPosWithArc;
        }

        private void Hit()
        {
            _hit = true;
            if (_target?.Transform != null)
            {
                Vector3 normal = (transform.position - _target.Transform.position).normalized;
                OnHit(_target, normal);
            }

            if (!DeferPoolReturn)
                ReturnToPool();
        }

        protected virtual void OnHit(IDamageable target, Vector3 normal)
        {
            if (!_isSplash || _splashRadius <= 0f)
            {
                target.TakeDamage(Damage, target.Transform.position, normal);
                return;
            }

            Vector3 center = target.Transform.position;
            SplashUtil.Apply(Damage, center, _splashRadius, _splashFilter, normal);
        }

        protected void ReturnToPool()
        {
            if (_pool != null)
                _pool.Push(this);
            else
                Destroy(gameObject);
        }
    }
}
