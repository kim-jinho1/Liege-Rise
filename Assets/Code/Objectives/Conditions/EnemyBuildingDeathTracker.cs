using Code.Core.Bus;
using Code.GameEvents.Building;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 적(Owner.Enemy) 건물 파괴 이벤트 구독/해제를 담당하는 ConditionTracker 공통 베이스입니다.
    /// </summary>
    public abstract class EnemyBuildingDeathTracker : ConditionTracker
    {
        public override void Subscribe()
            => Bus<BuildingDeathEvent>.OnEvent[Owner.Enemy] += OnEnemyBuildingDestroyed;

        public override void Unsubscribe()
            => Bus<BuildingDeathEvent>.OnEvent[Owner.Enemy] -= OnEnemyBuildingDestroyed;

        /// <summary>
        /// 적 건물 1채가 파괴될 때마다 호출됩니다.
        /// </summary>
        protected abstract void OnEnemyBuildingDestroyed(BuildingDeathEvent evt);
    }
}
