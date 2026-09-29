using System;
using Code.Animations;
using Code.Buildings.Construction;
using Code.Navigation;
using Code.Rendering;
using Code.Units.Base;
using Code.Units.Pawns;
using Code.Units.Util;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Code.Units.BT.Actions
{
    public enum ConstructPhase
    {
        Moving,
        Working
    }

    /// <summary>
    /// [Unit]이 [TargetGameObject]로 이동하여 건설 작업을 수행하도록 합니다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Construct Building", story: "[Unit] constructs [TargetGameObject]", category: "Action/Units", id: "b7c41f9e2a5d4e6c8f0a1b2c3d4e5f60")]
    public partial class ConstructBuildingAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractUnit> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;
        [SerializeReference] public BlackboardVariable<ParameterSO> MoveParam;
        [SerializeReference] public BlackboardVariable<ParameterSO> PawnIndexParameter;
        [SerializeReference] public BlackboardVariable<ParameterSO> InteractParameter;

        [SerializeReference] public BlackboardVariable<float> BuildAnimationId = new(1f);
        [SerializeReference] public BlackboardVariable<float> IdleAnimationId = new(0f);

        private UnitAnimator _animator;

        private ConstructPhase _phase;
        private PathAgent _agent;
        private MoveStallWatchdog _watchdog;
        private SortingOverride _sorting;
        private Pawn _pawn;
        private ConstructionDummy _dummy;
        private PawnConstructionRunner _runner;
        private Vector3 _workPosition;

        protected override Status OnStart()
        {
            if (Unit.Value is not Pawn pawn)
                return Status.Failure;

            _pawn = pawn;
            _animator = pawn.UnitAnimator;
            _dummy = TargetGameObject.Value != null
                ? TargetGameObject.Value.GetComponentInParent<ConstructionDummy>()
                : null;

            _runner = _dummy != null ? _dummy.GetComponent<PawnConstructionRunner>() : null;
            if (_dummy == null || _runner == null)
            {
                pawn.Stop();
                return Status.Success;
            }

            _agent = pawn.Agent;
            _phase = ConstructPhase.Moving;

            _runner.RegisterAssigned(pawn);
            _workPosition = _dummy.GetWorkPosition(_runner.GetWorkSlot(pawn));

            MovementHelper.SetMoveParam(Unit.Value, MoveParam?.Value, true);
            _agent.SetDestination(_workPosition);
            _watchdog.Reset();
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (_dummy == null)
            {
                _pawn?.Stop();
                return Status.Success;
            }

            if (_phase == ConstructPhase.Working)
                return Status.Running;

            if (!MovementHelper.HasArrived(_agent))
                // 경로 계산이 실패하면 남은 거리가 영원히 줄지 않는다. 포기해야 다른 일을 잡는다.
                return _watchdog.IsStuck(_agent) ? Status.Failure : Status.Running;

            TransitionToWorking();
            return Status.Running;
        }

        protected override void OnEnd()
        {
            MovementHelper.SetMoveParam(Unit.Value, MoveParam?.Value, false);
            SetAnimation(IdleAnimationId.Value);
            SetInteract(false);

            if (_runner != null && _pawn != null)
                _runner.WorkerLeft(_pawn);

            if (_agent != null)
                _agent.IsStopped = false;

            _sorting.Restore();
            _phase = ConstructPhase.Moving;
        }

        private void TransitionToWorking()
        {
            MovementHelper.SetMoveParam(Unit.Value, MoveParam?.Value, false);
            _agent.IsStopped = true;

            _pawn.transform.position = _workPosition;
            _agent.FaceTowards(_dummy.transform.position);

            // 옆/뒤 슬롯은 현장보다 바닥선이 높아 그대로 두면 건물 뒤로 숨는다.
            _sorting.Apply(_pawn.gameObject, _dummy.SortingOrder + 1);

            SetAnimation(BuildAnimationId.Value);
            SetInteract(true);

            _runner.WorkerArrived(_pawn);
            _phase = ConstructPhase.Working;
        }

        private void SetInteract(bool value)
        {
            if (_animator != null && InteractParameter?.Value != null)
                _animator.SetParameter(InteractParameter.Value, value);
        }

        private void SetAnimation(float value)
        {
            if (_animator != null && PawnIndexParameter?.Value != null)
                _animator.SetParameter(PawnIndexParameter.Value, value);
        }
    }
}
