using Code.Commands.Base;
using UnityEngine;

namespace Code.Commands.Types.System
{
    /// <summary>
    /// CommandUI에서 이전 커맨드 페이지로 돌아가는 커맨드입니다.
    /// OverrideCommandsCommand의 Commands[] 안에 배치하면 Cancel 역할을 합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Go Back", menuName = "Units/Commands/GoBack", order = 111)]
    public class GoBackCommandSO : BaseCommandSO
    {
        // 우클릭이 이 커맨드를 집으면 이동 명령을 납치하므로 항상 false다. 버튼은 Handle을 직접 부른다
        public override bool CanHandle(CommandContext context)
            => false;

        /// <summary>
        /// 커맨드 교체를 되돌려 원래 페이지로 돌아갑니다.
        /// </summary>
        public override void Handle(CommandContext context)
        {
            if (context.Commandable != null)
                context.Commandable.SetCommandOverrides(null);
        }

        public override bool IsLocked(CommandContext context)
            => false;
    }
}
