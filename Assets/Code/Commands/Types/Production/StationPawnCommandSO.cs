using Code.Buildings.Base;
using Code.Buildings.Logic.Shared;
using Code.Commands.Base;
using Code.Units.Pawns;
using UnityEngine;

namespace Code.Commands.Types.Production
{
    /// <summary>
    /// 일꾼을 고른 채 일꾼을 받는 건물(농장·전초·병영)을 우클릭하면 그 건물에 들어가 일합니다.
    /// AssignPawnCommandSO는 건물 쪽 버튼이라 우클릭을 거부합니다 — 이쪽이 일꾼 쪽 우클릭 경로입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Station pawn", menuName = "Units/Commands/Station Pawn", order = 106)]
    public class StationPawnCommandSO : BaseCommandSO
    {
        public override bool CanHandle(CommandContext context)
            => context.Commandable is Pawn pawn && TryGetHost(context, pawn, out _);

        public override void Handle(CommandContext context)
        {
            if (context.Commandable is Pawn pawn && TryGetHost(context, pawn, out AbstractBuilding building))
                pawn.StationAt(building);
        }

        public override bool IsLocked(CommandContext context)
            => false;

        /// <summary>
        /// 우클릭한 자리의 아군 건물에 빈 일꾼 자리가 있으면 true.
        /// </summary>
        private static bool TryGetHost(CommandContext context, Pawn pawn, out AbstractBuilding building)
        {
            PawnStationHost host;
            building = null;
            if (context.Hit.collider == null)
                return false;

            building = context.Hit.collider.GetComponentInParent<AbstractBuilding>();
            if (building == null || building.IsDead || building.Owner != pawn.Owner)
                return false;

            host = building.GetComponent<PawnStationHost>();
            return host != null && host.HasSpace;
        }
    }
}
