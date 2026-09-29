using Code.Commands.Base;
using Code.Core.Bus;

namespace Code.GameEvents.System
{
    public struct CommandSelectEvent : IEvent
    {
        public BaseCommandSO Command { get; }

        public CommandSelectEvent(BaseCommandSO command)
        {
            Command = command;
        }
    }
}
