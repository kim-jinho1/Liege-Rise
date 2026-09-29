using Code.Commands.Base;

namespace Code.Commands.Interfaces
{
    public interface ICommand
    {
        bool IsSingleUnitCommand { get; }
        bool CanHandle(CommandContext context);
        void Handle(CommandContext context);
    }
}
