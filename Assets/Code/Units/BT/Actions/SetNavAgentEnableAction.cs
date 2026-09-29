using System;
using Code.Units.Base;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Code.Units.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Set navAgent enable", story: "[Unit] set navAgent active status to [Active]", category: "Action/Navigation", id: "a035c153e1be8ae15aa9573a454efe00")]
    public partial class SetNavAgentEnableAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractUnit> Unit;
        [SerializeReference] public BlackboardVariable<bool> Active;
        [SerializeReference] public BlackboardVariable<bool> IsWarpTo = new(false);

        protected override Status OnStart()
        {
            if (Unit.Value == null || Unit.Value.Agent == null)
                return Status.Failure;

            Unit.Value.Agent.enabled = Active.Value;
            if (Active.Value && IsWarpTo.Value)
                Unit.Value.Agent.Warp(Unit.Value.transform.position);

            return Status.Success;
        }
    }
}
