using Code.Commandable;
using Code.Core.Bus;
using UnityEngine;

namespace Code.GameEvents.Combat
{
    /// <summary>
    /// 유닛이나 건물이 피해를 입었음을 알리는 이벤트입니다.
    /// AbstractCommandable.TakeDamage가 유닛·건물 공통의 유일한 길목이라 여기서만 발행됩니다.
    /// </summary>
    public struct DamageTakenEvent : IEvent
    {
        /// <summary>
        /// 피해를 입은 대상입니다.
        /// </summary>
        public AbstractCommandable Victim { get; private set; }

        /// <summary>
        /// 이번에 입은 피해량입니다.
        /// </summary>
        public int Damage { get; private set; }

        /// <summary>
        /// 피격자의 월드 좌표입니다.
        /// </summary>
        public Vector3 Position { get; private set; }

        public DamageTakenEvent(AbstractCommandable victim, int damage, Vector3 position)
        {
            Victim = victim;
            Damage = damage;
            Position = position;
        }
    }
}
