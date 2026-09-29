using System.Collections.Generic;
using Code.Commandable.Interfaces;
using Code.Core.Bus;
using Code.FogSystem;
using Code.GameEvents.Enemy;
using Code.GameEvents.Unit;
using UnityEngine;

namespace Code.Units.Combat
{
    /// <summary>
    /// 범위 내 적 유닛을 감지하는 센서입니다.
    /// 시야 시스템과 연동하여 가시 상태인 적만 타겟팅합니다.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class DamageableSensor : MonoBehaviour
    {
        [field: SerializeField] public Owner Owner { get; set; }
        [field: SerializeField] public bool DetectFriendly { get; private set; }

        public delegate void UnitDetectEvent(IDamageable damageable);

        public event UnitDetectEvent OnUnitEnter;
        public event UnitDetectEvent OnUnitExit;

        /// <summary>
        /// 시야 내 가시 상태인 적 목록만 반환합니다(전략 정보·표시용).
        /// </summary>
        public IReadOnlyCollection<IDamageable> Damageable => _visibleDamageable;

        /// <summary>
        /// 감지 범위(콜라이더) 안의 모든 적을 반환합니다(안개 가시성 무관).
        /// 유닛이 코앞의 적과는 시야와 상관없이 교전해야 하므로 전투 타겟 획득에 사용합니다.
        /// </summary>
        public IReadOnlyCollection<IDamageable> AllInRange => _allDamageable;

        private CircleCollider2D _collider;
        private readonly HashSet<IDamageable> _allDamageable = new();
        private readonly HashSet<IDamageable> _visibleDamageable = new();

        private void Awake()
        {
            _collider = GetComponent<CircleCollider2D>();
        }

        private void OnDestroy()
        {
            foreach (IDamageable damageable in _allDamageable)
            {
                if (damageable is IHideable hideable)
                    hideable.OnVisibilityChanged -= HandleVisibilityChanged;
            }

            Bus<UnitDeathEvent>.UnRegisterForAll(HandleUnitDeath);
            Bus<EnemyDeathEvent>.UnRegisterForAll(HandleEnemyDeath);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out IDamageable damageable))
                return;

            bool isSameOwner = damageable.Owner == Owner;
            if (DetectFriendly ? !isSameOwner : isSameOwner)
                return;

            if (!_allDamageable.Add(damageable))
                return;

            if (other.TryGetComponent(out IHideable hideable))
            {
                hideable.OnVisibilityChanged += HandleVisibilityChanged;
                if (hideable.IsVisible)
                {
                    _visibleDamageable.Add(damageable);
                    OnUnitEnter?.Invoke(damageable);
                }
            }
            else
            {
                _visibleDamageable.Add(damageable);
                OnUnitEnter?.Invoke(damageable);
            }

            if (_allDamageable.Count == 1)
            {
                Bus<UnitDeathEvent>.RegisterForAll(HandleUnitDeath);
                Bus<EnemyDeathEvent>.RegisterForAll(HandleEnemyDeath);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent(out IDamageable damageable))
                return;

            RemoveDamageable(damageable);

            if (other.TryGetComponent(out IHideable hideable))
                hideable.OnVisibilityChanged -= HandleVisibilityChanged;
        }

        private void HandleUnitDeath(UnitDeathEvent evt)
        {
            if (_allDamageable.Contains(evt.Unit))
                RemoveDamageable(evt.Unit);
        }

        /// <summary>
        /// 적 유닛 사망 시 감지 목록에서 제거합니다.
        /// </summary>
        private void HandleEnemyDeath(EnemyDeathEvent evt)
        {
            if (_allDamageable.Contains(evt.Unit))
                RemoveDamageable(evt.Unit);
        }

        private void RemoveDamageable(IDamageable damageable)
        {
            _allDamageable.Remove(damageable);

            if (_visibleDamageable.Remove(damageable))
                OnUnitExit?.Invoke(damageable);

            if (_allDamageable.Count == 0)
            {
                Bus<UnitDeathEvent>.UnRegisterForAll(HandleUnitDeath);
                Bus<EnemyDeathEvent>.UnRegisterForAll(HandleEnemyDeath);
            }
        }

        /// <summary>
        /// 가시성 변경 시 visible 목록을 갱신하고 Enter/Exit 이벤트를 발행합니다.
        /// </summary>
        private void HandleVisibilityChanged(IHideable hideable, bool isVisible)
        {
            if (hideable is not IDamageable damageable)
                return;

            if (isVisible)
            {
                if (_visibleDamageable.Add(damageable))
                    OnUnitEnter?.Invoke(damageable);
            }
            else
            {
                if (_visibleDamageable.Remove(damageable))
                    OnUnitExit?.Invoke(damageable);
            }
        }

        public void SetupFrom(Owner owner, AttackConfigSO attackConfig)
        {
            Owner = owner;
            if (_collider == null)
                _collider = GetComponent<CircleCollider2D>();

            _collider.radius = attackConfig.DetectRange;
        }

        /// <summary>
        /// 버프 센서 등 커스텀 용도로 범위와 감지 방향(아군/적)을 직접 설정합니다.
        /// </summary>
        public void Setup(Owner owner, float range, bool detectFriendly = false)
        {
            Owner = owner;
            DetectFriendly = detectFriendly;
            if (_collider == null)
                _collider = GetComponent<CircleCollider2D>();

            _collider.radius = range;
        }
    }
}
