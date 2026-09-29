#if UNITY_EDITOR
using Code.Navigation.Graph;
using UnityEngine;

namespace Code.Navigation.Baking
{
    /// <summary>
    /// PathBaker의 에디터 전용 그래프 시각화(gizmo) partial입니다.
    /// </summary>
    public partial class PathBaker
    {
        private static readonly Color RampGizmoColor = new(0.2f, 0.9f, 0.75f);
        private static readonly Color HighGizmoColor = new(0.95f, 0.55f, 0.25f);

        private void OnDrawGizmos()
        {
            if (!isDrawGizmo || bakedData == null || bakedData.points == null)
                return;

            foreach (NodeData node in bakedData.points)
            {
                Gizmos.color = GetNodeGizmoColor(node);
                Gizmos.DrawWireSphere(node.worldPosition, 0.15f);

                foreach (LinkData link in node.neighbors)
                {
                    bool crossLevel = bakedData.TryGetNode(link.endCellPosition, out NodeData other)
                                      && other.level != node.level;
                    Gizmos.color = crossLevel ? RampGizmoColor : edgeColor;
                    DrawArrowGizmo(link.startPosition, link.endPosition);
                }
            }
        }

        /// <summary>
        /// 램프는 단색으로, 고지대는 층이 높을수록 진하게 칠해 절벽 경계를 눈으로 확인합니다.
        /// </summary>
        private Color GetNodeGizmoColor(in NodeData node)
        {
            if (node.topographyType == TopographyType.Ramp)
                return RampGizmoColor;

            return node.level <= 0
                ? nodeColor
                : Color.Lerp(nodeColor, HighGizmoColor, Mathf.Clamp01(0.4f + node.level * 0.3f));
        }

        private void DrawArrowGizmo(Vector3 start, Vector3 end)
        {
            Vector3 dir = (end - start).normalized;
            Vector3 arrowEnd = end - dir * 0.15f;
            Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;
            const float arrowSize = 0.2f;
            Gizmos.DrawLine(start, arrowEnd);
            Gizmos.DrawLine(arrowEnd, arrowEnd + right * arrowSize);
            Gizmos.DrawLine(arrowEnd, arrowEnd + left * arrowSize);
        }
    }
}
#endif
