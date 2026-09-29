using Code.Buildings.Interfaces;
using Code.Commands.Base;
using Code.Core;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Code.Commands.Types.System
{
    [CreateAssetMenu(fileName = "Cancel construction", menuName = "Units/Commands/Cancel construction", order = 0)]
    public class CancelConstructionCommandSO : BaseCommandSO
    {
        public override bool CanHandle(CommandContext context)
        {
            return context is { Commandable: IBuildingConstructor, MouseButton: MouseButton.Left };
        }

        public override void Handle(CommandContext context)
        {
            // 인터페이스라 ?.이 파괴된 건설자를 통과시킨다
            if (context.Commandable is IBuildingConstructor constructor && UnityRef.IsAlive(constructor))
                constructor.CancelConstruction();
        }

        public override bool IsLocked(CommandContext context)
            => false;
    }
}
