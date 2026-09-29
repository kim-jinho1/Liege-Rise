using Code.Commandable;
using Code.Commandable.Interfaces;
using Code.Core.Bus;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Code.Commands.Base
{
    public struct CommandContext
    {
        public AbstractCommandable Commandable { get; private set; }
        public RaycastHit2D Hit { get; private set; }
        public int UnitIndex { get; private set; }
        public MouseButton MouseButton { get; private set; }
        public Owner Owner { get; private set; }

        /// <summary>
        /// 커맨드 대상 선택 개체입니다. AbstractCommandable이 아닌 대상(분대·단독 지휘관)도 담을 수 있으며,
        /// Commandable 기반 생성자에서는 Commandable과 동일합니다.
        /// </summary>
        public ISelectable Target { get; private set; }

        /// <summary>
        /// 콜라이더 없이도 마우스 클릭 월드 좌표를 전달할 때 사용합니다.
        /// </summary>
        public Vector3 WorldPosition { get; private set; }

        public CommandContext(AbstractCommandable commandable, RaycastHit2D hit, int unitIndex = 0, MouseButton mouseButton = MouseButton.Left)
        {
            Commandable = commandable;
            Target = commandable;
            Hit = hit;
            UnitIndex = unitIndex;
            MouseButton = mouseButton;
            Owner = Owner.Player;
            WorldPosition = hit.point;
        }

        public CommandContext(Owner owner, AbstractCommandable commandable, RaycastHit2D hit, int unitIndex = 0, MouseButton mouseButton = MouseButton.Left)
        {
            Commandable = commandable;
            Target = commandable;
            Hit = hit;
            UnitIndex = unitIndex;
            MouseButton = mouseButton;
            Owner = owner;
            WorldPosition = hit.point;
        }

        public CommandContext(AbstractCommandable commandable, RaycastHit2D hit, int unitIndex, MouseButton mouseButton, Vector3 worldPosition)
        {
            Commandable = commandable;
            Target = commandable;
            Hit = hit;
            UnitIndex = unitIndex;
            MouseButton = mouseButton;
            Owner = Owner.Player;
            WorldPosition = worldPosition;
        }

        /// <summary>
        /// AbstractCommandable이 아닌 선택 대상(분대·단독 지휘관)에 커맨드를 내릴 때 사용합니다.
        /// </summary>
        public CommandContext(Owner owner, ISelectable target, RaycastHit2D hit, int unitIndex, MouseButton mouseButton, Vector3 worldPosition)
        {
            Commandable = target as AbstractCommandable;
            Target = target;
            Hit = hit;
            UnitIndex = unitIndex;
            MouseButton = mouseButton;
            Owner = owner;
            WorldPosition = worldPosition;
        }
    }
}
