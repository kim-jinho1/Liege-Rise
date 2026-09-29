using UnityEngine;

namespace Code.Military.Squads.Formations
{
    /// <summary>
    /// 분대원들을 원형으로 배치하는 포메이션입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Circle Formation", menuName = "Military/Formation/Circle", order = 30)]
    public class CircleFormationSO : FormationSO
    {
        public override Vector2 GetOffset(int slotIndex, int memberCount, float spacing)
        {
            if (slotIndex <= 0)
                return Vector2.zero;

            float angle = (slotIndex - 1) * (360f / Mathf.Max(1, memberCount - 1));
            return (Vector2)(Quaternion.Euler(0f, 0f, angle) * Vector3.up) * spacing;
        }
    }
}
