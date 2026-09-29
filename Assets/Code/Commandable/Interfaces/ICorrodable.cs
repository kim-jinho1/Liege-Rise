namespace Code.Commandable.Interfaces
{
    /// <summary>
    /// 부식 공격을 받을 수 있는 유닛이 구현하는 인터페이스입니다.
    /// 드론봇은 즉사, 방어 유닛은 지속 데미지(DoT)를 받습니다.
    /// </summary>
    public interface ICorrodable
    {
        /// <summary>
        /// 부식 효과를 적용합니다.
        /// </summary>
        void ApplyCorrosion(int damagePerTick, float tickInterval, float duration);
    }
}
