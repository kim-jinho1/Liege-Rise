using Code.Commands.Base;
using Code.Military.Commanders;
using Code.Military.Squads;
using UnityEngine;

namespace Code.Military.Commands
{
    /// <summary>
    /// 선택한 분대를 우클릭한 지휘관 밑에 배속하는 커맨드입니다. 다른 지휘관 밑에 있던 분대는 옮겨 갑니다.
    /// 우선순위를 이동보다 높게 두어야 지휘관을 찍은 우클릭이 이동으로 새지 않습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Assign To Commander", menuName = "Military/Commands/Assign To Commander", order = 44)]
    public class AssignToCommanderCommandSO : BaseCommandSO
    {
        public override bool CanHandle(CommandContext context)
            => context.Commandable is Squad squad
               && TryCommander(context, out Commander commander)
               && commander.Roster.CanAccept(squad);

        public override void Handle(CommandContext context)
        {
            if (context.Commandable is Squad squad && TryCommander(context, out Commander commander))
                commander.Roster.Assign(squad);
        }

        public override bool IsLocked(CommandContext context)
            => !CanHandle(context);

        private static bool TryCommander(CommandContext context, out Commander commander)
        {
            commander = null;
            return context.Hit.collider != null
                   && context.Hit.collider.TryGetComponent(out commander)
                   && !commander.IsDead
                   && commander.Owner == context.Commandable.Owner;
        }
    }
}
