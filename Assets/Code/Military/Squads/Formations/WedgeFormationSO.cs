using UnityEngine;

namespace Code.Military.Squads.Formations
{
    /// <summary>
    /// 분대원들을 쐐기 모양으로 배치하는 포메이션입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Wedge Formation", menuName = "Military/Formation/Wedge", order = 30)]
    public class WedgeFormationSO : FormationSO
    {
        public override Vector2 GetOffset(int slotIndex, int memberCount, float spacing)
        {
            if (slotIndex <= 0)
                return Vector2.zero;

            int rank = (slotIndex + 1) / 2;             // 1,1,2,2,3,3 …
            float side = slotIndex % 2 == 1 ? -1f : 1f; // 좌 · 우 교대
            return new Vector2(side * rank * spacing, -rank * spacing);
        }
    }
}
