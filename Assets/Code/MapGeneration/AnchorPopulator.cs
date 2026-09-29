using System.Collections.Generic;
using Code.Navigation.Baking;
using Code.Navigation.Graph;
using UnityEngine;

namespace Code.MapGeneration
{
    /// <summary>
    /// 앵커 하나의 범위를 후보로 삼아 실제 Instantiate까지 수행하는 배치 워커입니다.
    /// 후보 수집·지형 검증·격자 정렬을 여기서 맡고, 무엇을 몇 개 놓을지는 호출자가 정합니다.
    /// </summary>
    public class AnchorPopulator
    {
        /// <summary>
        /// 현재 앵커에서 쓸 수 있는 후보 수입니다.
        /// </summary>
        public int CandidateCount => _candidates.Count;

        /// <summary>
        /// 현재 앵커에서 뭉침 반경을 풀고 놓은 횟수입니다. 무슨 말을 할지는 호출자가 정합니다.
        /// </summary>
        public int RelaxedCount { get; private set; }

        private readonly List<Vector3> _candidates = new(512);
        private readonly PathBaker _baker;
        private readonly PlacementSampler _sampler;
        private readonly int _maxAttempts;

        public AnchorPopulator(PathBaker baker, PlacementSampler sampler, int maxAttempts)
        {
            _baker = baker;
            _sampler = sampler;
            _maxAttempts = Mathf.Max(1, maxAttempts);
        }

        /// <summary>
        /// 앵커 범위 안의 건설 가능 셀을 후보로 모아 섞습니다. 앵커를 바꿀 때마다 호출합니다.
        /// </summary>
        public void Collect(MapAnchor anchor, BakedDataSO bakedData)
        {
            _candidates.Clear();
            RelaxedCount = 0;

            foreach (NodeData node in bakedData.points)
            {
                if (!node.canBuildGeneral || !anchor.Contains(node.worldPosition))
                    continue;

                _candidates.Add(node.worldPosition);
            }

            _sampler.Shuffle(_candidates);
        }

        /// <summary>
        /// 지정한 좌표에 프리팹을 놓습니다.
        /// 그 자리가 건설 불가면 현재 후보 중 가장 가까운 유효 위치로 대체합니다.
        /// 본부 건물처럼 "작성자가 찍은 위치를 최대한 지켜야" 하는 것에 씁니다.
        /// </summary>
        public bool TryPlaceAt(Vector3 target, Vector2Int footprint, GameObject prefab, Transform parent)
        {
            Vector3 position = _baker.SnapToGrid(target, footprint);

            if (!_baker.CanBuildFootprint(position, footprint)
                && !TryFindNearestBuildable(target, footprint, out position))
                return false;

            Object.Instantiate(prefab, position, Quaternion.identity, parent);
            _sampler.MarkPlaced(position);
            return true;
        }

        /// <summary>
        /// 항목 하나를 목표 개수만큼 후보에서 뽑아 배치하고 실제 배치 수를 반환합니다.
        /// 씨앗을 하나 놓고 그 주위를 무리 크기만큼 채우기를 반복하며, 무리 크기가 1이면 씨앗만 남아 흩어집니다.
        /// 판정과 생성 좌표가 어긋나지 않도록 스냅한 위치로 건설 가능 여부를 검사합니다.
        /// </summary>
        public int PlaceEntry(SpawnEntry entry, int target, Transform parent, ClusterBounds bounds)
        {
            Vector2Int footprint = entry.SafeFootprint;
            int clumpSize = Mathf.Max(1, entry.clumpSize);
            int placed = 0;

            while (placed < target)
            {
                if (!TryTakeSeed(entry.minSpacing, footprint, bounds, out Vector3 seed))
                    break;

                Place(entry.prefab, seed, footprint, parent);
                placed++;

                ClusterBounds clump = new(seed, entry.clumpRadius);
                for (int i = 1; i < clumpSize && placed < target; i++)
                {
                    // 반경 안이 차면 이 무리는 여기까지다. 풀지 않고 다음 씨앗으로 넘어간다
                    if (!TryTakeInClump(entry.clumpSpacing, footprint, bounds, clump, out Vector3 member))
                        break;

                    Place(entry.prefab, member, footprint, parent);
                    placed++;
                }
            }

            return placed;
        }

        /// <summary>
        /// 무리의 첫 자리를 뽑되, 앵커의 뭉침 반경 안이 찼으면 구역 전체로 풀어서 다시 시도합니다.
        /// </summary>
        private bool TryTakeSeed(float spacing, Vector2Int footprint, ClusterBounds bounds, out Vector3 result)
        {
            if (_sampler.TryTake(_candidates, spacing, _maxAttempts, Accepts(footprint, bounds, ClusterBounds.None),
                    out result))
                return true;

            // 반경 안이 찼다고 건물을 빼면 진지가 조용히 비므로, 구역 전체로 풀어 마저 놓는다
            if (!bounds.IsActive
                || !_sampler.TryTake(_candidates, spacing, _maxAttempts,
                    Accepts(footprint, ClusterBounds.None, ClusterBounds.None), out result))
                return false;

            RelaxedCount++;
            return true;
        }

        /// <summary>
        /// 씨앗 주위 반경 안에서 자리를 뽑습니다. 무리 안은 전역 최소 간격보다 좁아도 됩니다.
        /// </summary>
        private bool TryTakeInClump(float spacing, Vector2Int footprint, ClusterBounds bounds, ClusterBounds clump,
            out Vector3 result)
            => _sampler.TryTakeTight(_candidates, spacing, _maxAttempts, Accepts(footprint, bounds, clump), out result);

        /// <summary>
        /// 지형과 범위 조건을 만족하는 후보인지 판정하는 조건입니다.
        /// </summary>
        private System.Func<Vector3, bool> Accepts(Vector2Int footprint, ClusterBounds bounds, ClusterBounds clump)
            => candidate => bounds.Contains(candidate) && clump.Contains(candidate) &&
                            _baker.CanBuildFootprint(_baker.SnapToGrid(candidate, footprint), footprint);

        /// <summary>
        /// 후보 좌표를 격자에 맞춰 놓고 이후 간격 판정의 기준으로 기록합니다.
        /// </summary>
        private void Place(GameObject prefab, Vector3 candidate, Vector2Int footprint, Transform parent)
        {
            Vector3 position = _baker.SnapToGrid(candidate, footprint);
            Object.Instantiate(prefab, position, Quaternion.identity, parent);
            _sampler.MarkPlaced(position);
        }

        /// <summary>
        /// 후보 중 기준점에 가장 가깝고 풋프린트가 들어가는 위치를 찾습니다.
        /// </summary>
        private bool TryFindNearestBuildable(Vector3 target, Vector2Int footprint, out Vector3 result)
        {
            result = default;
            float best = float.MaxValue;

            foreach (Vector3 candidate in _candidates)
            {
                Vector3 snapped = _baker.SnapToGrid(candidate, footprint);
                if (!_baker.CanBuildFootprint(snapped, footprint))
                    continue;

                float distance = ((Vector2)snapped - (Vector2)target).sqrMagnitude;
                if (distance >= best)
                    continue;

                best = distance;
                result = snapped;
            }

            return best < float.MaxValue;
        }
    }
}
