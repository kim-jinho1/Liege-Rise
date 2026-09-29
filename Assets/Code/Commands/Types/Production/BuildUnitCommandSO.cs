using Code.Buildings.Base;
using Code.Commandable;
using Code.Commands.Base;
using Code.Core.Bus;
using Code.GameEvents.System;
using Code.Players;
using Code.UI;
using Code.Units.Pawns;
using UnityEngine;

namespace Code.Commands.Types.Production
{
    /// <summary>
    /// 건물의 생산 큐에 유닛을 넣는 커맨드입니다. 폰이면 폰 상한(본부·집)까지만 받습니다 —
    /// 큐에 이미 든 폰도 자리를 차지한 것으로 셉니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Build Unit", menuName = "Buildings/Commands/BuildUnit", order = 120)]
    public class BuildUnitCommandSO : BaseCommandSO
    {
        [field: SerializeField] public AbstractCommandableSO Unit { get; private set; }

        [SerializeField] private string populationNotice = "일꾼 한도에 걸렸습니다 — 집을 지으세요";

        private void OnEnable()
            => RequireClickToActivate = false;

        public override bool CanHandle(CommandContext context)
            => Unit != null
               && context.Commandable is AbstractBuilding building
               && HasRoom(context.Owner, building)
               && UserSupplies.Instance != null
               && UserSupplies.Instance.HasEnoughSupplies(context.Owner, Unit.Cost);

        public override void Handle(CommandContext context)
        {
            if (CanHandle(context) && context.Commandable is AbstractBuilding building)
                building.BuildUnlockable(Unit);
        }

        public override bool IsLocked(CommandContext context)
            => !CanHandle(context);

        public override void NotifyRefusal(CommandContext context)
        {
            if (Unit == null || context.Commandable is not AbstractBuilding building)
                return;

            string message = !HasRoom(context.Owner, building) ? populationNotice
                : UserSupplies.Instance != null && !UserSupplies.Instance.HasEnoughSupplies(context.Owner, Unit.Cost)
                    ? TextFormat.NotEnoughSupplies
                    : LockedNotice;

            if (!string.IsNullOrEmpty(message))
                Bus<NoticeEvent>.Raise(context.Owner, new NoticeEvent(message, building.transform.position));
        }

        private bool HasRoom(Owner owner, AbstractBuilding building)
        {
            if (Unit is not PawnSO || UserPopulation.Instance == null)
                return true;

            // 대기 목록은 진행 중인 것을 빼고 주므로 그것도 따로 센다
            int queued = building.CurrentItem is PawnSO ? 1 : 0;
            foreach (UnlockableSO item in building.QueuedItems)
                if (item is PawnSO)
                    queued++;

            return UserPopulation.Instance.GetPawnCount(owner) + queued
                   < UserPopulation.Instance.GetPawnCapacity(owner);
        }
    }
}
