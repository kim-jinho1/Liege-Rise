using System.Collections.Generic;
using Code.Core.Bus;
using Code.Players;
using Code.Units.BT;
using Unity.Behavior;
using UnityEngine;

namespace Code.Units.Pawns
{
    /// <summary>
    /// 폰을 소모하는 건물(징집·분대 생산)이 공유하는 선발 유틸리티입니다.
    /// 유휴 폰을 우선 선발하고, 없으면 작업 중 폰을 이탈시켜 소모합니다.
    /// 건물로 이동 중인 폰의 전역 예약 목록을 관리해 여러 건물이 같은 폰을
    /// 중복 선발하지 않도록 합니다.
    /// </summary>
    public static class PawnConscription
    {
        private static readonly HashSet<Pawn> Reserved = new();

        /// <summary>
        /// 소모 가능한 폰(유휴 또는 비상주 작업 폰)이 있는지 확인합니다.
        /// </summary>
        public static bool HasConsumable(Owner owner)
            => FindConsumable(owner, Vector3.zero) != null;

        /// <summary>
        /// 선발한 폰을 예약 목록에 등록합니다. 소모 또는 이탈 시 반드시 Release로 해제해야 합니다.
        /// </summary>
        public static void Reserve(Pawn pawn)
            => Reserved.Add(pawn);

        /// <summary>
        /// 폰의 예약을 해제합니다.
        /// </summary>
        public static void Release(Pawn pawn)
            => Reserved.Remove(pawn);

        /// <summary>
        /// 소모 대상 폰을 찾습니다. 유휴 폰 중 가장 가까운 폰이 우선이고,
        /// 유휴 폰이 없으면 상주·예약 이동 중이 아닌 작업 폰 중 가장 가까운 폰을 반환합니다.
        /// </summary>
        public static Pawn FindConsumable(Owner owner, Vector3 near)
        {
            // 죽었거나 이탈한 폰이 예약 목록에 남아 선발을 막지 않도록 정리
            Reserved.RemoveWhere(p => p == null || !IsEnRouteToStation(p));

            UnitRegistry registry = UnitRegistry.Instance;
            if (registry == null)
                return null;

            Pawn bestIdle = null;
            Pawn bestWorking = null;
            float idleSqrDist = float.MaxValue;
            float workingSqrDist = float.MaxValue;

            foreach (Pawn pawn in registry.Pawns)
            {
                if (pawn == null || pawn.Owner != owner ||
                    pawn.StationedBuilding != null || Reserved.Contains(pawn))
                    continue;

                float sqrDist = (pawn.transform.position - near).sqrMagnitude;
                if (pawn.IsAvailable)
                {
                    if (!(sqrDist < idleSqrDist))
                        continue;

                    idleSqrDist = sqrDist;
                    bestIdle = pawn;
                }
                else if (sqrDist < workingSqrDist)
                {
                    workingSqrDist = sqrDist;
                    bestWorking = pawn;
                }
            }

            return bestIdle != null ? bestIdle : bestWorking;
        }

        /// <summary>
        /// 폰이 상주(건물로의 이동 포함) 명령을 수행 중인지 확인합니다.
        /// </summary>
        public static bool IsEnRouteToStation(Pawn pawn)
            => pawn.GetVariable(BTVariables.Command, out BlackboardVariable<UnitCommands> cmd) &&
               cmd.Value == UnitCommands.StationedAtBuilding;
    }
}
