using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 활성화되는 즉시 완료되는 조건입니다. 안내 전용(읽고 넘기는) 스텝에 사용합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_Immediate", menuName = "Objectives/Conditions/Immediate")]
    public class ImmediateConditionSO : ObjectiveConditionSO
    {
        public override ConditionTracker CreateTracker()
            => new ImmediateTracker();
    }

    public class ImmediateTracker : ConditionTracker
    {
        public override void Subscribe()
            => Complete();

        public override void Unsubscribe()
        {
        }
    }
}
