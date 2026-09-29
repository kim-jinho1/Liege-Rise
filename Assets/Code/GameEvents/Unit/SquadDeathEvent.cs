using Code.Core.Bus;
using Code.Military.Squads;

namespace Code.GameEvents.Unit
{
    /// <summary>
    /// 분대가 사망할 때 발생하는 이벤트입니다.
    /// 분대 소유자와 사망한 분대 정보를 포함합니다.
    /// </summary>
    public struct SquadDeathEvent : IEvent
    {
        public Owner Owner { get; private set; }
        public ISquad Squad { get; private set; }

        public SquadDeathEvent(Owner owner, ISquad squad)
        {
            Owner = owner;
            Squad = squad;
        }
    }
}
