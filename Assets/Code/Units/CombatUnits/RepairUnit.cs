using Code.Commandable.Interfaces;
using Code.Units.BT;
using Code.Units.Combat;
using Code.Units.Interfaces;

namespace Code.Units.CombatUnits
{
    /// <summary>
    /// 아군을 회복시켜주는 유닛
    /// </summary>
    public class RepairUnit : AbstractCombatUnit, IRepairer
    {
        /// <summary>
        /// 자동 공격할 수 없는 유닛입니다.
        /// </summary>
        public override bool CanAutoAttack()
            => false;

        /// <summary>
        /// 지정한 아군 유닛을 수리 대상으로 설정하고 수리 커맨드를 발행합니다.
        /// </summary>
        public void Repair(IDamageable target)
        {
            SetVariableValue(BTVariables.TargetGameObject, target.Transform.gameObject);
            SetVariableValue(BTVariables.Command, UnitCommands.Repair);
        }
    }
}
