using Code.Commandable;

namespace Code.GameEvents.System
{
    public struct RefreshData
    {
        public AbstractCommandableSO Unit { get; private set; }

        public RefreshData(AbstractCommandableSO unit)
        {
            Unit = unit;
        }
    }
}
