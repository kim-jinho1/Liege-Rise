using Code.Core.Bus;
using Code.Military.Squads;
using UnityEngine;

namespace Code.Military.Commanders
{
    /// <summary>
    /// 분대가 자리로 가는 직선이 적을 지나가면 옆으로 돌아갈 지점을 잡습니다.
    /// 적 뒤에 그은 선은 적 앞을 지나며 붙습니다 — 옆으로 돌아 뒤로 갑니다.
    /// </summary>
    public static class DetourPlanner
    {
        private const float SampleStep = 2f;
        private const float EndMargin = 2f;    // 출발점·자리 바로 옆의 적은 어차피 붙을 상대다
        private const float ScanRadius = 6f;
        private const float SideDistance = 8f; // 적 감지 반경(7)보다 살짝 멀리 돈다

        // 우회점이 앉을 구간. 길의 앞뒤 끝에 붙으면 그 자리에서 급하게 꺾인다
        private const float MinDetourAlong = 0.35f;
        private const float MaxDetourAlong = 0.65f;

        /// <summary>
        /// 직선 위의 첫 적이 선 앞쪽에 있으면(선이 그 적의 뒤·옆을 친다) 우회점을 주고 true입니다.
        /// 적이 선 뒤에 있으면 정면으로 미는 선이라 그냥 걸어가며 붙습니다.
        /// </summary>
        public static bool TryPlan(Vector2 from, Vector2 seat, Vector2 lineNormal, Owner owner,
            BattleLineProbe probe, out Vector2 detour)
        {
            detour = default;

            Vector2 delta = seat - from;
            float length = delta.magnitude;
            if (length <= EndMargin * 2f)
                return false;

            Vector2 axis = delta / length;

            for (float distance = EndMargin; distance <= length - EndMargin; distance += SampleStep)
            {
                Squad enemy = probe.FindNearestEnemySquad(from + axis * distance, ScanRadius, owner);
                if (enemy == null)
                    continue;

                Vector2 enemyPosition = enemy.transform.position;
                if (Vector2.Dot(enemyPosition - seat, lineNormal) <= 0f)
                    return false;

                // 적이 축의 왼쪽이면 오른쪽으로, 오른쪽이면 왼쪽으로 돈다
                Vector2 rel = enemyPosition - from;
                float cross = axis.x * rel.y - axis.y * rel.x;
                Vector2 left = new(-axis.y, axis.x);
                Vector2 side = cross > 0f ? -left : left;

                // 우회점을 적 옆에 그대로 두면 갈 때와 돌아올 때 두 번 직각으로 꺾인다.
                // 길 한가운데쯤으로 당겨 두 번의 완만한 꺾임으로 만든다 — 옆으로 빼는 거리는 그대로다
                float along = Mathf.Clamp(Vector2.Dot(enemyPosition - from, axis),
                    length * MinDetourAlong, length * MaxDetourAlong);

                detour = LineTerrain.Snap(from + axis * along + side * SideDistance);
                return true;
            }

            return false;
        }
    }
}
