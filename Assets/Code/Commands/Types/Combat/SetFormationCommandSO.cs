using System.Collections.Generic;
using Code.Commands.Base;
using Code.Military.Commanders;
using Code.Military.Squads;
using Code.Military.Squads.Formations;
using UnityEngine;

namespace Code.Commands.Types.Combat
{
    /// <summary>
    /// 대형을 지정하는 UI 전용 커맨드입니다. 스킬 버튼처럼 즉시 발동하며, 대상 클릭이 필요 없습니다.
    /// 대형은 지휘관이 내립니다 — 지휘관을 고르고 누르면 거느린 분대가 한꺼번에 그 대형으로 섭니다.
    /// 분대를 직접 고른 경로도 남겨 두어, 아직 배속되지 않은 분대도 대형을 바꿀 수 있습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Set Formation", menuName = "Military/Commands/Set Formation", order = 42)]
    public class SetFormationCommandSO : BaseCommandSO
    {
        [field: SerializeField] public FormationSO Formation { get; private set; }

        /// <summary>
        /// 항상 false입니다. CommandExecutor는 AvailableCommands를 순회하며 첫 CanHandle true에서
        /// 멈추므로, 여기서 true를 내면 우클릭 이동이 대형 변경으로 납치됩니다.
        /// 이 커맨드는 UI 버튼이 Handle을 직접 호출하는 경로로만 실행됩니다.
        /// </summary>
        public override bool CanHandle(CommandContext context)
            => false;

        public override void Handle(CommandContext context)
        {
            if (context.Target is Squad squad)
            {
                squad.SetFormation(Formation);
                return;
            }

            if (context.Target is not Commander commander)
                return;

            // 대형 변경은 명부를 건드리지 않으므로 그대로 순회해도 된다
            IReadOnlyList<Squad> squads = commander.Roster.Squads;
            for (int i = 0; i < squads.Count; i++)
                if (squads[i] != null && squads[i].CanUseFormation(Formation))
                    squads[i].SetFormation(Formation);
        }

        /// <summary>
        /// 이 대형을 쓸 수 있는 대상이 하나도 없으면 잠급니다.
        /// 분대를 거느리지 않은 지휘관에게도 잠깁니다 — 눌러도 아무 일이 없으면 고장으로 보입니다.
        /// </summary>
        public override bool IsLocked(CommandContext context)
            => context.Target switch
            {
                Squad squad => !squad.CanUseFormation(Formation),
                Commander commander => !AnySquadCanUse(commander),
                _ => true
            };

        private bool AnySquadCanUse(Commander commander)
        {
            IReadOnlyList<Squad> squads = commander.Roster.Squads;
            for (int i = 0; i < squads.Count; i++)
                if (squads[i] != null && squads[i].CanUseFormation(Formation))
                    return true;

            return false;
        }
    }
}
