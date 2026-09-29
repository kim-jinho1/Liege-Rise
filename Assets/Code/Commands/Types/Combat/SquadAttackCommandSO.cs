using Code.Commandable.Interfaces;
using Code.Commands.Base;
using Code.Military.Squads;
using UnityEngine;

namespace Code.Commands.Types.Combat
{
    /// <summary>
    /// 분대 지정 공격 명령입니다. 적 IDamageable을 우클릭했을 때만 성립하며,
    /// 분대를 접근(Approaching) 상태로 넣습니다. 땅을 클릭하면 false를 내고 분대 이동으로 넘어갑니다.
    /// 기존 Attack command는 IAttacker 게이트라 Squad를 처리하지 못합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Squad Attack", menuName = "Military/Commands/Squad Attack", order = 41)]
    public class SquadAttackCommandSO : BaseCommandSO
    {
        public override bool CanHandle(CommandContext context)
        {
            if (context.Commandable is not Squad)
                return false;

            if (context.Hit.collider == null)
                return false;

            if (!context.Hit.collider.TryGetComponent(out IDamageable target))
                return false;

            // 타워에 탄 사수를 찍으면 성립시키지 않는다 — 이동으로 넘어가 타워 앞에 서면 센서가 타워를 문다
            return target.IsTargetable && target.Owner != context.Commandable.Owner; // 적만
        }

        public override void Handle(CommandContext context)
        {
            if (context.Commandable is Squad squad &&
                context.Hit.collider.TryGetComponent(out IDamageable target))
                squad.Attack(target);
        }

        public override bool IsLocked(CommandContext context)
            => !CanHandle(context);
    }
}
