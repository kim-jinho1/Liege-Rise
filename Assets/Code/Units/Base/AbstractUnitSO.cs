using Code.Commandable;
using UnityEngine;

namespace Code.Units.Base
{
    /// <summary>
    /// 유닛이 공통 데이터 SO입니다.
    /// </summary>
    public class AbstractUnitSO : AbstractCommandableSO
    {
        /// <summary>
        /// 다른 유닛과 겹치지 않도록 유지하는 최소 간격입니다.
        /// </summary>
        [field: SerializeField] public float SeparationRadius { get; private set; } = 0.4f;

        public override object Clone()
        {
            AbstractUnitSO copy = (AbstractUnitSO)base.Clone();
            return copy;
        }
    }
}
