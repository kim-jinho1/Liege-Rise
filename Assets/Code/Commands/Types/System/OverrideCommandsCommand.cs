using Code.Commands.Base;
using UnityEngine;

namespace Code.Commands.Types.System
{
    /// <summary>
    /// 커맨드 패널을 다른 페이지로 갈아 끼웁니다.
    /// Commands에 새 페이지의 커맨드가 들어갑니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Override commands", menuName = "Units/Commands/OverrideCommands", order = 110)]
    public class OverrideCommandsCommand : BaseCommandSO
    {
        [field: SerializeField] public BaseCommandSO[] Commands { get; private set; }
        public override bool CanHandle(CommandContext context)
        {
            return context.Commandable != null;
        }

        public override void Handle(CommandContext context)
        {
            context.Commandable.SetCommandOverrides(Commands);
        }

        public override bool IsLocked(CommandContext context)
            => false;
    }
}
