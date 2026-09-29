using Code.Buildings.Base;
using Code.Commands.Base;
using Code.Environments;
using Code.Units.Pawns;
using UnityEngine;

namespace Code.Commands.Types.Production
{
    /// <summary>
    /// 우클릭 채집 커맨드입니다. 자원 우클릭 → Gather, 건물 우클릭 + 자원 보유 → ReturnSupplies.
    /// </summary>
    [CreateAssetMenu(fileName = "Gather action", menuName = "Units/Commands/Gather", order = 105)]
    public class GatherCommandSO : BaseCommandSO
    {
        public override bool CanHandle(CommandContext context)
        {
            if (context.Commandable is not Pawn pawn)
                return false;

            if (TryGetFlower(context, out _))
                return true;

            // 자원 보유 중일 때만 건물 납품 처리
            return pawn.HasSupplies
                   && context.Hit.collider != null
                   && context.Hit.collider.GetComponentInParent<AbstractBuilding>() != null;
        }

        public override void Handle(CommandContext context)
        {
            if (context.Commandable is not Pawn pawn)
                return;

            if (TryGetFlower(context, out ResourceObject flower))
            {
                pawn.Gather(flower);
                return;
            }

            if (pawn.HasSupplies && context.Hit.collider != null
                && context.Hit.collider.GetComponentInParent<AbstractBuilding>() != null)
            {
                pawn.ReturnSupplies(context.Hit.collider.gameObject);
                return;
            }

            pawn.MoveTo(context.WorldPosition);
        }

        private static bool TryGetFlower(CommandContext context, out ResourceObject resource)
        {
            resource = null;
            return context.Hit.collider != null
                   && context.Hit.collider.TryGetComponent(out resource)
                   && !resource.IsDepleted;
        }

        public override bool IsLocked(CommandContext context)
            => false;
    }
}
