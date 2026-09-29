using System.Collections.Generic;
using Code.Commandable.Interfaces;
using Code.Units.Combat;
using UnityEngine;

namespace Code.Units.CombatUnits
{
    /// <summary>
    /// 범위 내 아군 유닛에게 공격력/공격속도 버프를 부여하는 지원 유닛입니다.
    /// 범위에 진입하면 버프 적용, 이탈하면 버프 해제입니다.
    /// </summary>
    public class BuffUnit : AbstractCombatUnit
    {
        [Header("Buff Config")]
        [SerializeField] private DamageableSensor buffSensor;
        [SerializeField] private BuffConfigSO buffConfig;

        private readonly HashSet<IBuffable> _buffedUnits = new();
        private readonly Collider2D[] _overlapBuffer = new Collider2D[32];

        protected override void Start()
        {
            base.Start();
            Debug.Assert(buffSensor != null, "BuffUnit requires a _buffSensor DamageableSensor component");
            Debug.Assert(buffConfig != null, "BuffUnit requires a _buffConfig BuffConfigSO");

            buffSensor.Setup(Owner, buffConfig.BuffRange, true);
            buffSensor.OnUnitEnter += HandleBuffEnter;
            buffSensor.OnUnitExit += HandleBuffExit;

            ScanInitialUnitsInRange();
        }

        /// <summary>
        /// 스폰 시 이미 범위 내에 있는 아군 유닛에게 즉시 버프를 적용합니다.
        /// </summary>
        private void ScanInitialUnitsInRange()
        {
            ContactFilter2D filter = new ContactFilter2D
            {
                useTriggers = true,
                useLayerMask = false
            };

            int count = Physics2D.OverlapCircle(transform.position, buffConfig.BuffRange, filter, _overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                if (!_overlapBuffer[i].TryGetComponent(out IDamageable damageable) || damageable.Owner != Owner)
                    continue;

                HandleBuffEnter(damageable);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            foreach (IBuffable buffable in _buffedUnits)
                buffable.RemoveBuff(buffConfig);

            _buffedUnits.Clear();
            if (buffSensor == null)
                return;

            buffSensor.OnUnitEnter -= HandleBuffEnter;
            buffSensor.OnUnitExit -= HandleBuffExit;
        }

        /// <summary>
        /// 버프 유닛은 직접 공격하지 않습니다.
        /// </summary>
        public override bool CanAutoAttack()
            => false;

        private void HandleBuffEnter(IDamageable damageable)
        {
            if (damageable.Transform == transform)
                return;

            if (damageable is not IBuffable buffable || !_buffedUnits.Add(buffable))
                return;

            buffable.AddBuff(buffConfig);
        }

        private void HandleBuffExit(IDamageable damageable)
        {
            if (damageable is not IBuffable buffable || !_buffedUnits.Remove(buffable))
                return;

            buffable.RemoveBuff(buffConfig);
        }
    }
}
