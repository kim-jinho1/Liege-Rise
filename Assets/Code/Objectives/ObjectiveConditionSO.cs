using UnityEngine;

namespace Code.Objectives
{
    public abstract class ObjectiveConditionSO : ScriptableObject
    {
        /// <summary>
        /// 런타임 추적 인스턴스를 생성합니다. SO에 상태를 저장하지 않습니다.
        /// </summary>
        public abstract ConditionTracker CreateTracker();
    }
}
