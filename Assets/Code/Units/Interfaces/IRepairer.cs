
using Code.Commandable.Interfaces;

namespace Code.Units.Interfaces
{
    /// <summary>
    /// 아군 유닛을 수리할 수 있는 유닛 인터페이스입니다.
    /// </summary>
    public interface IRepairer
    {
        /// <summary>
        /// 지정한 대상을 수리 목표로 설정하고 수리 커맨드를 발행합니다.
        /// </summary>
        void Repair(IDamageable target);
    }
}
