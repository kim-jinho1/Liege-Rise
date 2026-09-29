using UnityEngine;

namespace Code.MapGeneration
{
    /// <summary>
    /// 맵 생성기가 읽는 씬 마커의 공통 기반입니다.
    /// 위치는 트랜스폼이, 영향 범위는 반지름이 정합니다. 셀 크기가 1이므로 반지름 숫자는 타일 수와 같습니다.
    /// </summary>
    public abstract class MapAnchor : MonoBehaviour
    {
        // 이 앵커가 다루는 원형 범위의 반지름 (월드 단위 = 타일 수)
        [field: SerializeField, Min(0f)] public float Radius { get; private set; } = 10f;

        /// <summary>
        /// 앵커의 중심 좌표입니다.
        /// </summary>
        public Vector3 Center => transform.position;

        /// <summary>
        /// 기즈모 색입니다. 파생 클래스가 자기 색을 지정합니다.
        /// </summary>
        protected abstract Color GizmoColor { get; }

        /// <summary>
        /// 기즈모에 함께 띄울 설명입니다. 씬에서 앵커 역할을 눈으로 구분하는 용도입니다.
        /// </summary>
        protected abstract string GizmoLabel { get; }

        /// <summary>
        /// 해당 좌표가 이 앵커의 범위 안에 있는지 검사합니다.
        /// 앵커의 z가 0이 아니어도 어긋나지 않도록 평면 거리만 봅니다.
        /// </summary>
        public bool Contains(Vector3 point)
        {
            Vector2 offset = (Vector2)point - (Vector2)Center;
            return offset.sqrMagnitude <= Radius * Radius;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 배치 범위를 항상 그려 작성자가 반경을 눈으로 조정할 수 있게 합니다.
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor;
            Gizmos.DrawWireSphere(Center, Radius);

            UnityEditor.Handles.color = GizmoColor;
            UnityEditor.Handles.Label(Center + Vector3.up * (Radius + 1f), GizmoLabel);
        }
#endif
    }
}
