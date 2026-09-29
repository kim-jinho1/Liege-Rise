using Code.Commands.Base;
using Code.Military.Squads;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Code.Military.Commands
{
    /// <summary>
    /// 타워에 올라간 분대를 내리는 커맨드입니다. 패널 버튼 전용입니다 —
    /// 탑승 중에는 상태 기계가 Locked라 우클릭 명령이 분대에 도달하지 않습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Dismount Tower", menuName = "Military/Commands/Dismount Tower", order = 45)]
    public class DismountTowerCommandSO : BaseCommandSO
    {
        private void OnEnable()
            => RequireClickToActivate = false; // 대상 클릭 없이 버튼만으로 발동한다

        /// <summary>
        /// 타워에 올라가 있는 분대일 때만 받습니다.
        /// 우클릭 컨텍스트는 거부합니다 — CommandExecutor가 첫 CanHandle true에서 멈추기 때문입니다.
        /// </summary>
        public override bool CanHandle(CommandContext context)
            => context.MouseButton != MouseButton.Right &&
               context.Target is Squad { MountedTower: not null };

        /// <summary>
        /// 분대를 타워에서 내립니다.
        /// </summary>
        public override void Handle(CommandContext context)
        {
            if (context.Target is Squad squad)
                squad.DismountTower();
        }

        /// <summary>
        /// 타워에 올라가 있지 않으면 버튼을 잠급니다.
        /// </summary>
        public override bool IsLocked(CommandContext context)
            => !CanHandle(context);
    }
}
