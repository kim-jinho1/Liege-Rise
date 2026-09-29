namespace Code.Commandable.Interfaces
{
    public interface ISlowAffectable
    {
        void ApplySlow(float slowPercent);
        void ResetSpeed();
    }
}
