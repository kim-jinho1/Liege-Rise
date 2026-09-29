using Code.Units.Combat;

namespace Code.Commandable.Interfaces
{
    /// <summary>
    /// 버프를 받을 수 있는 유닛 인터페이스입니다.
    /// </summary>
    public interface IBuffable
    {
        /// <summary>
        /// 버프를 추가하고 배율을 재계산합니다.
        /// </summary>
        void AddBuff(BuffConfigSO config);

        /// <summary>
        /// 버프를 제거하고 배율을 재계산합니다.
        /// </summary>
        void RemoveBuff(BuffConfigSO config);
    }
}
