using System;
using UnityEngine;

namespace Code.MapGeneration
{
    /// <summary>
    /// 한 링에 뿌릴 프리팹 한 종류의 배치 규칙입니다.
    /// </summary>
    [Serializable]
    public struct SpawnEntry
    {
        public GameObject prefab;       // 배치할 프리팹. Owner는 프리팹 값 그대로 쓴다

        public Vector2Int countRange;   // 배치 개수 범위(x 최소, y 최대)
        public Vector2Int footprint;    // 건설 판정에 쓸 타일 크기. 자원 노드처럼 작은 것은 (1,1)

        public float minSpacing;        // 다른 배치물과의 최소 거리(월드). 뭉칠 때는 무리 사이 거리

        public int clumpSize;           // 한 무리에 놓을 개수. 0·1이면 뭉치지 않는다
        public float clumpRadius;       // 무리가 퍼지는 반경. minSpacing보다 작아야 남의 무리에 안 낀다
        public float clumpSpacing;      // 무리 안에서 유지할 최소 거리. 0이면 전역 최소 간격

        /// <summary>
        /// 이 항목이 배치 가능한 상태인지 검사합니다.
        /// </summary>
        public bool IsValid
            => prefab != null && countRange.y > 0;

        /// <summary>
        /// 무리를 지어 놓는 항목인지입니다.
        /// </summary>
        public bool IsClumped
            => clumpSize > 1 && clumpRadius > 0f;

        /// <summary>
        /// 실제로 시도할 배치 개수를 반환합니다.
        /// </summary>
        public int ResolveCount(System.Random random)
        {
            int min = Mathf.Max(0, countRange.x);
            int max = Mathf.Max(min, countRange.y);
            return min == max ? min : random.Next(min, max + 1);
        }

        /// <summary>
        /// 건설 판정에 쓸 타일 크기입니다. 미설정 시 1x1로 간주합니다.
        /// </summary>
        public Vector2Int SafeFootprint
            => new(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
    }
}
