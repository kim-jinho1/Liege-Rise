using System;
using Code.Units.Base;
using Code.Units.Combat;
using Code.Units.Util;
using Unity.Behavior;
using UnityEngine;

namespace Code.Units.BT.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "Is in attackDistance", story: "[Target] is in [Unit] attack distance by [Config]", category: "Conditions", id: "61e1eb52831cd9f82257a7a383779f37")]
    public partial class IsInAttackDistanceCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<AbstractUnit> Unit;
        [SerializeReference] public BlackboardVariable<AttackConfigSO> Config;

        public override bool IsTrue()
        {
            if (Target.Value == null || Unit.Value == null || Config.Value == null)
                return false;

            Vector3 targetPosition = DistanceUtil.GetClosestTargetPosition(Unit.Value.gameObject, Target.Value);
            Vector3 selfPosition = Unit.Value.transform.position;

            return Vector3.Distance(targetPosition, selfPosition) <= Config.Value.AttackRange;
        }
    }
}
