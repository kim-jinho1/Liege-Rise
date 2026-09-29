using Code.Buildings.Logic.Shared;
using Code.Commands.Base;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Code.Commands.Types.Production
{
    /// <summary>
    /// 선택한 건물에서 일꾼 한 기를 빼내는 커맨드입니다.
    /// PawnStationHost만 보므로 농장·전초·병영 어디에 꽂아도 같게 동작합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Release Pawn", menuName = "Buildings/Commands/Release Pawn", order = 61)]
    public class ReleasePawnCommandSO : BaseCommandSO
    {
        private void OnEnable()
            => RequireClickToActivate = false; // 대상 클릭 없이 버튼만으로 발동한다

        /// <summary>
        /// 일꾼이 상주하는 건물만 받습니다.
        /// </summary>
        // 우클릭을 받으면 CommandExecutor가 여기서 멈춰 이동 명령을 납치한다
        public override bool CanHandle(CommandContext context)
            => context.MouseButton != MouseButton.Right && Host(context) != null;

        /// <summary>
        /// 일꾼 한 기를 내보냅니다.
        /// </summary>
        public override void Handle(CommandContext context)
        {
            PawnStationHost host = Host(context);
            if (host != null)
                host.ReleaseOne();
        }

        /// <summary>
        /// 상주 중인 일꾼이 없으면 버튼을 잠급니다.
        /// </summary>
        public override bool IsLocked(CommandContext context)
        {
            PawnStationHost host = Host(context);
            return host == null || host.Count <= 0;
        }

        /// <summary>
        /// 대상에서 일꾼 배치 컴포넌트를 찾습니다. 없으면 null입니다.
        /// </summary>
        private static PawnStationHost Host(CommandContext context)
            => context.Commandable == null ? null : context.Commandable.GetComponent<PawnStationHost>();
    }
}
