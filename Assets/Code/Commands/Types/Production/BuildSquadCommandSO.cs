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
    /// 병영(SquadRecruitmentBuilding)의 분대 생산 버튼 커맨드입니다.
    /// 비용·생산 목록 검증은 건물 측 CanOrder에 위임합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Build Squad", menuName = "Buildings/Commands/BuildSquad", order = 121)]
    public class BuildSquadCommandSO : BaseCommandSO
    {
        /// <summary>
        /// 생산할 분대 SO
        /// </summary>
        [field: SerializeField] public SquadSO Squad { get; private set; }

        // 대기열·한도는 병영에만 있는 사정이라 여기가 제자리다.
        // 자원 부족은 여러 커맨드가 같은 말을 써서 TextFormat이 쥔다
        [Header("Notices")]
        [SerializeField] private string queueFullNotice = "생산 대기열이 가득 찼습니다";
        [SerializeField] private string populationNotice = "분대 한도에 걸렸습니다 — 지휘관을 뽑으세요";

        private void OnEnable()
            => RequireClickToActivate = false;

        /// <summary>
        /// 왜 훈련이 시작되지 않았는지 건물에 물어 그대로 알립니다.
        /// </summary>
        public override void NotifyRefusal(CommandContext context)
        {
            if (context.Commandable is not SquadRecruitmentBuilding building)
                return;

            string message = building.RefusalFor(Squad) switch
            {
                SquadOrderRefusal.QueueFull => queueFullNotice,
                SquadOrderRefusal.PopulationCap => populationNotice,
                SquadOrderRefusal.NotEnoughSupplies => TextFormat.NotEnoughSupplies,
                _ => LockedNotice
            };

            if (!string.IsNullOrEmpty(message))
                Bus<NoticeEvent>.Raise(context.Owner, new NoticeEvent(message, building.transform.position));
        }

        public override bool CanHandle(CommandContext context)
        {
            return Squad != null &&
                   context.Commandable is SquadRecruitmentBuilding building &&
                   building.CanOrder(Squad);
        }

        public override void Handle(CommandContext context)
        {
            if (context.Commandable is SquadRecruitmentBuilding building)
                building.TryOrder(Squad);
        }

        public override bool IsLocked(CommandContext context)
        {
            return Squad == null ||
                   context.Commandable is not SquadRecruitmentBuilding building ||
                   !building.CanOrder(Squad);
        }
    }
}
