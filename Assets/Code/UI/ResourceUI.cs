using Code.Core.Bus;
using Code.Economics;
using Code.Players;
using Code.UI.Components;
using UnityEngine;

namespace Code.UI
{
    /// <summary>
    /// 자원 슬롯 UI를 관리하는 컴포넌트입니다.
    /// ResourceSlotUI를 여러 개 포함하며, UserSupplies의 변경 이벤트를 구독하여 해당 슬롯을 갱신합니다.
    /// </summary>
    public class ResourceUI : MonoBehaviour
    {
        [SerializeField] private Owner owner;
        [SerializeField] private ResourceSlotUI[] slots;

        private void Start()
        {
            UserSupplies supplies = UserSupplies.Instance;
            if (supplies == null)
                return;

            foreach (ResourceSlotUI slotUI in slots)
            {
                if (slotUI == null)
                    continue;

                slotUI.EnableFor(supplies, owner);
            }

            supplies.OnSupplyChanged += HandleSupplyChanged;
            supplies.OnSupplyUpdateValue += HandleSupplyDelta;
        }

        private void OnDestroy()
        {
            if (UserSupplies.Instance == null)
                return;

            UserSupplies.Instance.OnSupplyChanged -= HandleSupplyChanged;
            UserSupplies.Instance.OnSupplyUpdateValue -= HandleSupplyDelta;
        }

        /// <summary>
        /// 변경된 자원에 해당하는 슬롯만 갱신합니다.
        /// </summary>
        private void HandleSupplyChanged(Owner changedOwner, int newAmount, ResourceSO resource)
        {
            if (changedOwner != owner)
                return;

            foreach (ResourceSlotUI slot in slots)
                if (slot != null && slot.SupplySO == resource)
                    slot.UpdateValue(newAmount);
        }

        /// <summary>
        /// 변화량을 해당 슬롯 위에 잠깐 띄웁니다. 획득은 +, 소비는 -로 표시됩니다.
        /// </summary>
        private void HandleSupplyDelta(Owner changedOwner, int delta, ResourceSO resource)
        {
            if (changedOwner != owner || delta == 0)
                return;

            foreach (ResourceSlotUI slot in slots)
                if (slot != null && slot.SupplySO == resource)
                    slot.ShowDelta(delta);
        }
    }
}
