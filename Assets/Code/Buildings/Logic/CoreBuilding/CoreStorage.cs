using Code.Core.Bus;
using Code.Economics;
using Code.GameEvents.Economy;
using UnityEngine;

namespace Code.Buildings.Logic.CoreBuilding
{
    /// <summary>
    /// 플레이어 자원 저장소 역할을 하는 컴포넌트입니다.
    /// DeliverResources 호출 시 SupplyEvent를 발행해 UserSupplies에 자원을 추가합니다.
    /// </summary>
    public class CoreStorage : MonoBehaviour
    {
        [field: SerializeField] public Owner Owner { get; private set; } = Owner.Player;

        /// <summary>
        /// 일벌이 자원을 납품합니다. SupplyEvent를 발행해 UserSupplies에 반영합니다.
        /// </summary>
        public void DeliverResources(ResourceSO resource, int amount)
        {
            if (resource == null || amount <= 0)
                return;

            // 여기로 들어오는 것만 "캐 온 것"이다 — 시퀀스가 준 자원·농장 산출과 구분된다
            Bus<SupplyEvent>.Raise(Owner, new SupplyEvent(Owner, amount, resource, true));
        }
    }
}
