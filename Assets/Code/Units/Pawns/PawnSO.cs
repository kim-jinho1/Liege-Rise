using Code.Units.Base;
using UnityEngine;

namespace Code.Units.Pawns
{
    [CreateAssetMenu(fileName = "Pawn", menuName = "Units/Pawn", order = 10)]
    public class PawnSO : AbstractUnitSO
    {
        /// <summary>
        /// 일꾼이 한 번에 운반할 수 있는 최대 자원량
        /// </summary>
        [field: SerializeField] public int CarryCapacity { get; private set; } = 6;

        public override object Clone()
        {
             return (PawnSO)base.Clone();
        }
    }
}
