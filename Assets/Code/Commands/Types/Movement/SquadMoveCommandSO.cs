using Code.Commands.Base;
using Code.Military.Squads;
using UnityEngine;

namespace Code.Commands.Types.Movement
{
    /// <summary>
    /// 분대 이동 명령을 처리하는 ScriptableObject입니다. 이 명령은 CommandContext에서 분대(Squad) 객체를 확인하고,
    /// 해당 분대를 지정된 월드 위치로 이동시키는 기능을 제공합니다. 또한
    /// CanHandle 메서드를 통해 분대 객체만 처리할 수 있도록 제한하며, IsLocked 메서드를 통해 명령의 잠금 상태를 확인할 수 있습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Squad Move", menuName = "Military/Commands/Squad Move", order = 40)]
    public class SquadMoveCommandSO : BaseCommandSO
    {
        public override bool CanHandle(CommandContext context)
            => context.Commandable is Squad;

        public override void Handle(CommandContext context)
        {
            if (context.Commandable is Squad squad)
                squad.MoveTo(context.WorldPosition);
        }

        public override bool IsLocked(CommandContext context)
            => !CanHandle(context);
    }
}
