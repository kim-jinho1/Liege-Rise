using Code.Core.Bus;
using Code.Economics;

namespace Code.GameEvents.Economy
{
    public struct SupplyEvent : IEvent
    {
        public Owner Owner { get; private set; }
        public int Amount { get; private set; }
        public ResourceSO Supply { get; private set; }

        /// <summary>
        /// 일꾼이 실제로 캐서 납품한 것인지입니다. 시퀀스·치트가 그냥 준 자원과 농장 산출은 false입니다.
        /// 결과 화면의 "모은 자원"이 이것만 셉니다 — 안 가르면 받은 선물까지 캔 것으로 잡힙니다.
        /// </summary>
        public bool FromGathering { get; private set; }

        public SupplyEvent(Owner owner, int amount, ResourceSO supply)
            : this(owner, amount, supply, false) { }

        public SupplyEvent(Owner owner, int amount, ResourceSO supply, bool fromGathering)
        {
            Owner = owner;
            Amount = amount;
            Supply = supply;
            FromGathering = fromGathering;
        }
    }
}
