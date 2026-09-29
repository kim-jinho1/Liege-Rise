using Code.Core.Bus;
using Code.GameEvents.Economy;
using Code.Players;
using Code.UI.Components;
using UnityEngine;

namespace Code.UI
{
    /// <summary>
    /// 인구 수 UI를 관리하는 컴포넌트입니다.
    /// </summary>
    public class PopulationUI : MonoBehaviour
    {
        [SerializeField] private Owner owner;
        [SerializeField] private RatioSlotUI pawnSlot;
        [SerializeField] private RatioSlotUI squadSlot;

        private void Awake()
        {
            pawnSlot.SetValue(0,0);
            squadSlot.SetValue(0,0);
        }

        private void Start()
        {
            Bus<PopulationChangedEvent>.OnEvent[owner] += HandlePopulationChanged;
            if (UserPopulation.Instance == null)
                return;

            UserPopulation instance = UserPopulation.Instance;
            pawnSlot.SetValue(instance.GetPawnCount(owner), instance.GetPawnCapacity(owner));
            squadSlot.SetValue(instance.GetSquadCount(owner), instance.GetSquadCapacity(owner));
        }

        private void OnDestroy()
        {
            Bus<PopulationChangedEvent>.OnEvent[owner] -= HandlePopulationChanged;
        }

        /// <summary>
        /// 인구 수가 변경될 때 UI를 업데이트합니다.
        /// </summary>
        private void HandlePopulationChanged(PopulationChangedEvent evt)
        {
            pawnSlot.SetValue(evt.PawnCount, evt.PawnCapacity);
            squadSlot.SetValue(evt.SquadCount, evt.SquadCapacity);

            // 한도만 바뀐 경우에는 델타가 0이라 아무것도 뜨지 않는다.
            pawnSlot.ShowDelta(evt.PawnDelta);
            squadSlot.ShowDelta(evt.SquadDelta);
        }
    }
}
