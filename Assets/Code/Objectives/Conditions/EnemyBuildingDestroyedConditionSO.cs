using Code.Buildings.Data;
using Code.GameEvents.Building;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 지정한 적 건물이 파괴되었을 때 완료되는 조건입니다. TargetBuilding이 null이면 모든 적 건물 파괴에 반응합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_EnemyBuildingDestroyed", menuName = "Objectives/Conditions/EnemyBuildingDestroyed")]
    public class EnemyBuildingDestroyedConditionSO : ObjectiveConditionSO
    {
        [field: SerializeField] public BuildingSO TargetBuilding { get; private set; }

        public override ConditionTracker CreateTracker()
            => new EnemyBuildingDestroyedTracker(TargetBuilding);
    }

    public class EnemyBuildingDestroyedTracker : EnemyBuildingDeathTracker
    {
        private readonly BuildingSO _targetBuilding;

        public EnemyBuildingDestroyedTracker(BuildingSO targetBuilding)
            => _targetBuilding = targetBuilding;

        protected override void OnEnemyBuildingDestroyed(BuildingDeathEvent evt)
        {
            if (_targetBuilding == null)
            {
                Complete();
                return;
            }
            BuildingSO destroyed = evt.Building.BuildingSO;
            if (destroyed != null && !string.IsNullOrEmpty(destroyed.Name)
                && destroyed.Name == _targetBuilding.Name)
                Complete();
        }
    }
}
