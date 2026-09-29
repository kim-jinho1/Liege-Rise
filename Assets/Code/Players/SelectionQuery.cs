using System.Collections.Generic;
using Code.Buildings.Interfaces;
using Code.Commandable;
using Code.Commandable.Interfaces;
using Code.Core;
using Code.Core.Bus;
using Code.FogSystem;
using Code.Navigation.Baking;
using Code.Units.Combat;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Players
{
    /// <summary>
    /// 화면의 한 점이나 한 구역에 무엇이 잡히는지 판정합니다.
    /// 입력·선택 상태는 SelectionController가 쥐고, 여기는 판정만 합니다.
    /// </summary>
    public sealed class SelectionQuery
    {
        // 정확히 맞은 것이 없을 때만 커서에서 이만큼 떨어진 유닛까지 훑는다.
        // 픽셀 기준이라 줌 배율이 달라져도 손끝 감각이 같다
        private const float PickTolerancePixels = 12f;

        private readonly Camera _camera;
        private readonly LayerMask _layerMask;
        private readonly Owner _controlOwner;
        private readonly List<RaycastHit2D> _hitResults = new(10);
        private readonly List<Collider2D> _nearResults = new(16);
        private ContactFilter2D _nearFilter;

        private Tilemap _tilemap;

        public SelectionQuery(Camera camera, LayerMask layerMask, Owner controlOwner)
        {
            _camera = camera;
            _layerMask = layerMask;
            _controlOwner = controlOwner;

            _nearFilter.SetLayerMask(layerMask);
            _nearFilter.useTriggers = true; // 광선 판정(queriesHitTriggers)과 기준을 맞춘다
        }

        /// <summary>
        /// 화면 좌표 아래에서 고를 대상 하나를 돌려줍니다. 유닛이 건물보다 앞섭니다.
        /// 소유자를 가리지 않습니다 — 적을 클릭해 이름·체력을 보는 경로가 여기를 지납니다.
        /// </summary>
        public ISelectable PreferredAt(Vector2 screenPoint)
        {
            if (_camera == null)
                return null;

            Ray ray = _camera.ScreenPointToRay(screenPoint);
            int hitCount = Physics2D.GetRayIntersection(ray, _camera.farClipPlane, _hitResults, _layerMask);

            ISelectable unit = null;
            ISelectable building = null;
            float unitDepth = float.MaxValue;
            float buildingDepth = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D collider = _hitResults[i].collider;
                ISelectable selectable = Resolve(collider);

                // 인터페이스에 is null을 쓰면 파괴된 분대 루트가 살아 있는 것으로 통과한다
                if (!UnityRef.IsAlive(selectable))
                    continue;

                // 겹친 것이 여럿이면 화면 앞쪽(Y가 작은 쪽)을 고른다. Y 정렬과 같은 기준이다
                float depth = collider.bounds.center.y;

                if (selectable is IBuilding)
                {
                    if (depth >= buildingDepth)
                        continue;

                    buildingDepth = depth;
                    building = selectable;
                    continue;
                }

                if (depth >= unitDepth)
                    continue;

                unitDepth = depth;
                unit = selectable;
            }

            // 건물 콜라이더는 유닛보다 훨씬 커서, 건물이 이기면 그 앞에 선 일꾼을 영영 못 집는다
            if (unit != null)
                return unit;

            if (building != null)
                return building;

            // 정확히 맞은 것이 없을 때만 관용 반경을 쓴다. 건물은 크고 멈춰 있어 빗맞을 일이 없으므로 유닛만 본다
            return NearestUnitWithin(_camera.ScreenToWorldPoint(screenPoint));
        }

        /// <summary>
        /// 커서 주변 관용 반경 안에서 가장 가까운 유닛을 돌려줍니다. 건물은 제외합니다.
        /// </summary>
        private ISelectable NearestUnitWithin(Vector2 cursorWorld)
        {
            float radius = ToleranceRadius();
            if (radius <= 0f)
                return null;

            int count = Physics2D.OverlapCircle(cursorWorld, radius, _nearFilter, _nearResults);

            ISelectable best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider2D collider = _nearResults[i];
                ISelectable selectable = Resolve(collider);

                if (!UnityRef.IsAlive(selectable) || selectable is IBuilding)
                    continue;

                float distance = (collider.ClosestPoint(cursorWorld) - cursorWorld).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = selectable;
            }

            return best;
        }

        /// <summary>
        /// 화면 픽셀로 정한 관용 폭을 지금 줌 배율의 월드 반경으로 옮깁니다.
        /// </summary>
        private float ToleranceRadius()
        {
            if (!_camera.orthographic || Screen.height <= 0)
                return 0f;

            return PickTolerancePixels * (_camera.orthographicSize * 2f / Screen.height);
        }

        /// <summary>
        /// 구역 안에서 드래그로 잡을 대상을 모읍니다.
        /// 건물과 남의 것은 빠집니다 — 드래그는 내 병력을 긁어모으는 조작입니다.
        /// </summary>
        public void CollectInBox(Vector3 worldMin, Vector3 worldMax, ContactFilter2D filter,
            List<Collider2D> buffer, HashSet<ISelectable> into)
        {
            Physics2D.OverlapArea(worldMin, worldMax, filter, buffer);

            foreach (Collider2D hit in buffer)
            {
                ISelectable selectable = Resolve(hit);

                // 인터페이스에는 == null도 is null도 Unity 오버로드를 타지 않는다. 둘 다 파괴된 것을 통과시킨다
                if (!UnityRef.IsAlive(selectable) || selectable is IBuilding)
                    continue;

                if (selectable is AbstractCommandable commandable && commandable.Owner != _controlOwner)
                    continue;

                into.Add(selectable);
            }
        }

        /// <summary>
        /// 콜라이더 하나를 고를 수 있는 대상으로 옮깁니다.
        /// 안개 판정은 분대로 접기 전 원본으로 합니다 — 분대 프록시는 IHideable이 아닙니다.
        /// </summary>
        private static ISelectable Resolve(Collider2D hit)
        {
            if (!FogVisibility.IsVisibleNow(hit))
                return null;

            ISelectable selectable = hit.GetComponentInParent<ISelectable>();

            // 개별 전투 유닛은 선택 불가 — 클릭은 소속 분대로 넘긴다
            return selectable is AbstractCombatUnit combat ? combat.SelectionProxy : selectable;
        }

        /// <summary>
        /// 두 월드 좌표가 이루는 영역을 타일맵 셀 경계로 스냅합니다.
        /// </summary>
        public void SnapToCells(Vector3 a, Vector3 b, out Vector3 worldMin, out Vector3 worldMax)
        {
            worldMin = new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), 0f);
            worldMax = new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), 0f);

            Tilemap map = ResolveTilemap();
            if (map == null)
                return;

            Vector3Int minCell = map.WorldToCell(worldMin);
            Vector3Int maxCell = map.WorldToCell(worldMax);

            // CellToWorld는 셀의 좌하단 모서리를 반환하므로, 우상단은 maxCell의 다음 칸 모서리를 사용합니다.
            worldMin = map.CellToWorld(minCell);
            worldMax = map.CellToWorld(maxCell + new Vector3Int(1, 1, 0));
            worldMin.z = 0f;
            worldMax.z = 0f;
        }

        /// <summary>
        /// PathBaker가 보유한 베이스 타일맵을 지연 캐싱하여 반환합니다.
        /// </summary>
        private Tilemap ResolveTilemap()
        {
            if (_tilemap == null && PathBaker.Instance != null)
                _tilemap = PathBaker.Instance.BaseTilemap;

            return _tilemap;
        }
    }
}
