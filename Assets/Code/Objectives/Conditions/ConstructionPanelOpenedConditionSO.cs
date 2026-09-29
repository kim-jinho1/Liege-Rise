using Code.Core.Bus;
using Code.GameEvents.System;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 건설 패널이 열렸을 때 완료되는 조건입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_ConstructionPanelOpened", menuName = "Objectives/Conditions/ConstructionPanelOpened")]
    public class ConstructionPanelOpenedConditionSO : ObjectiveConditionSO
    {
        public override ConditionTracker CreateTracker()
            => new ConstructionPanelOpenedTracker();
    }

    public class ConstructionPanelOpenedTracker : ConditionTracker
    {
        public override void Subscribe()
            => Bus<BuildPanelToggledEvent>.OnEvent[Owner.Player] += OnToggled;

        public override void Unsubscribe()
            => Bus<BuildPanelToggledEvent>.OnEvent[Owner.Player] -= OnToggled;

        private void OnToggled(BuildPanelToggledEvent evt)
        {
            if (evt.IsOpen)
                Complete();
        }
    }
}
