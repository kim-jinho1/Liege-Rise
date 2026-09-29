using System.Collections.Generic;
using Code.Buildings.Logic.DefenseBuilding;
using Code.Commandable.Interfaces;
using Code.Units.Combat;
using Code.Units.Util;
using UnityEngine;

namespace Code.Military.Squads
{
    /// <summary>
    /// 명부의 살아 있는 분대원 전원에게 같은 명령을 뿌리거나 같은 것을 묻습니다.
    /// 명부를 쥔 SquadRoster가 창구이고, 여기에는 상태가 없습니다.
    /// </summary>
    public static class SquadOrders
    {
        /// <summary>
        /// 분대원들을 멈춥니다. 죽은 분대원은 무시됩니다.
        /// </summary>
        public static void HaltAll(IReadOnlyList<AbstractCombatUnit> members)
        {
            foreach (AbstractCombatUnit member in members)
                if (!member.IsDead)
                    member.Stop();
        }

        /// <summary>
        /// 분대원들에게 지정한 대상에 대한 공격을 할당합니다. 죽은 분대원은 무시됩니다.
        /// </summary>
        public static void AssignAttack(IReadOnlyList<AbstractCombatUnit> members, IDamageable target)
        {
            foreach (AbstractCombatUnit member in members)
                if (!member.IsDead)
                    member.Attack(target);
        }

        /// <summary>
        /// 분대원 전원에게 탑승 명령을 내립니다. 슬롯은 각자 빈 자리를 잡으므로,
        /// 슬롯보다 인원이 많으면 남은 인원은 타워 밑에 그대로 섭니다.
        /// </summary>
        public static void AssignMount(IReadOnlyList<AbstractCombatUnit> members, MannedTowerBuilding tower)
        {
            foreach (AbstractCombatUnit member in members)
                if (!member.IsDead)
                    member.OrderMountTower(tower);
        }

        /// <summary>
        /// 분대원들이 모두 지정한 위치에 도착했는지 확인합니다. 죽은 분대원은 무시됩니다.
        /// </summary>
        public static bool AllArrived(IReadOnlyList<AbstractCombatUnit> members)
        {
            foreach (AbstractCombatUnit member in members)
            {
                if (member.IsDead)
                    continue;

                if (!MovementHelper.HasArrived(member.Agent))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 분대원 중 한 명이라도 지정한 범위 안에 있는지 확인합니다.
        /// </summary>
        public static bool AnyInRange(IReadOnlyList<AbstractCombatUnit> members, GameObject targetGo, float range)
        {
            foreach (AbstractCombatUnit member in members)
                if (!member.IsDead && InRangeOf(member, targetGo, range))
                    return true;

            return false;
        }

        /// <summary>
        /// 분대원 하나가 대상의 표면까지 지정 거리 안인지입니다.
        /// 분대 판정과 멤버 판정이 이 하나를 공유해야 갈라지지 않습니다.
        /// </summary>
        public static bool InRangeOf(AbstractCombatUnit member, GameObject targetGo, float range)
        {
            Vector3 closest = DistanceUtil.GetClosestTargetPosition(member.gameObject, targetGo);
            return (closest - member.transform.position).sqrMagnitude <= range * range;
        }
    }
}
