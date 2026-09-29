using Code.Animations;
using Code.Buildings.Logic.DefenseBuilding;
using Code.Commandable.Interfaces;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Enemy;
using Code.GameEvents.System;
using Code.Units.Base;
using Code.Units.BT;
using Code.Units.Combat.Calculator;
using Code.Units.Interfaces;
using Code.Units.Util;
using UnityEngine;

namespace Code.Units.Combat
{
    /// <summary>
    /// 전투 가능한 유닛의 기반 클래스입니다.
    /// </summary>
    public class AbstractCombatUnit : AbstractUnit, IAttacker, IBuffable
    {
        [field: SerializeField] public DamageableSensor Sensor { get; private set; }
        [SerializeField] private ParameterSO attackDirectionParam; // 8방향 공격 스프라이트를 가진 유닛만 할당(비어 있으면 단일 방향 적용됨)

        /// <summary>
        /// 이 유닛을 클릭·드래그로 잡았을 때 실제로 선택될 대상입니다.
        /// 분대원은 소속 분대로 접히고, 분대가 없는 단독 유닛(망루 사수 등)은 자기 자신입니다 —
        /// null을 돌려주면 SelectionQuery가 그 자리를 빈 땅으로 보아 이름·체력이 뜨지 않습니다.
        /// </summary>
        public virtual ISelectable SelectionProxy
            => Squad is UnityEngine.Object squadObject && squadObject != null ? Squad as ISelectable : this;

        public CombatUnitSO CombatSO { get; private set; }

        /// <summary>
        /// 소속 분대입니다. null이면 단독 유닛으로 동작합니다.
        /// 분대가 스폰 직후 대입하며, 이후 바뀌지 않습니다.
        /// </summary>
        public ISquadContext Squad { get; set; }

        /// <summary>
        /// 공격력 배율
        /// </summary>
        public float DamageMultiplier
            => _buffCalculator.DamageMultiplier * (Squad?.DamageMultiplier ?? 1f);

        /// <summary>
        /// 공격 딜레이 배율
        /// </summary>
        public float AttackDelayMultiplier
            => _buffCalculator.AttackDelayMultiplier * (Squad?.AttackDelayMultiplier ?? 1f);

        /// <summary>
        /// 현재 타워에 탑승 중인지 여부
        /// </summary>
        public bool IsMounted
            => _mountCalculator.IsMounted;

        /// <summary>
        /// 타워에 탄 동안에는 목표가 되지 않습니다. 타워를 치러 온 적이 위에 탄 사수부터
        /// 물어 버리면, 건물은 멀쩡한데 사수만 갈아 넣는 싸움이 됩니다.
        /// </summary>
        public override bool IsTargetable
            => base.IsTargetable && !IsMounted;

        private readonly UnitBuffCalculator _buffCalculator = new();
        private TowerMountCalculator _mountCalculator;
        private AutoEngagementCalculator _engagementCalculator;

        /// <summary>
        /// 버프를 추가하고 활성 버프 전체로부터 배율을 재계산합니다.
        /// </summary>
        public void AddBuff(BuffConfigSO config)
            => _buffCalculator.Add(config);

        /// <summary>
        /// 버프를 제거하고 활성 버프 전체로부터 배율을 재계산합니다.
        /// </summary>
        public void RemoveBuff(BuffConfigSO config)
            => _buffCalculator.Remove(config);

        protected override void Awake()
        {
            base.Awake();
            _mountCalculator = new TowerMountCalculator(this);
            _engagementCalculator = new AutoEngagementCalculator(this);
        }

        protected override void Start()
        {
            CombatSO = AbstractCommandableSO as CombatUnitSO;
            base.Start();
            Debug.Assert(Sensor != null, "Combat unit must have a DamageableSensor component");

            if (Squad == null)
            {
                Sensor.OnUnitEnter += _engagementCalculator.HandleUnitEnter;
                Sensor.OnUnitExit += _engagementCalculator.HandleUnitExit;
            }

            if (CombatSO?.AttackConfig != null)
            {
                SetVariableValue(BTVariables.AttackConfig, CombatSO.AttackConfig);
                Sensor.SetupFrom(Owner, CombatSO.AttackConfig);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Sensor == null)
                return;

            if (Squad == null)
            {
                Sensor.OnUnitEnter -= _engagementCalculator.HandleUnitEnter;
                Sensor.OnUnitExit -= _engagementCalculator.HandleUnitExit;
            }
        }

        /// <summary>
        /// 분대 소속이면 피격 방향에 따른 배율을 곱해 피해를 적용합니다.
        /// 판정 기준은 공격자를 향하는 normal이라, 맞은 멤버가 대형 어디에 서 있든 결과가 같습니다.
        /// </summary>
        protected override void ApplyDamage(int damage, Vector3 position, Vector3 normal)
        {
            float multiplier = Squad?.DirectionalMultiplierFrom(normal) ?? 1f;
            int scaled = Mathf.RoundToInt(damage * multiplier);

            // 배율이 1 미만이면 반올림으로 0이 되어 피해가 통째로 사라진다. 최소 1은 남긴다.
            base.ApplyDamage(damage > 0 ? Mathf.Max(1, scaled) : scaled, position, normal);

            // 회복(음수 피해)은 반격 사유가 아니다.
            // Squad는 인터페이스라 ?.이 파괴된 분대 루트를 통과시킨다
            if (damage > 0 && UnityRef.IsAlive(Squad))
                Squad.NotifyMemberDamaged(normal);
        }

        /// <summary>
        /// 적 유닛 사망 이벤트를 발행합니다. 적 유닛은 WasDispatched 정보를 포함한 EnemyDeathEvent를 발행합니다.
        /// </summary>
        protected override void RaiseDeathEvent()
        {
            if (Owner == Owner.Enemy)
                Bus<EnemyDeathEvent>.Raise(Owner, new EnemyDeathEvent(Owner, this, WasDispatched));
            else
                base.RaiseDeathEvent();

            if (UnityRef.IsAlive(Squad))
                Squad.NotifyMemberDied(this);
        }

        public override void RefreshData(RefreshData evt)
        {
            base.RefreshData(evt);
            if (!evt.Unit.Equals(AbstractCommandableSO))
                return;

            CombatSO = AbstractCommandableSO as CombatUnitSO;
            if (CombatSO?.AttackConfig == null)
                return;

            SetVariableValue(BTVariables.AttackConfig, CombatSO.AttackConfig);
            Sensor.SetupFrom(Owner, CombatSO.AttackConfig);
        }

        /// <summary>
        /// 자동 교전 허용 여부를 반환합니다. false인 경우 적 유닛이 감지되어도 공격하지 않습니다.
        /// </summary>
        public virtual bool CanAutoAttack()
            => Squad == null;

        /// <summary>
        /// 타워에 탑승합니다. 감지 범위가 RangeBonus만큼 증가합니다.
        /// </summary>
        public void MountTower(MannedTowerBuilding tower, int slotIndex)
            => _mountCalculator.Mount(tower, slotIndex);

        /// <summary>
        /// 지정한 탑승형 타워로 이동해 탑승하도록 명령합니다.
        /// 실제 탑승은 BT의 MountTower 분기가 처리하며, 빈 슬롯이 없으면 실패해 제자리에 섭니다.
        /// </summary>
        public void OrderMountTower(MannedTowerBuilding tower)
        {
            SetVariableValue(BTVariables.TargetGameObject, tower.gameObject);
            SetVariableValue(BTVariables.Command, UnitCommands.MountTower);
        }

        /// <summary>
        /// 타워에서 하차합니다. 감지 범위를 원래 값으로 복구하고 Stop 상태로 전환합니다.
        /// </summary>
        public void UnmountTower()
            => _mountCalculator.Unmount();

        /// <summary>
        /// 공격방향을 설정합니다. attackDirectionParam이 비어 있으면 단일 방향 공격으로 간주하고 무시됩니다.
        /// </summary>
        public void SetFacingDirection(Vector3 worldDir)
            => AttackDirectionUtil.Apply(attackDirectionParam, UnitAnimator, worldDir);

        public void Attack(IDamageable damageable)
        {
            SetVariableValue(BTVariables.TargetGameObject, damageable.Transform.gameObject);
            SetVariableValue(BTVariables.Command, UnitCommands.Attack);
        }

        public void Attack(Vector3 location)
        {
            SetVariableValue(BTVariables.TargetLocation, location);
            SetVariableValue(BTVariables.Command, UnitCommands.Attack);

            GameObject closestEnemy = _engagementCalculator.GetClosestEnemy();
            SetVariableValue(BTVariables.TargetGameObject, closestEnemy);
        }
    }
}
