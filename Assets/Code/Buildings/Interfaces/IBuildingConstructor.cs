using Code.Core.Bus;

namespace Code.Buildings.Interfaces
{
    public interface IBuildingConstructor
    {
        Owner Owner { get; }
        bool IsBuilding { get; }

        /// <summary>
        /// 다른 작업에 매이지 않아 건설에 자동 배정될 수 있는(유휴) 상태인지 여부입니다.
        /// </summary>
        bool IsAvailable { get; }

        void CancelConstruction();
    }
}
