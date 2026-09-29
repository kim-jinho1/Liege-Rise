using System;
using Code.Economics;
using UnityEngine;

namespace Code.Commandable
{
    public abstract class UnlockableSO : ScriptableObject, ICloneable
    {
        [field: SerializeField] public string Name { get; private set; } = "Unit";
        [field: SerializeField] public float BuildTime { get; private set; } = 5;
        [field: SerializeField] public Sprite Icon { get; private set; }

        /// <summary>
        /// UI 툴팁에 표시할 역할 설명입니다.
        /// </summary>
        [field: TextArea(2, 4)]
        [field: SerializeField] public string Description { get; private set; }

        [field: SerializeField] public ResourceCostSO Cost { get; private set; }

        public virtual object Clone()
        {
            UnlockableSO copy = Instantiate(this);
            copy.Cost = Cost == null ? null : Instantiate(Cost);

            return copy;
        }
    }
}
