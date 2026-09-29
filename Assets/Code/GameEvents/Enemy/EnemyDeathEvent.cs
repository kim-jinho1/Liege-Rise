using Code.Commandable.Interfaces;
using Code.Core.Bus;

namespace Code.GameEvents.Enemy
{
    public struct EnemyDeathEvent : IEvent
    {
        public Owner Owner { get; private set; }
        public IDamageable Unit { get; private set; }

        /// <summary>
        /// 사망 시점에 이미 출발(Dispatch)한 유닛이면 true, 대기 중이었으면 false.
        /// </summary>
        public bool WasDispatched { get; private set; }

        public EnemyDeathEvent(Owner owner, IDamageable unit, bool wasDispatched)
        {
            Owner = owner;
            Unit = unit;
            WasDispatched = wasDispatched;
        }
    }
}
