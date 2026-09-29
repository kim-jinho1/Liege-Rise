using System.Collections.Generic;
using UnityEngine;

namespace Code.MapGeneration
{
    /// <summary>
    /// 후보 위치 목록에서 최소 간격 조건을 지키며 위치를 뽑아 주는 샘플러입니다.
    /// 시드를 받아 같은 시드면 같은 배치를 재현합니다.
    /// </summary>
    public class PlacementSampler
    {
        private readonly List<Vector3> _placed = new(128);
        private readonly System.Random _random;
        private readonly float _globalMinSpacing;

        public PlacementSampler(System.Random random, float globalMinSpacing)
        {
            _random = random;
            _globalMinSpacing = Mathf.Max(0f, globalMinSpacing);
        }

        /// <summary>
        /// 지금까지 배치된 위치 수입니다.
        /// </summary>
        public int PlacedCount
            => _placed.Count;

        /// <summary>
        /// 후보 목록을 제자리에서 섞습니다. 링별로 한 번만 호출하면 됩니다.
        /// </summary>
        public void Shuffle(List<Vector3> candidates)
        {
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }
        }

        /// <summary>
        /// 간격·지형 조건을 만족하는 후보를 찾아 반환하고, 채택한 후보만 목록에서 제거합니다.
        /// 조건에 걸린 후보는 남겨 둡니다 — minSpacing이 더 작은 다음 항목은 같은 자리를 쓸 수 있으므로
        /// 버리면 좁은 구역에서 뒤 항목이 굶습니다.
        /// maxAttempts만큼 훑어도 찾지 못하면 false를 반환합니다.
        /// </summary>
        public bool TryTake(List<Vector3> candidates, float minSpacing, int maxAttempts,
            System.Func<Vector3, bool> isValid, out Vector3 result)
            => TryTakeSpaced(candidates, Mathf.Max(minSpacing, _globalMinSpacing), maxAttempts, isValid, out result);

        /// <summary>
        /// 전역 최소 간격을 하한으로 두지 않고 주어진 간격 그대로 뽑습니다.
        /// 숲처럼 작성자가 일부러 붙이는 배치에만 씁니다. 간격이 0이면 전역값을 씁니다.
        /// </summary>
        public bool TryTakeTight(List<Vector3> candidates, float spacing, int maxAttempts,
            System.Func<Vector3, bool> isValid, out Vector3 result)
            => TryTakeSpaced(candidates, spacing > 0f ? spacing : _globalMinSpacing, maxAttempts, isValid, out result);

        private bool TryTakeSpaced(List<Vector3> candidates, float spacing, int maxAttempts,
            System.Func<Vector3, bool> isValid, out Vector3 result)
        {
            result = default;
            int attempts = Mathf.Min(Mathf.Max(1, maxAttempts), candidates.Count);

            for (int i = candidates.Count - 1, tried = 0; i >= 0 && tried < attempts; i--, tried++)
            {
                Vector3 candidate = candidates[i];

                if (!IsFarEnough(candidate, spacing))
                    continue;

                if (isValid != null && !isValid(candidate))
                    continue;

                candidates.RemoveAt(i);
                result = candidate;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 배치가 확정된 위치를 기록합니다. 이후 간격 검사의 기준이 됩니다.
        /// </summary>
        public void MarkPlaced(Vector3 position)
            => _placed.Add(position);

        /// <summary>
        /// 기록된 모든 배치 위치를 지웁니다.
        /// </summary>
        public void Clear()
            => _placed.Clear();

        /// <summary>
        /// 이미 배치된 모든 위치로부터 최소 간격 이상 떨어져 있는지 검사합니다.
        /// </summary>
        private bool IsFarEnough(Vector3 position, float spacing)
        {
            float sqrSpacing = spacing * spacing;
            foreach (Vector3 placed in _placed)
                if ((placed - position).sqrMagnitude < sqrSpacing)
                    return false;

            return true;
        }
    }
}
