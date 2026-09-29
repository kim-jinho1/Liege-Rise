using UnityEngine;

namespace Code.Military.Squads.Formations
{
    /// <summary>
    /// 분대원들을 직선으로 배치하는 포메이션입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Line Formation", menuName = "Military/Formation/Line", order = 30)]
    public class LineFormationSO : FormationSO
    {
        public override Vector2 GetOffset(int slotIndex, int memberCount, float spacing)
        {
            float centered = slotIndex - (memberCount - 1) * 0.5f;
            return new Vector2(centered * spacing, 0f);
        }
    }
}
