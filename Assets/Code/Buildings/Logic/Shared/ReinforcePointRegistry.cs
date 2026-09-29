using System.Collections.Generic;
using Code.Buildings.Interfaces;
using Code.Core.Bus;
using UnityEngine;

namespace Code.Buildings.Logic.Shared
{
    /// <summary>
    /// 생존 중인 모든 보충 거점의 static 목록입니다.
    /// </summary>
    // OutpostRegistry와 나눠 둔 이유: 그쪽은 전초 간 최소 건설 거리도 함께 본다
    public static class ReinforcePointRegistry
    {
        /// <summary>
        /// 현재 생존 중인 전체 보충 거점 목록입니다.
        /// </summary>
        public static IReadOnlyList<IReinforcePoint> Points => Registered;

        private static readonly List<IReinforcePoint> Registered = new();

        /// <summary>
        /// 거점을 목록에 등록합니다.
        /// </summary>
        public static void Register(IReinforcePoint point)
        {
            if (point != null && !Registered.Contains(point))
                Registered.Add(point);
        }

        /// <summary>
        /// 거점을 목록에서 제거합니다.
        /// </summary>
        public static void Unregister(IReinforcePoint point)
            => Registered.Remove(point);

        /// <summary>
        /// 지정 위치를 보충 범위에 포함하는 같은 편 거점을 반환합니다. 없으면 null입니다.
        /// </summary>
        public static IReinforcePoint FindCovering(Owner owner, Vector3 position)
        {
            foreach (IReinforcePoint point in Registered)
            {
                if (!IsAlive(point))
                    continue;

                if (point.Owner == owner && point.Covers(position))
                    return point;
            }

            return null;
        }

        // 인터페이스 참조에는 Unity의 == 오버로드가 안 걸린다 — Object로 캐스팅해야 파괴를 본다
        private static bool IsAlive(IReinforcePoint point)
            => point is Object unityObject && unityObject != null;

        // 에디터 도메인 리로드 비활성화 환경 대비: 플레이 시작마다 목록 초기화
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
            => Registered.Clear();
    }
}
