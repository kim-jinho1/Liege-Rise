using System;
using Code.Animations;
using Code.Navigation;
using Code.Units.Base;
using Code.Units.Util;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Code.Units.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Move to target location", story: "[Unit] move to [TargetLocation]", category: "Action/Navigation", id: "b2279e6d74bb05ea5d0f3ab2adfca23a")]
    public partial class MoveToTargetLocationAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractUnit> Unit;
        [SerializeReference] public BlackboardVariable<Vector3> TargetLocation;
        [SerializeReference] public BlackboardVariable<ParameterSO> MoveParam;

        private PathAgent _agent;

        protected override Status OnStart()
        {
            if(Unit.Value == null)
                return Status.Failure;

            _agent = Unit.Value.Agent;
            if (_agent == null)
                return Status.Failure;

            if (Vector3.Distance(_agent.transform.position, TargetLocation.Value) < _agent.StoppingDistance)
                return Status.Success;

            SetMoveParam(true);
            _agent.SetDestination(TargetLocation.Value);
            return _agent.IsOnPath ? Status.Running : Status.Failure;
        }

        protected override Status OnUpdate()
        {
            if (_agent == null)
                return Status.Failure;

            if (MovementHelper.HasArrived(_agent))
                return Status.Success;

            return Status.Running;
        }

        protected override void OnEnd()
        {
            SetMoveParam(false);
        }

        private void SetMoveParam(bool value)
            => MovementHelper.SetMoveParam(Unit.Value, MoveParam?.Value, value);
    }
}
