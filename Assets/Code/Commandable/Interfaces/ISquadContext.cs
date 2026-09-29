using Code.Units.Combat;
using UnityEngine;

namespace Code.Commandable.Interfaces
{
    /// <summary>
    /// 전투 유닛이 소속 분대에서 받아오는 값들입니다.
    /// </summary>
    public interface ISquadContext
    {
        /// <summary>
        /// 분대 스탯이 공급하는 공격력 배율입니다.
        /// </summary>
        float DamageMultiplier { get; }

        /// <summary>
        /// 분대 스탯이 공급하는 공격 주기 배율입니다.
        /// </summary>
        float AttackDelayMultiplier { get; }

        /// <summary>
        /// 지정한 방향에서 들어온 피해에 곱할 방향 배율입니다.
        /// 인자는 피격자에서 공격자를 향하는 단위 벡터입니다(방향이 없으면 Vector3.zero).
        /// </summary>
        float DirectionalMultiplierFrom(Vector3 attackerDirection);

        /// <summary>
        /// 소속 유닛의 사망을 분대에 알립니다.
        /// </summary>
        void NotifyMemberDied(AbstractCombatUnit member);

        /// <summary>
        /// 소속 유닛이 피해를 입었음을 분대에 알립니다.
        /// 인자는 피격자에서 공격자를 향하는 단위 벡터입니다(방향이 없으면 Vector3.zero).
        /// 분대원은 개별 자동 교전을 하지 않으므로(AbstractCombatUnit.CanAutoAttack),
        /// 사거리가 긴 적에게 맞을 때 이 통지가 없으면 분대는 영원히 반격하지 못합니다.
        /// </summary>
        void NotifyMemberDamaged(Vector3 attackerDirection);
    }
}
