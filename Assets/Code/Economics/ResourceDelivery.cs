using Code.Core.Bus;
using Code.GameEvents.Economy;
using UnityEngine;

namespace Code.Economics
{
    /// <summary>
    /// 일꾼이 보유한 자원을 어디에 입금할지 결정하는 단일 규칙입니다.
    /// 대상이 해당 자원을 받는 가공 건물이면 입력 버퍼에, 그 외에는 UserSupplies에 입금합니다.
    /// </summary>
    public static class ResourceDelivery
    {
        /// <summary>
        /// 자원을 플레이어 경제(UserSupplies)에 입금합니다.
        /// </summary>
        public static void DepositToSupplies(Owner owner, ResourceSO resource, int amount)
        {
            if (resource == null || amount <= 0)
                return;

            // 이 경로는 일꾼이 지고 온 것만 지난다 — 결과 화면의 "모은 자원"이 이 표시로 센다
            Bus<SupplyEvent>.Raise(owner, new SupplyEvent(owner, amount, resource, true));
        }

        /// <summary>
        /// 단일 입금 규칙입니다. 대상이 가공 건물이면 입력 버퍼에만 납품하고 수령되지 못한 잔량을 반환합니다(잔량은 휴대 유지).
        /// 가공 건물이 아니면(본성 등) 전량을 UserSupplies에 입금하고 0을 반환합니다.
        /// </summary>
        public static int Deliver(Owner owner, GameObject target, ResourceSO resource, int amount)
        {
            if (resource == null || amount <= 0)
                return 0;

            IResourceReceiver receiver = FindAcceptingReceiver(target, resource);
            if (receiver != null)
                return amount - receiver.DeliverResources(resource, amount);

            DepositToSupplies(owner, resource, amount);
            return 0;
        }

        /// <summary>
        /// 대상 GameObject에서 해당 자원을 받는 수령자(IResourceReceiver)를 찾습니다. 없으면 null.
        /// </summary>
        private static IResourceReceiver FindAcceptingReceiver(GameObject target, ResourceSO resource)
        {
            if (target == null || resource == null)
                return null;

            IResourceReceiver receiver = target.GetComponentInParent<IResourceReceiver>();
            return receiver != null && receiver.AcceptsResource(resource) ? receiver : null;
        }
    }
}
