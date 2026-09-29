using System.Collections.Generic;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Unit;
using Code.Units.Combat;
using Code.Units.Pawns;
using UnityEngine;


namespace Code.Players
{
    /// <summary>
    /// 플레이어 소속 유닛을 추적하는 싱글톤 레지스트리입니다.
    /// </summary>
    public class UnitRegistry : MonoSingleton<UnitRegistry>
    {
        [SerializeField] private Owner owner = Owner.Player;

        private readonly HashSet<Pawn> _pawns = new();
        private readonly HashSet<AbstractCombatUnit> _combatUnits = new();

        /// <summary>
        /// 현재 살아있는 소속 Pawn 목록 (읽기 전용).
        /// </summary>
        public IReadOnlyCollection<Pawn> Pawns => _pawns;

        /// <summary>
        /// 현재 살아있는 소속 전투 유닛 목록 (읽기 전용). 전투 루틴 수동 배정 대상 조회에 사용합니다.
        /// </summary>
        public IReadOnlyCollection<AbstractCombatUnit> CombatUnits => _combatUnits;

        protected override void Awake()
        {
            base.Awake();
            Bus<UnitSpawnEvent>.OnEvent[owner] += HandleUnitSpawn;
            Bus<UnitDeathEvent>.OnEvent[owner] += HandleUnitDeath;
        }

        protected override void OnDestroy()
        {
            Bus<UnitSpawnEvent>.OnEvent[owner] -= HandleUnitSpawn;
            Bus<UnitDeathEvent>.OnEvent[owner] -= HandleUnitDeath;
            base.OnDestroy();
        }

        private void HandleUnitSpawn(UnitSpawnEvent evt)
        {
            if (evt.Unit is Pawn pawn)
                _pawns.Add(pawn);
            else if (evt.Unit is AbstractCombatUnit combatUnit)
                _combatUnits.Add(combatUnit);
        }

        private void HandleUnitDeath(UnitDeathEvent evt)
        {
            if (evt.Unit is Pawn pawn)
                _pawns.Remove(pawn);
            else if (evt.Unit is AbstractCombatUnit combatUnit)
                _combatUnits.Remove(combatUnit);
        }
    }
}
