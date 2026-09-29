using System.Collections.Generic;
using Code.Military.Data;
using Code.Military.Squads;
using Code.Military.Stats;
using UnityEngine;

namespace Code.Military.Commanders
{
    /// <summary>
    /// 지휘관 반경 안에 있는 배속 분대에 패시브를 겁니다. 반경을 벗어나면 뗍니다.
    /// 출처 키가 지휘관 인스턴스라 하드 복원 없이 정확히 원래 값으로 돌아갑니다.
    /// </summary>
    public sealed class CommanderAura
    {
        private const float TickInterval = 0.5f;

        private readonly Commander _commander;
        private readonly CommanderPassiveSO _passive;
        private readonly float _rangeSqr;
        private readonly List<Squad> _buffed = new(CommanderRoster.DefaultCapacity);

        private float _timer;

        public CommanderAura(Commander commander, CommanderPassiveSO passive, float range)
        {
            _commander = commander;
            _passive = passive;
            _rangeSqr = range * range;
        }

        public bool IsBuffed(Squad squad)
            => _buffed.Contains(squad);

        public void Tick(float deltaTime, IReadOnlyList<Squad> squads, Vector3 origin)
        {
            if (_passive == null || _passive.Stat == null)
                return;

            _timer -= deltaTime;
            if (_timer > 0f)
                return;

            _timer = TickInterval;

            for (int i = 0; i < squads.Count; i++)
            {
                Squad squad = squads[i];
                bool inRange = (squad.transform.position - origin).sqrMagnitude <= _rangeSqr;
                bool buffed = _buffed.Contains(squad);

                if (inRange && !buffed)
                    Apply(squad);
                else if (!inRange && buffed)
                    Remove(squad);
            }
        }

        /// <summary>
        /// 명부에서 빠진 분대의 보정을 뗍니다. 죽은 분대는 시트가 같이 사라져 목록에서만 지웁니다.
        /// </summary>
        public void Remove(Squad squad)
        {
            if (!_buffed.Remove(squad))
                return;

            if (squad != null && squad.Stats != null)
                squad.Stats.RemoveBySource(_commander);
        }

        public void Clear()
        {
            for (int i = _buffed.Count - 1; i >= 0; i--)
                Remove(_buffed[i]);
        }

        private void Apply(Squad squad)
        {
            StatSheet stats = squad.Stats;
            if (stats == null)
                return;

            StatSO stat = stats.Get(_passive.Stat);
            if (stat == null)
                return;

            stat.AddModifier(new StatModifier(_passive.Operator, _passive.Value, _commander));
            _buffed.Add(squad);
        }
    }
}
