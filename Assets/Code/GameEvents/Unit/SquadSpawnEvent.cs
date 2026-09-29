using Code.Core.Bus;
using Code.Military.Squads;

namespace Code.GameEvents.Unit
{
    /// <summary>
    /// 분대가 스폰될 때 발생하는 이벤트입니다.
    /// 분대 소유자와 스폰된 분대 정보를 포함합니다.
    /// </summary>
    public struct SquadSpawnEvent : IEvent
    {
        public Owner Owner { get; private set; }
        public ISquad Squad { get; private set; }

        public SquadSpawnEvent(Owner owner, ISquad squad)
        {
            Owner = owner;
            Squad = squad;
        }
    }
}
