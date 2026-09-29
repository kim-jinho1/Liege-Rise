using Code.Buildings.Logic.ProductionBuilding;
using Code.Commands.Base;
using Code.Core.Bus;
using Code.GameEvents.System;
using Code.Military.Data;
using Code.UI;
using UnityEngine;

namespace Code.Commands.Types.Production
{
    /// <summary>
    /// 병영의 지휘관 생산 버튼 커맨드입니다. 지휘관은 분대처럼 병영에서 시간과 자원으로 뽑습니다 —
    /// 분대에 임명하는 방식은 처음 보는 사람이 어디서 왔는지 몰라 접었습니다(2026-09-07).
    /// </summary>
    [CreateAssetMenu(fileName = "Build Commander", menuName = "Buildings/Commands/BuildCommander", order = 122)]
    public class BuildCommanderCommandSO : BaseCommandSO
    {
        [field: SerializeField] public CommanderSO Commander { get; private set; }

        [Header("Notices")]
        [SerializeField] private string queueFullNotice = "생산 대기열이 가득 찼습니다";

        private void OnEnable()
            => RequireClickToActivate = false;

        public override void NotifyRefusal(CommandContext context)
        {
            if (context.Commandable is not SquadRecruitmentBuilding building)
                return;

            string message = building.RefusalFor(Commander) switch
            {
                SquadOrderRefusal.QueueFull => queueFullNotice,
                SquadOrderRefusal.NotEnoughSupplies => TextFormat.NotEnoughSupplies,
                _ => LockedNotice
            };

            if (!string.IsNullOrEmpty(message))
                Bus<NoticeEvent>.Raise(context.Owner, new NoticeEvent(message, building.transform.position));
        }

        public override bool CanHandle(CommandContext context)
            => Commander != null
               && context.Commandable is SquadRecruitmentBuilding building
               && building.CanOrder(Commander);

        public override void Handle(CommandContext context)
        {
            if (context.Commandable is SquadRecruitmentBuilding building)
                building.TryOrder(Commander);
        }

        public override bool IsLocked(CommandContext context)
            => !CanHandle(context);
    }
}
