using System.Collections.Generic;
using Code.Buildings.Base;
using Code.Buildings.Data;
using Code.Buildings.Interfaces;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Building;
using Code.GameEvents.Economy;
using Code.GameEvents.Unit;
using Code.Military.Commanders;
using Code.Units.Pawns;
using UnityEngine;

namespace Code.Players
{
    /// <summary>
    /// 인구를 집계합니다. 폰 축과 분대 축이 분리돼 있습니다 — 폰은 채집·건설 전용이고 분대의 재료가 아닙니다.
    /// 폰 상한은 본부·집(IPawnCapacityProvider)이, 분대 상한은 본부 기본치 + 지휘관 한 명당 배속 상한이 공급합니다.
    /// 지휘관이 죽으면 상한이 줄지만 넘친 분대는 죽지 않고 신규 생산만 막힙니다.
    /// </summary>
    public class UserPopulation : MonoSingleton<UserPopulation>
    {
        private readonly Dictionary<Owner, int> _pawnCount = new();
        private readonly Dictionary<Owner, int> _pawnCapacity = new();
        private readonly Dictionary<Owner, int> _squadCount = new();
        private readonly Dictionary<Owner, int> _squadCapacity = new();

        private BusSubscriptionScope _scope;

        protected override void Awake()
        {
            base.Awake();
            _scope = new BusSubscriptionScope();
            _scope.SubscribeAll<UnitSpawnEvent>(HandleUnitSpawn);
            _scope.SubscribeAll<UnitDeathEvent>(HandleUnitDeath);
            _scope.SubscribeAll<SquadSpawnEvent>(HandleSquadSpawn);
            _scope.SubscribeAll<SquadDeathEvent>(HandleSquadDeath);
            _scope.SubscribeAll<BuildingSpawnEvent>(HandleBuildingSpawn);
            _scope.SubscribeAll<BuildingDeathEvent>(HandleBuildingDeath);
        }

        protected override void OnDestroy()
        {
            _scope?.Dispose();
            base.OnDestroy();
        }

        public int GetPawnCount(Owner owner)
            => _pawnCount.GetValueOrDefault(owner, 0);

        public int GetPawnCapacity(Owner owner)
            => _pawnCapacity.GetValueOrDefault(owner, 0);

        public int GetSquadCount(Owner owner)
            => _squadCount.GetValueOrDefault(owner, 0);

        public int GetSquadCapacity(Owner owner)
            => _squadCapacity.GetValueOrDefault(owner, 0);

        public bool CanAddSquad(Owner owner)
            => GetSquadCount(owner) < GetSquadCapacity(owner);

        public bool CanAddPawn(Owner owner)
            => GetPawnCount(owner) < GetPawnCapacity(owner);

        private void HandleUnitSpawn(UnitSpawnEvent evt)
        {
            if (evt.Unit is Pawn)
                AddPawn(evt.Owner, 1);
            else if (evt.Unit is Commander commander)
                AddSquadCapacity(evt.Owner, CapacityOf(commander));
        }

        private void HandleUnitDeath(UnitDeathEvent evt)
        {
            // UnitDeathEvent에는 Owner가 없어 유닛에서 직접 읽는다
            if (evt.Unit is Pawn pawn)
                AddPawn(pawn.Owner, -1);
            else if (evt.Unit is Commander commander)
                AddSquadCapacity(commander.Owner, -CapacityOf(commander));
        }

        private static int CapacityOf(Commander commander)
            => commander.CommanderSO != null ? commander.CommanderSO.SquadCapacity : CommanderRoster.DefaultCapacity;

        private void HandleSquadSpawn(SquadSpawnEvent evt)
            => AddSquad(evt.Owner, 1);

        private void HandleSquadDeath(SquadDeathEvent evt)
            => AddSquad(evt.Owner, -1);

        private void HandleBuildingSpawn(BuildingSpawnEvent evt)
            => ApplyBuilding(evt.Owner, evt.Building, 1);

        private void HandleBuildingDeath(BuildingDeathEvent evt)
            => ApplyBuilding(evt.Owner, evt.Building, -1);

        private void ApplyBuilding(Owner owner, AbstractBuilding building, int sign)
        {
            if (building == null)
                return;

            bool changed = false;

            if (building.BuildingSO is HeadquartersBuildingSO headquarters)
            {
                _squadCapacity[owner] = Mathf.Max(0,
                    _squadCapacity.GetValueOrDefault(owner, 0) + headquarters.SquadCapacity * sign);
                changed = true;
            }

            if (building.BuildingSO is IPawnCapacityProvider provider)
            {
                _pawnCapacity[owner] = Mathf.Max(0,
                    _pawnCapacity.GetValueOrDefault(owner, 0) + provider.PawnCapacity * sign);
                changed = true;
            }

            if (changed)
                Raise(owner);
        }

        private void AddSquadCapacity(Owner owner, int delta)
        {
            _squadCapacity[owner] = Mathf.Max(0, _squadCapacity.GetValueOrDefault(owner, 0) + delta);
            Raise(owner);
        }

        private void AddPawn(Owner owner, int delta)
        {
            int previous = GetPawnCount(owner);
            _pawnCount[owner] = Mathf.Max(0, previous + delta);

            // 클램프 후 실제 변화량을 넘긴다 — 0에서 더 깎이면 요청량과 다르다.
            Raise(owner, _pawnCount[owner] - previous);
        }

        private void AddSquad(Owner owner, int delta)
        {
            int previous = GetSquadCount(owner);
            _squadCount[owner] = Mathf.Max(0, previous + delta);
            Raise(owner, 0, _squadCount[owner] - previous);
        }

        private void Raise(Owner owner, int pawnDelta = 0, int squadDelta = 0)
            => Bus<PopulationChangedEvent>.Raise(owner, new PopulationChangedEvent(
                owner, GetPawnCount(owner), GetPawnCapacity(owner),
                GetSquadCount(owner), GetSquadCapacity(owner), pawnDelta, squadDelta));
    }
}
