using System;
using System.Collections.Generic;
using Code.Buildings.Data;
using Code.Commandable;
using Code.Core.Bus;
using Code.GameEvents.Building;
using Code.GameEvents.Unit;
using UnityEngine;

namespace Code.Core
{
    /// <summary>
    /// Owner별로 UnlockableSO 데이터를 복제·관리하며,
    /// 유닛/건물 스폰 시 복제본을 주입합니다.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class DataManager : MonoBehaviour
    {
        [SerializeField] private List<UnlockableSO> allUnlockables;

        private readonly Dictionary<Owner, Dictionary<int, UnlockableSO>> _dataDict = new();

        private BusSubscriptionScope _scope;

        private void Awake()
        {
            foreach (Owner owner in Enum.GetValues(typeof(Owner)))
                _dataDict.Add(owner, BuildOwnerData());

            _scope = new BusSubscriptionScope();
            _scope.SubscribeAll<UnitSpawnEvent>(HandleUnitSpawnEvent);
            _scope.SubscribeAll<BuildingSpawnEvent>(HandleBuildingSpawnEvent);
        }

        /// <summary>
        /// Owner별로 UnlockableSO를 복제하여 Dictionary를 구성합니다.
        /// </summary>
        private Dictionary<int, UnlockableSO> BuildOwnerData()
        {
            Dictionary<int, UnlockableSO> data = new(allUnlockables.Count);

            foreach (UnlockableSO source in allUnlockables)
            {
                if (source == null)
                    continue;

                int code = source.GetHashCode();
                if (data.ContainsKey(code))
                    continue;

                data.Add(code, source.Clone() as UnlockableSO);
            }

            return data;
        }

        private void OnDestroy()
        {
            _scope?.Dispose(); // Awake가 도중에 실패했으면 _scope는 아직 null이다
        }

        /// <summary>
        /// 유닛 스폰 시 Owner별 복제 SO를 주입합니다.
        /// </summary>
        private void HandleUnitSpawnEvent(UnitSpawnEvent evt)
        {
            int code = evt.Unit.AbstractCommandableSO.GetHashCode();
            if (_dataDict[evt.Owner].TryGetValue(code, out UnlockableSO unlockable))
                evt.Unit.AbstractCommandableSO = unlockable as AbstractCommandableSO;
        }

        /// <summary>
        /// 건물 스폰 시 Owner별 복제 SO를 주입합니다.
        /// </summary>
        private void HandleBuildingSpawnEvent(BuildingSpawnEvent evt)
        {
            int code = evt.Building.AbstractCommandableSO.GetHashCode();
            if (!_dataDict[evt.Owner].TryGetValue(code, out UnlockableSO unlockable))
                return;

            evt.Building.AbstractCommandableSO = unlockable as AbstractCommandableSO;
            if (unlockable is BuildingSO buildingSO)
                evt.Building.BuildingSO = buildingSO;
        }
    }
}
