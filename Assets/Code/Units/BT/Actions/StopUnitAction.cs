using System;
using Code.Units.Base;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Code.Units.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Stop Unit", story: "[Unit] Stop moving", category: "Action/Navigation", id: "19e720ea64b8e676491a4fca146b3898")]
    public partial class StopUnitAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<AbstractUnit> Unit;

        protected override Status OnStart()
        {
            if (Unit.Value == null || Unit.Value.Agent == null)
                return Status.Failure;

            Unit.Value.Agent.ResetPath();
            Unit.Value.SetVariableValue(BTVariables.Command, UnitCommands.Stop);

            return Status.Success;
        }
    }
}
