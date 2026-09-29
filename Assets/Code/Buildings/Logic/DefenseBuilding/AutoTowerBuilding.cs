using System.Threading;
using Code.Buildings.Base;
using Code.Buildings.Data;
using Code.Commandable.Interfaces;
using Code.Units.Combat;
using Code.Units.Util;
using UnityEngine;

namespace Code.Buildings.Logic.DefenseBuilding
{
    /// <summary>
    /// 자동 공격형 타워 건물입니다. 유닛 탑승 없이 타워가 직접 적을 감지하고 공격합니다.
    /// </summary>
    public class AutoTowerBuilding : AbstractBuilding
    {
        [Header("Auto Tower")]
        [SerializeField] private DamageableSensor sensor;
        [SerializeField] private Transform firePosition;

        private AutoTowerBuildingSO _autoSO;
        private CancellationTokenSource _attackCts;

        protected override void Awake()
        {
            base.Awake();
            _autoSO = BuildingSO as AutoTowerBuildingSO;
        }

        protected override void Start()
        {
            base.Start();
            if (_autoSO == null || _autoSO.AttackConfig == null || sensor == null)
                return;

            sensor.SetupFrom(Owner, _autoSO.AttackConfig);
            sensor.OnUnitEnter += HandleUnitEnter;
            sensor.OnUnitExit += HandleUnitExit;
        }

        protected override void OnDestroy()
        {
            CancelAttackLoop();

            if (sensor != null)
            {
                sensor.OnUnitEnter -= HandleUnitEnter;
                sensor.OnUnitExit -= HandleUnitExit;
            }

            base.OnDestroy();
        }

        private void HandleUnitEnter(IDamageable damageable)
        {
            if (_attackCts != null)
                return;

            StartAttackLoop();
        }

        private void HandleUnitExit(IDamageable damageable)
        {
            if (sensor.Damageable.Count > 0)
                return;

            CancelAttackLoop();
        }

        private void StartAttackLoop()
        {
            CancelAttackLoop();
            _attackCts = new CancellationTokenSource();
            AttackLoopAsync(_attackCts.Token);
        }

        private void CancelAttackLoop()
        {
            _attackCts?.Cancel();
            _attackCts?.Dispose();
            _attackCts = null;
        }

        /// <summary>
        /// 센서 범위 내 적이 있는 동안 AttackDelay 간격으로 가장 가까운 적을 공격합니다.
        /// </summary>
        private async void AttackLoopAsync(CancellationToken token)
        {
            AttackConfigSO config = _autoSO.AttackConfig;

            while (!token.IsCancellationRequested && sensor.Damageable.Count > 0)
            {
                IDamageable target = GetClosestTarget();
                if (target?.Transform != null)
                {
                    AttackResolver.Resolve(
                        Owner, transform, target, firePosition,
                        config, 1f, 0f);
                }

                try
                {
                    await Awaitable.WaitForSecondsAsync(config.AttackDelay, token);
                }
                catch (System.OperationCanceledException)
                {
                    return;
                }
            }

            _attackCts?.Dispose();
            _attackCts = null;
        }

        /// <summary>
        /// 센서 범위 내 가장 가까운 적을 반환합니다.
        /// </summary>
        private IDamageable GetClosestTarget()
        {
            IDamageable closest = null;
            float minSqrDist = float.MaxValue;
            Vector3 pos = transform.position;

            foreach (IDamageable damageable in sensor.Damageable)
            {
                if (damageable?.Transform == null || !damageable.IsTargetable)
                    continue;

                float sqrDist = (damageable.Transform.position - pos).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closest = damageable;
                }
            }

            return closest;
        }
    }
}
