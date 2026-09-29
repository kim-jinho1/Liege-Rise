using System;
using System.Collections.Generic;
using Code.Core.Bus;
using Code.GameEvents.Unit;
using Code.Military.Squads;

namespace Code.Military.Commanders
{
    /// <summary>
    /// 지휘관이 거느리는 분대 명부입니다. 배속·해제와 죽은 분대 정리를 맡습니다.
    /// 분대 쪽 참조(Squad.Commander)는 여기서만 바꿉니다.
    /// </summary>
    public sealed class CommanderRoster
    {
        public const int DefaultCapacity = 6;

        public int Count => _squads.Count;
        public int Capacity { get; }
        public IReadOnlyList<Squad> Squads => _squads;

        /// <summary>분대가 명부에서 빠질 때입니다. 버프처럼 분대에 걸어 둔 것을 되돌리는 통로입니다.</summary>
        public event Action<Squad> SquadReleased;

        private readonly List<Squad> _squads;
        private readonly Commander _owner;

        public CommanderRoster(Commander owner, int capacity = DefaultCapacity)
        {
            _owner = owner;
            Capacity = capacity;
            _squads = new List<Squad>(capacity);
        }

        public bool CanAccept(Squad squad)
            => squad != null && !squad.IsDead && squad.Owner == _owner.Owner
               && squad.Commander != _owner && _squads.Count < Capacity;

        /// <summary>
        /// 분대를 배속합니다. 다른 지휘관 밑에 있던 분대는 그쪽에서 먼저 빠집니다.
        /// </summary>
        public bool Assign(Squad squad)
        {
            if (!CanAccept(squad))
                return false;

            if (squad.Commander != null)
                squad.Commander.Roster.Release(squad);

            _squads.Add(squad);
            squad.SetCommander(_owner);
            Bus<CommanderOrderEvent>.Raise(_owner.Owner,
                new CommanderOrderEvent(_owner.Owner, _owner, CommanderOrderKind.Assigned));
            return true;
        }

        public void Release(Squad squad)
        {
            if (!_squads.Remove(squad))
                return;

            SquadReleased?.Invoke(squad);
            if (squad != null)
                squad.SetCommander(null);
        }

        /// <summary>
        /// 지휘관이 죽을 때입니다. 분대는 각자 그 자리에 남고 선만 사라집니다.
        /// </summary>
        public void ReleaseAll()
        {
            for (int i = _squads.Count - 1; i >= 0; i--)
                Release(_squads[i]);
        }

        /// <summary>
        /// 전멸한 분대를 명부에서 뺍니다. 이벤트 구독 대신 틱마다 훑습니다 — 여섯을 넘지 않아 싸고,
        /// 구독은 분대마다 델리게이트를 남겨 해제를 잊기 쉽습니다.
        /// </summary>
        public void Prune()
        {
            for (int i = _squads.Count - 1; i >= 0; i--)
            {
                // AliveCount는 보지 않는다 — 방금 생성돼 Start 전인 분대는 분대원이 아직 0이다
                Squad squad = _squads[i];
                if (squad == null || squad.IsDead)
                {
                    _squads.RemoveAt(i);
                    SquadReleased?.Invoke(squad);
                }
            }
        }
    }
}
