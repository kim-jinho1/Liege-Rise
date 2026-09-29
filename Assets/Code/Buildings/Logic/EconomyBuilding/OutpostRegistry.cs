using System.Collections.Generic;
using Code.Core.Bus;
using UnityEngine;

namespace Code.Buildings.Logic.EconomyBuilding
{
    /// <summary>
    /// 생존 중인 모든 전초의 static 목록입니다. 보충 커맨드의 범위 판정과
    /// 전초 간 최소 거리 건설 제한이 조회합니다.
    /// </summary>
    public static class OutpostRegistry
    {
        /// <summary>
        /// 현재 생존 중인 전체 전초 목록
        /// .</summary>
        public static IReadOnlyList<OutpostBuilding> OutpostBuildings => Outpost;

        private static readonly List<OutpostBuilding> Outpost = new();

        /// <summary>
        /// 전초를 목록에 등록합니다.
        /// </summary>
        public static void Register(OutpostBuilding outpost)
        {
            if (!Outpost.Contains(outpost))
                Outpost.Add(outpost);
        }

        /// <summary>
        /// 전초를 목록에서 제거합니다.
        /// </summary>
        public static void Unregister(OutpostBuilding outpost)
            => Outpost.Remove(outpost);

        /// <summary>
        /// 지정 위치를 보충 범위에 포함하는 아군 전초를 반환합니다. 없으면 null.
        /// </summary>
        public static OutpostBuilding FindCovering(Owner owner, Vector3 position)
        {
            foreach (OutpostBuilding outpost in Outpost)
                if (outpost != null && outpost.Owner == owner && outpost.Covers(position))
                    return outpost;

            return null;
        }

        // 에디터 도메인 리로드 비활성화 환경 대비: 플레이 시작마다 목록 초기화
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
            => Outpost.Clear();
    }
}
