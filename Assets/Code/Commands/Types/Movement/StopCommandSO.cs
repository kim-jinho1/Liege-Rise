using Code.Commands.Base;
using Code.Units.Base;
using UnityEngine;

namespace Code.Commands.Types.Movement
{
    [CreateAssetMenu(fileName = "Stop command", menuName = "Units/Commands/Stop", order = 101)]
    public class StopCommandSO : BaseCommandSO
    {
        public override bool CanHandle(CommandContext context)
            => context.Commandable is AbstractUnit;

        public override void Handle(CommandContext context)
        {
            if (context.Commandable is not AbstractUnit unit)
                return;

            unit.Stop();
        }

        public override bool IsLocked(CommandContext context)
            => false;
    }
}
