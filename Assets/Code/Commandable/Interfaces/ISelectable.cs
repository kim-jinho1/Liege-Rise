namespace Code.Commandable.Interfaces
{
    public interface ISelectable
    {
        bool IsSelected { get; }
        void Select();
        void DeSelect();
    }
}
