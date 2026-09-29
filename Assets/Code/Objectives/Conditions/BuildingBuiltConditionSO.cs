using Code.Buildings.Data;
using Code.Core.Bus;
using Code.GameEvents.Building;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 특정 건물이 완공되었을 때 완료되는 조건입니다.
    /// TargetBuilding이 null이면 모든 플레이어 건물 완공에 반응합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_BuildingBuilt", menuName = "Objectives/Conditions/BuildingBuilt")]
    public class BuildingBuiltConditionSO : ObjectiveConditionSO
    {
        [field: SerializeField] public BuildingSO TargetBuilding { get; private set; }

        public override ConditionTracker CreateTracker()
            => new BuildingBuiltTracker(TargetBuilding);
    }

    public class BuildingBuiltTracker : ConditionTracker
    {
        private readonly BuildingSO _targetBuilding;

        public BuildingBuiltTracker(BuildingSO targetBuilding)
        {
            _targetBuilding = targetBuilding;
        }

        public override void Subscribe()
            => Bus<BuildingSetupCompleteEvent>.OnEvent[Owner.Player] += OnBuildingBuilt;

        public override void Unsubscribe()
            => Bus<BuildingSetupCompleteEvent>.OnEvent[Owner.Player] -= OnBuildingBuilt;

        private void OnBuildingBuilt(BuildingSetupCompleteEvent e)
        {
            if (_targetBuilding != null && e.Building.BuildingSO != _targetBuilding)
                return;

            Complete();
        }
    }
}
