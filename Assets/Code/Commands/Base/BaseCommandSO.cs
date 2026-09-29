using Ami.BroAudio;
using Code.Commands.Interfaces;
using Code.Core.Bus;
using Code.GameEvents.System;
using Code.Players.Cursors;
using Code.Progression;
using UnityEngine;

namespace Code.Commands.Base
{
    public abstract class BaseCommandSO : ScriptableObject, ICommand
    {
        [field: SerializeField] public string Name { get; private set; } = "Command";
        [field: SerializeField] public Sprite Icon { get; private set; }

        [field: SerializeField, Range(-1, 7)] public int Slot { get; private set; }
        [field: SerializeField] public int Priority { get; private set; } // 우클릭 시 우선 순위 표시

        // 이 커맨드가 우클릭에 걸릴 때 띄울 커서. 비워 두면 기본 커서다
        [field: SerializeField] public CursorKind HoverCursor { get; private set; }

        [field: SerializeField] public bool IsSingleUnitCommand { get; protected set; }
        [field: SerializeField] public bool RequireClickToActivate { get; protected set; } = true;

        /// <summary>
        /// 이 커맨드가 표시되기 위해 필요한 해금 키입니다. 비어 있으면 항상 표시되고,
        /// 값이 있으면 ProgressService에서 해금된 경우에만 표시됩니다(스테이지 클리어로 해금).
        /// </summary>
        [field: SerializeField] public string UnlockKey { get; private set; }

        /// <summary>
        /// 잠긴 버튼을 눌렀을 때 띄울 안내입니다. 비워 두면 아무것도 알리지 않습니다.
        /// 커맨드마다 이유가 달라 여기 두면 새 커맨드가 에셋만으로 따라옵니다.
        /// </summary>
        [field: SerializeField] public string LockedNotice { get; private set; }

        /// <summary>
        /// 이 명령이 실제로 실행됐을 때 낼 소리입니다. 비워 두면 공용 명령음이 납니다.
        /// 문구와 같은 이유로 여기 둡니다 — 새 커맨드가 에셋만으로 자기 소리를 갖습니다.
        /// </summary>
        [field: SerializeField] public SoundID OrderSound { get; private set; }

        public abstract bool CanHandle(CommandContext context);
        public abstract void Handle(CommandContext context);

        /// <summary>
        /// 이 커맨드가 지금 잠겨 있는지 판정합니다. true면 커맨드 패널이 버튼을 비활성으로 그립니다.
        /// </summary>
        public abstract bool IsLocked(CommandContext context);

        /// <summary>
        /// 해금 키가 없거나 진척상 해금되었으면 true를 반환합니다(컨텍스트 불필요, UI 표시 필터용).
        /// </summary>
        public bool IsUnlockedByProgress()
            => string.IsNullOrEmpty(UnlockKey) || ProgressService.IsUnlocked(UnlockKey);

        /// <summary>
        /// 눌렀는데 아무 일도 일어나지 않았을 때 이유를 한 줄로 알립니다.
        /// 이유가 여럿인 커맨드는 이 메서드를 재정의해 갈라 씁니다.
        /// </summary>
        public virtual void NotifyRefusal(CommandContext context)
        {
            if (!string.IsNullOrEmpty(LockedNotice))
                Bus<NoticeEvent>.Raise(context.Owner, new NoticeEvent(LockedNotice, context.WorldPosition));
        }
    }
}
