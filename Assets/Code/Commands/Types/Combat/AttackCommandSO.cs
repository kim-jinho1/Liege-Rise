using Code.Commandable.Interfaces;
using Code.Commands.Base;
using Code.Units.Interfaces;
using UnityEngine;

namespace Code.Commands.Types.Combat
{
    [CreateAssetMenu(fileName = "Attack command", menuName = "Units/Commands/Attack", order = 99)]
    public class AttackCommandSO : BaseCommandSO
    {
        public override bool CanHandle(CommandContext context)
        {
            if (context.Commandable is not IAttacker)
                return false;

            if (context.Hit.collider == null)
                return false;

            if (!context.Hit.collider.TryGetComponent(out IDamageable target))
                return false;

            // 적(다른 Owner)만 공격 대상. 타워에 탄 사수는 목표가 아니다 — 타워를 쳐야 한다
            return target.IsTargetable && target.Owner != context.Commandable.Owner;
        }

        public override void Handle(CommandContext context)
        {
            IAttacker attacker = context.Commandable as IAttacker;
            Debug.Assert(attacker != null, $"Attacker is null, check CanHandle method");

            if (context.Hit.collider.TryGetComponent(out IDamageable damageable))
                attacker.Attack(damageable);
            else
                attacker.Attack(context.Hit.point);
        }

        public override bool IsLocked(CommandContext context) => false;
    }
}
