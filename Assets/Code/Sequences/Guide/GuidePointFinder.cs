using Code.Buildings.Base;
using Code.Buildings.Logic.CoreBuilding;
using Code.Core.Bus;
using Code.Military.Commanders;
using Code.Stages;
using UnityEngine;

namespace Code.Sequences.Guide
{
    /// <summary>
    /// 강조할 오브젝트가 아니라 좌표만 내는 힌트를 풉니다. 찍어 둔 자리를 먼저 보고, 매복만 계산 경로가 있습니다.
    /// </summary>
    public static class GuidePointFinder
    {
        // 적이 실제로 성에 닿는 자리 — 길 끝에서 이만큼 앞. 여기서 정면 대결이 벌어진다
        private const float AmbushStandoff = 10f;
        // 그 자리에서 길 옆으로 이만큼. 감지 반경(7~8)보다 넉넉히 크되,
        // 대결이 시작되면 한달음에 옆구리에 닿을 만큼 가까워야 한다
        private const float AmbushLateral = 13f;

        /// <summary>
        /// 강조 대상 없이 좌표만 내는 종류인지입니다. 전부 StageSO.GuidePoints에 찍어 둘 수 있습니다.
        /// </summary>
        public static bool IsAuthoredPoint(GameObjectHighlightType type)
            => type is GameObjectHighlightType.AmbushSpot
                or GameObjectHighlightType.FarmSpot
                or GameObjectHighlightType.CottageSpot
                or GameObjectHighlightType.BarracksSpot
                or GameObjectHighlightType.DefendSpot
                or GameObjectHighlightType.MoveSpot;

        /// <summary>
        /// 찍어 둔 자리를 먼저 보고, 없으면 종류별로 계산합니다.
        /// 매복만 계산 경로가 있고 나머지는 찍어 두지 않으면 짚지 않습니다.
        /// </summary>
        public static bool TryGetAuthoredPoint(GameObjectHighlightType type, out Vector3 position)
        {
            position = default;

            StageSO stage = StageDatabaseSO.Current;
            if (stage != null && stage.TryGetGuidePoint(type, out Vector2 authored))
            {
                position = authored;
                return true;
            }

            return type == GameObjectHighlightType.AmbushSpot && TryGetAmbushSpot(out position);
        }

        /// <summary>
        /// 적이 성에 닿는 자리 바로 옆의 매복 지점입니다. 어느 쪽 본부든 없으면 false.
        /// 기준은 직선이 아니라 적이 실제로 걸어오는 길입니다 — 성↔소굴 직선에서 옆으로 밀면
        /// 조금 밀 때는 길에 붙어 지나가고, 많이 밀면 정면 대결이 벌어지는 동안 구석에서 놉니다.
        /// 길 끝에서 조금 앞(부딪히는 자리)을 먼저 찾고, 그 자리의 진행 방향에 수직으로 밉니다.
        /// </summary>
        public static bool TryGetAmbushSpot(out Vector3 position)
        {
            position = default;
            if (!TryGetHeadquarters(Owner.Player, out Vector3 ours) || !TryGetHeadquarters(Owner.Enemy, out Vector3 theirs))
                return false;

            if (!TryGetClashPoint(ours, theirs, out Vector2 clash, out Vector2 heading))
                return false;

            Vector2 side = new(-heading.y, heading.x);
            Vector2 wanted = clash + side * AmbushLateral;
            Vector2 other = clash - side * AmbushLateral;

            Vector2 snapped = LineTerrain.Snap(wanted);
            Vector2 snappedOther = LineTerrain.Snap(other);

            // 통행 칸으로 덜 끌려간 쪽이 실제로 설 수 있는 쪽이다. 한쪽이 물·절벽이면 반대쪽에 숨는다
            position = (snapped - wanted).sqrMagnitude <= (snappedOther - other).sqrMagnitude
                ? snapped
                : snappedOther;

            return true;
        }

        public static bool TryGetPlayerCastle(out Vector3 position)
            => TryGetHeadquarters(Owner.Player, out position);

        /// <summary>
        /// 적이 성에 닿기 직전의 지점과 그 자리의 진행 방향입니다.
        /// 길을 못 찾으면 직선으로 대신합니다 — 베이크 데이터가 없는 씬에서도 자리는 나와야 합니다.
        /// </summary>
        private static bool TryGetClashPoint(Vector3 ours, Vector3 theirs, out Vector2 clash, out Vector2 heading)
        {
            if (LineTerrain.TryStepBeforeEnd(theirs, ours, AmbushStandoff, out clash, out heading))
                return true;

            Vector2 axis = (Vector2)(ours - theirs);
            if (axis.sqrMagnitude < 0.001f)
                return false;

            heading = axis.normalized;
            clash = (Vector2)ours - heading * AmbushStandoff;
            return true;
        }

        private static bool TryGetHeadquarters(Owner owner, out Vector3 position)
        {
            foreach (HeadquartersMarker marker in Object.FindObjectsByType<HeadquartersMarker>())
            {
                AbstractBuilding building = marker.Building;
                if (building == null || building.IsDead || building.Owner != owner)
                    continue;

                position = building.transform.position;
                return true;
            }

            position = default;
            return false;
        }
    }
}
