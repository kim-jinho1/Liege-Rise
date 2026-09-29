using System.Collections.Generic;
using Code.Buildings.Base;
using Code.Buildings.Logic.CoreBuilding;
using Code.Core.Bus;
using Code.GameEvents.Building;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 적의 모든 본거지(HeadquartersMarker가 붙은 건물)가 파괴되면 완료되는 조건입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_AllEnemyHeadquartersDestroyed", menuName = "Objectives/Conditions/AllEnemyHeadquartersDestroyed")]
    public class AllEnemyHeadquartersDestroyedConditionSO : ObjectiveConditionSO
    {
        public override ConditionTracker CreateTracker()
            => new AllEnemyHeadquartersDestroyedTracker();
    }

    public class AllEnemyHeadquartersDestroyedTracker : EnemyBuildingDeathTracker
    {
        public override int Current
            => _total - _remaining.Count;

        public override int Target => _total;

        private readonly HashSet<AbstractBuilding> _remaining = new();
        private int _total;

        /// <summary>
        /// 현재 살아있는 적 본거지를 수집한 뒤 파괴 이벤트를 구독합니다.
        /// </summary>
        public override void Subscribe()
        {
            foreach (HeadquartersMarker marker in Object.FindObjectsByType<HeadquartersMarker>())
                if (marker.Building != null && marker.Building.Owner == Owner.Enemy)
                    _remaining.Add(marker.Building);

            _total = _remaining.Count;
            base.Subscribe();

            if (_remaining.Count == 0)
                Complete();
        }

        protected override void OnEnemyBuildingDestroyed(BuildingDeathEvent evt)
        {
            if (!_remaining.Remove(evt.Building))
                return;

            Report();
            if (_remaining.Count == 0)
                Complete();
        }
    }
}
