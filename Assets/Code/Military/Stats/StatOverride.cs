using System;
using UnityEngine;

namespace Code.Military.Stats
{
    /// <summary>
    /// 스탯 정의 하나와, 이 소유자에게만 적용할 기본값 조정입니다.
    /// 같은 정의를 쓰면서 분대마다 다른 수치를 주는 통로입니다.
    /// </summary>
    [Serializable]
    public class StatOverride
    {
        [field: Header("Stat")]
        [field: SerializeField] public StatSO Definition { get; private set; }
        [SerializeField] private bool useOverride;
        [SerializeField] private float overrideBaseValue;

        /// <summary>
        /// 소유자 전용 런타임 스탯을 만듭니다.
        /// </summary>
        public StatSO CreateRuntimeStat()
        {
            StatSO runtimeStat = (StatSO)Definition.Clone();
            if (useOverride)
                runtimeStat.BaseValue = overrideBaseValue;

            return runtimeStat;
        }
    }
}
