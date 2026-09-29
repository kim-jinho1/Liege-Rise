using Code.Commands.Base;
using Code.Core.Bus;
using Code.GameEvents.System;
using Code.Military.Squads;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Code.Military.Commands
{
    /// <summary>
    /// 선택한 분대의 결원 보충을 켜고 끄는 커맨드입니다.
    /// 실제 보충은 분대가 거점 범위 안에 있을 때 SquadReinforcement가 한 명씩 채웁니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Reinforce Squad", menuName = "Military/Commands/Reinforce Squad", order = 44)]
    public class ReinforceSquadCommandSO : BaseCommandSO
    {
        [Header("Notices")]
        [SerializeField] private string outOfRangeNotice = "거점 범위 밖 — 전초나 본성 근처로 가면 보충됩니다";

        private void OnEnable()
            => RequireClickToActivate = false; // 대상 클릭 없이 버튼만으로 발동한다

        /// <summary>
        /// 결원이 있는 아군 분대만 받습니다.
        /// </summary>
        // 우클릭을 받으면 CommandExecutor가 여기서 멈춰 분대 이동 명령을 납치한다
        public override bool CanHandle(CommandContext context)
            => context.MouseButton != MouseButton.Right &&
               context.Target is Squad squad &&
               squad.Owner == context.Owner &&
               squad.Reinforcement != null &&
               squad.MissingCount > 0;

        /// <summary>
        /// 보충을 켜고, 이미 켜져 있으면 끕니다.
        /// </summary>
        public override void Handle(CommandContext context)
        {
            if (context.Target is not Squad squad || !CanHandle(context))
                return;

            if (squad.Reinforcement.IsActive)
            {
                squad.Reinforcement.Stop();
                return;
            }

            squad.Reinforcement.Begin();

            // 켜지긴 하되 거점에 닿아야 채워지므로, 왜 안 늘어나는지 먼저 알려 준다.
            // 문구는 인스펙터에서 비울 수 있으므로 빈 배너가 뜨지 않게 거른다
            if (squad.Reinforcement.CoveringPoint() == null && !string.IsNullOrEmpty(outOfRangeNotice))
                Bus<NoticeEvent>.Raise(context.Owner, new NoticeEvent(outOfRangeNotice, squad.transform.position));
        }

        /// <summary>
        /// 정원이 차 있으면 버튼을 잠급니다.
        /// </summary>
        public override bool IsLocked(CommandContext context)
            => !CanHandle(context);
    }
}
