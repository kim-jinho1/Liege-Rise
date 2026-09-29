using Code.Core.Bus;
using Code.Economics;

namespace Code.GameEvents.Economy
{
    /// <summary>
    /// 자원 차감/환불 이벤트 입니다.
    /// </summary>
    public struct ResourceChangedEvent : IEvent
    {
        /// <summary>
        /// 환불을 하는 주체
        /// </summary>
        public Owner Owner { get; private set; }

        /// <summary>
        /// 차감/환불을 하는 자원과 양
        /// </summary>
        public ResourceCostSO Cost { get; private set; }

        /// <summary>
        /// 자원 차감/환불에 적용할 배율입니다. AssemblyBuilding 활성화 시 1.0 미만으로 감소합니다.
        /// </summary>
        public float Multiplier { get; private set; }

        /// <summary>
        /// 환불을 하는지 체크하는 변수 true면 환불 false면 차감
        /// </summary>
        public bool IsRefund { get; private set; }

        public ResourceChangedEvent(Owner owner, ResourceCostSO cost, float multiplier = 1f, bool isRefund = false)
        {
            Owner = owner;
            Cost = cost;
            Multiplier = multiplier;
            IsRefund = isRefund;
        }
    }
}
