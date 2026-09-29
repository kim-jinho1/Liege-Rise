using Code.Commandable;
using Code.Commandable.Interfaces;
using Code.Commands.Base;
using Code.Units.Interfaces;
using UnityEngine;

namespace Code.Commands.Types.Combat
{
    /// <summary>
    /// RepairBot이 지정한 아군 유닛을 수리하도록 명령하는 커맨드입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Repair command", menuName = "Units/Commands/Repair", order = 99)]
    public class RepairCommandSO : BaseCommandSO
    {
        /// <summary>
        /// 선택 유닛이 IRepairer이고, 클릭 대상이 같은 Owner의 IDamageable일 때 처리합니다.
        /// </summary>
        public override bool CanHandle(CommandContext context)
        {
            if (context.Commandable is not IRepairer)
                return false;

            if (context.Hit.collider == null)
                return false;

            if (!context.Hit.collider.TryGetComponent(out IDamageable target))
                return false;

            // 아군 유닛만 수리 가능
            return target.Transform.TryGetComponent(out AbstractCommandable commandable)
                   && commandable.Owner == context.Owner;
        }

        /// <summary>
        /// 클릭한 아군 유닛을 수리 대상으로 설정합니다.
        /// </summary>
        public override void Handle(CommandContext context)
        {
            IRepairer repairer = context.Commandable as IRepairer;
            Debug.Assert(repairer != null, "RepairCommandSO: IRepairer is null, check CanHandle");

            context.Hit.collider.TryGetComponent(out IDamageable target);
            repairer.Repair(target);
        }

        public override bool IsLocked(CommandContext context)
            => false;
    }
}
