using Code.Commandable;
using Code.Commands.Base;
using Code.Core.Bus;

namespace Code.GameEvents.Unit
{
    /// <summary>
    /// 플레이어 우클릭이 커맨드를 실제로 실행했을 때입니다. 튜토리얼이 "이동했는가"를 이것으로 봅니다.
    /// </summary>
    public struct CommandIssuedEvent : IEvent
    {
        public Owner Owner { get; private set; }
        public AbstractCommandable Unit { get; private set; }
        public BaseCommandSO Command { get; private set; }

        public CommandIssuedEvent(Owner owner, AbstractCommandable unit, BaseCommandSO command)
        {
            Owner = owner;
            Unit = unit;
            Command = command;
        }
    }
}
