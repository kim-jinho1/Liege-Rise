using System;
using Code.Animations;
using Code.Economics;
using Code.Environments;
using Code.Navigation;
using Code.Units.Base;
using Code.Units.Pawns;
using Code.Units.Util;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Code.Units.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Move to available supply", story: "[Unit] move to [TargetFlower] filtered by [ResourceFilter]", category: "Action", id: "fe51a728e33c6ce7be0039fd40a6b1ed")]
    public partial class MoveToAvailableSupplyAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractUnit> Unit;
        [SerializeReference] public BlackboardVariable<ResourceSO> ResourceFilter;
        [SerializeReference] public BlackboardVariable<float> SearchRadius = new(7f);
        [SerializeReference] public BlackboardVariable<ParameterSO> MoveParam;

        private ResourceObject _resource;

        private PathAgent _agent;
        private Vector3 _targetWorldPos;
        private MoveStallWatchdog _watchdog;

        protected override Status OnStart()
        {
            if (Unit.Value == null || Unit.Value.Agent == null)
                return Status.Failure;

            _agent = Unit.Value.Agent;
            if (!TryUsePresetResource() && !FindAndSetTarget())
            {
                // 채집할 자원이 하나도 없다. 명령을 유휴로 되돌리지 않으면 Command가 Gather로 남아
                // 매 프레임 실패만 반복하고, 그 폰은 건설·상주 배정 대상에서도 영원히 제외된다.
                Unit.Value.Stop();
                return Status.Failure;
            }

            _agent.IgnoreStoppingDistance = true;
            SetMoveParam(true);
            _agent.SetDestination(_targetWorldPos);
            _watchdog.Reset();
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (Unit.Value is Pawn pawn && pawn.TargetResource != _resource)
                return Status.Failure;

            if (!_agent.PathPending && !(_agent.RemainingDistance > 0.01f))
                return Status.Success;

            // 진척이 없으면 무한 대기 대신 실패를 내야 BT가 다른 자원을 재탐색한다
            return _watchdog.IsStuck(_agent) ? Status.Failure : Status.Running;

        }

        protected override void OnEnd()
        {
            if (_agent != null)
                _agent.IgnoreStoppingDistance = false;

            SetMoveParam(false);

            if (CurrentStatus == Status.Success || _resource == null || Unit.Value is not Pawn pawn)
                return;

            _resource.ReleaseGatherPoint(pawn);

            // 도달하지 못한 자원을 목표로 남겨 두면 다음 시도에서 같은 자원을 다시 붙잡아
            // 무한히 재시도한다. 목표를 비워 다른 자원을 찾게 한다.
            // 사용자가 채집 중에 다른 자원을 지정했다면 그 목표는 건드리지 않는다.
            if (pawn.TargetResource == _resource)
                pawn.TargetResource = null;
        }

        private void SetMoveParam(bool value)
            => MovementHelper.SetMoveParam(Unit.Value, MoveParam?.Value, value);

        /// <summary>
        /// 블랙보드에 이미 유효한 ResourceObject가 설정된 경우 해당 위치를 그대로 사용합니다.
        /// </summary>
        private bool TryUsePresetResource()
        {
            // Pawn 필드에서 직접 읽어 blackboard GameObject 역직렬화 문제를 우회합니다.
            if (Unit.Value is not Pawn pawn)
                return false;

            ResourceObject resource = pawn.TargetResource;
            if (resource == null || resource.IsDepleted)
                return false;

            int index = resource.ReserveGatherPoint(pawn, _agent.transform.position);
            if (index < 0)
                return false; // 만석이면 아래 탐색으로 넘어간다

            _resource = resource;
            _targetWorldPos = resource.GetGatherPoint(index);
            return true;
        }

        /// <summary>
        /// 채집 가능한 자원을 찾아 목표로 설정합니다.
        /// SearchRadius 안을 먼저 보고, 없으면 맵 전체에서 가장 가까운 것으로 폴백합니다.
        /// </summary>
        private bool FindAndSetTarget()
        {
            ResourceObjectManager manager = ResourceObjectManager.Instance;
            if (manager == null || Unit.Value is not Pawn pawn)
                return false;

            Vector3 from = _agent.transform.position;

            // 반경으로만 끊으면 채집하던 자원이 고갈됐을 때 폰이 그대로 멈춰 버린다.
            // 가까운 자원을 우선하되, 반경 밖에 남아 있으면 그쪽으로 보낸다.
            //
            // 탐색과 예약을 한 번에 맡긴다. 예전처럼 가장 가까운 것 하나만 받아 와서 예약에 실패하면
            // 다음 후보를 못 보고 그대로 Stop으로 굳었다 — 자원이 고갈돼 여러 일꾼이 동시에
            // 재탐색할 때 정확히 그 상황이 된다.
            if (!manager.TryReserveNearest(pawn, from, SearchRadius.Value, ResourceFilter.Value,
                    out ResourceObject resource, out int index)
                && !manager.TryReserveNearest(pawn, from, float.PositiveInfinity, ResourceFilter.Value,
                    out resource, out index))
                return false;

            pawn.TargetResource = resource;
            _resource = resource;
            _targetWorldPos = resource.GetGatherPoint(index);
            return true;
        }
    }
}
