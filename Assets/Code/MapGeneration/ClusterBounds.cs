using UnityEngine;

namespace Code.MapGeneration
{
    /// <summary>
    /// 배치 후보를 한 점 주위로 묶는 원형 제한입니다.
    /// 중심이 앵커면 진지 하나가 되고, 중심이 방금 놓은 씨앗이면 자원 한 무리가 됩니다.
    /// </summary>
    public readonly struct ClusterBounds
    {
        private readonly Vector3 _center;
        private readonly float _sqrRadius;

        public ClusterBounds(Vector3 center, float radius)
        {
            _center = center;
            _sqrRadius = radius * radius;
            IsActive = radius > 0f;
        }

        /// <summary>
        /// 아무 후보도 걸러 내지 않는 빈 제한입니다.
        /// </summary>
        public static ClusterBounds None => default;

        /// <summary>
        /// 이 제한이 실제로 후보를 걸러 내는지입니다.
        /// </summary>
        public bool IsActive { get; }

        /// <summary>
        /// 해당 좌표가 제한 안에 있는지 검사합니다.
        /// 앵커의 z가 0이 아니어도 어긋나지 않도록 평면 거리만 봅니다.
        /// </summary>
        public bool Contains(Vector3 point)
        {
            if (!IsActive)
                return true;

            Vector2 offset = (Vector2)point - (Vector2)_center;
            return offset.sqrMagnitude <= _sqrRadius;
        }
    }
}
