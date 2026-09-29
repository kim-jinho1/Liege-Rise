using Code.Commandable.Interfaces;
using Code.Units.BT;
using Unity.Behavior;
using UnityEngine;

namespace Code.Units.Combat.Calculator
{
    /// <summary>
    /// 자동 교전 계산기입니다. 적 유닛 감지 시 공격 명령을 내립니다.
    /// </summary>
    public class AutoEngagementCalculator
    {
        private readonly AbstractCombatUnit _unit;

        public AutoEngagementCalculator(AbstractCombatUnit unit)
            => _unit = unit;

        /// <summary>
        /// 적 감지 시 자동 공격을 시작합니다. 탑승 중이거나 건설 중이면 무시합니다.
        /// </summary>
        public void HandleUnitEnter(IDamageable damageable)
        {
            if (!_unit.CanAutoAttack())
                return;

            if (_unit.IsMounted)
                return; // 탑승 중에는 MountTowerAction이 공격을 직접 제어합니다.

            if (!_unit.GetVariable(BTVariables.Command, out BlackboardVariable<UnitCommands> currentCommand))
                return;

            if (currentCommand.Value == UnitCommands.ConstructBuilding)
                return;

            GameObject closestEnemy = GetClosestEnemy();
            if (closestEnemy == null)
                return;

            _unit.SetVariableValue(BTVariables.TargetGameObject, closestEnemy);
            _unit.SetVariableValue(BTVariables.Command, UnitCommands.Attack);
        }

        /// <summary>
        /// 타겟 이탈 시 다음 타겟으로 전환하거나, 남은 타겟이 없으면 모든 타겟 상실 훅을 호출합니다.
        /// </summary>
        public void HandleUnitExit(IDamageable damageable)
        {
            // 분대원의 목표는 분대 상태 기계가 쥔다. 여기서 바꾸거나 Stop을 걸면
            // 기계는 그 분대원을 이미 자리에 앉은 것으로 알고 있어 아무도 다시 명령하지 않는다
            if (!_unit.CanAutoAttack())
                return;

            if (!_unit.GetVariable(BTVariables.Command, out BlackboardVariable<UnitCommands> currentCommand) ||
                currentCommand.Value != UnitCommands.Attack)
                return;

            if (!_unit.GetVariable(BTVariables.TargetGameObject, out BlackboardVariable<GameObject> targetVariable) ||
                (targetVariable.Value != null && damageable.Transform.gameObject != targetVariable.Value))
                return;

            GameObject closestEnemy = GetClosestEnemy();
            if (closestEnemy != null)
            {
                _unit.SetVariableValue(BTVariables.TargetGameObject, closestEnemy);
            }
            else
            {
                _unit.SetVariableValue<GameObject>(BTVariables.TargetGameObject, null);
                _unit.Stop();
            }
        }

        /// <summary>
        /// 센서에 감지된 대상 중 가장 가까운 적을 반환합니다. 없으면 null입니다.
        /// 코앞의 적과는 시야와 무관하게 교전해야 하므로 안개 가시성을 보지 않습니다.
        /// </summary>
        public GameObject GetClosestEnemy()
        {
            GameObject closestEnemy = null;
            float minSqrDistance = float.MaxValue;
            Vector3 currentPosition = _unit.transform.position;

            foreach (IDamageable damageable in _unit.Sensor.AllInRange)
            {
                if (damageable?.Transform == null || !damageable.IsTargetable)
                    continue;

                float sqrDistance = (damageable.Transform.position - currentPosition).sqrMagnitude;
                if (!(sqrDistance < minSqrDistance))
                    continue;

                minSqrDistance = sqrDistance;
                closestEnemy = damageable.Transform.gameObject;
            }

            return closestEnemy;
        }
    }
}
