using System.Collections.Generic;

namespace Code.Military.Stats
{
    /// <summary>
    /// 소유자 하나가 쓰는 스탯 묶음입니다. 정의 에셋을 키로, 복제된 런타임 스탯을 값으로 갖습니다.
    /// </summary>
    public class StatSheet
    {
        private readonly Dictionary<StatSO, StatSO> _runtimeStats;

        public StatSheet(IReadOnlyList<StatOverride> overrides)
        {
            _runtimeStats = new Dictionary<StatSO, StatSO>(overrides.Count);
            foreach (StatOverride statOverride in overrides)
            {
                if (statOverride?.Definition == null)
                    continue;

                _runtimeStats[statOverride.Definition] = statOverride.CreateRuntimeStat();
            }
        }

        /// <summary>
        /// 정의 에셋으로 런타임 스탯을 찾습니다. 등록되지 않았으면 null입니다.
        /// </summary>
        public StatSO Get(StatSO definition)
            => definition != null && _runtimeStats.TryGetValue(definition, out StatSO stat) ? stat : null;

        /// <summary>
        /// 지정한 출처가 건 보정을 모든 스탯에서 제거합니다.
        /// </summary>
        public void RemoveBySource(object source)
        {
            foreach (StatSO stat in _runtimeStats.Values)
                stat.RemoveBySource(source);
        }
    }
}
