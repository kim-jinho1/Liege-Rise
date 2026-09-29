using Code.Units.Combat;

namespace Code.Military.Squads
{
    /// <summary>
    /// 분대에 붙는 표시가 안개를 존중하도록 가시성을 판정합니다.
    /// </summary>
    public static class SquadVisibility
    {
        /// <summary>
        /// 분대원 중 하나라도 플레이어 시야에 들어와 있는지입니다.
        /// 분대 루트의 IsVisible을 보면 안 됩니다 — 안개 시스템은 UnitSpawnEvent·BuildingSpawnEvent로만
        /// 대상을 등록하는데 분대는 SquadSpawnEvent로 태어나 한 번도 등록되지 않고,
        /// 그래서 루트는 기본값인 "늘 보임"에 굳어 있습니다.
        /// </summary>
        public static bool IsVisibleToPlayer(Squad squad)
        {
            if (squad == null)
                return false;

            foreach (AbstractCombatUnit member in squad.Members)
                if (member != null && member.IsVisible)
                    return true;

            return false;
        }
    }
}
