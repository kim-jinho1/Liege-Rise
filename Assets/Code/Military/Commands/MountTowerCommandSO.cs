using Code.Buildings.Logic.DefenseBuilding;
using Code.Commands.Base;
using Code.Military.Squads;
using UnityEngine;

namespace Code.Military.Commands
{
    /// <summary>
    /// 선택한 분대를 아군 탑승형 타워에 올리는 커맨드입니다.
    /// 타워를 우클릭했을 때만 성립하며, 땅이나 다른 대상을 클릭하면 false를 내고 이동 명령으로 넘어갑니다.
    /// 분대 프리팹의 AvailableCommands에서 반드시 Squad Move보다 앞에 두어야 합니다 —
    /// CommandExecutor가 첫 CanHandle true에서 멈추므로 뒤에 있으면 이동이 먼저 집어갑니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Mount Tower", menuName = "Military/Commands/Mount Tower", order = 44)]
    public class MountTowerCommandSO : BaseCommandSO
    {
        /// <summary>
        /// 아군 탑승형 타워를 클릭했고, 이 분대가 탑승 가능하며 아직 타워에 없을 때만 받습니다.
        /// </summary>
        public override bool CanHandle(CommandContext context)
        {
            if (context.Commandable is not Squad squad)
                return false;

            if (context.Hit.collider == null ||
                !context.Hit.collider.TryGetComponent(out MannedTowerBuilding tower))
                return false;

            return tower.Owner == squad.Owner &&
                   squad.CanMountTower &&
                   squad.MountedTower == null;
        }

        /// <summary>
        /// 분대를 타워에 올립니다. 슬롯이 모자라면 남은 인원은 타워 밑에 남습니다.
        /// </summary>
        public override void Handle(CommandContext context)
        {
            if (context.Commandable is Squad squad &&
                context.Hit.collider != null &&
                context.Hit.collider.TryGetComponent(out MannedTowerBuilding tower))
                squad.MountTower(tower);
        }

        /// <summary>
        /// 올릴 수 없는 상태이면 잠급니다.
        /// </summary>
        public override bool IsLocked(CommandContext context)
            => !CanHandle(context);
    }
}
