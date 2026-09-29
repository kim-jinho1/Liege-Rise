using System;
using Code.Animations;
using Code.Economics;
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
    [NodeDescription(name: "Move to target gameObject", story: "[Unit] move to [TargetGameObject]", category: "Action", id: "6288eb26a046feb4503856e3df2e98e9")]
    public partial class MoveToTargetGameObjectAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractUnit> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;
        [SerializeReference] public BlackboardVariable<float> MoveThreshold = new(0.25f);
        [SerializeReference] public BlackboardVariable<ParameterSO> MoveParam;

        [SerializeReference] public BlackboardVariable<ResourceSO> SupplySO;
        [SerializeReference] public BlackboardVariable<ParameterSO> PawnIndexParameter;
        [SerializeReference] public BlackboardVariable<ParameterSO> TransportParameter;

        private PathAgent _agent;
        private UnitAnimator _animator;
        private Vector3 _lastPosition;
        private bool _isPawn;
        private bool _warnedNoPath;

        private float _noPathElapsed;
        private MoveStallWatchdog _watchdog;

        private const float NoPathTimeout = 2f;

        protected override Status OnStart()
        {
            if (Unit.Value == null || Unit.Value.Agent == null)
                return Status.Failure;

            _isPawn = Unit.Value is Pawn;
            if (_isPawn && (Unit.Value.UnitAnimator == null || PawnIndexParameter.Value == null
                              || TransportParameter.Value == null))
                return Status.Failure;

            if (TargetGameObject.Value == null)
                return Status.Success;

            _agent = Unit.Value.Agent;
            _animator = Unit.Value.UnitAnimator;

            Vector3 targetPosition = DistanceUtil.GetClosestTargetPosition(Unit.Value.gameObject, TargetGameObject.Value);
            if (Vector3.Distance(_agent.transform.position, targetPosition) < _agent.StoppingDistance)
                return Status.Success;

            SetMoveParam(true);
            UpdatePawnCarryAnimation();

            _noPathElapsed = 0f; // _warnedNoPath는 되돌리지 않는다 — 경고는 유닛당 1회다
            _agent.SetDestination(targetPosition);
            _lastPosition = targetPosition;
            _watchdog.Reset();
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (TargetGameObject.Value == null)
                return Status.Failure;

            // 도착 판정이 먼저다. 목표점은 콜라이더 표면 위라 이쪽이 움직이면 같이 미끄러지는데,
            // 재계산 분기가 앞에 있으면 그 프레임엔 도착을 아예 확인하지 않는다.
            if (MovementHelper.HasArrived(_agent))
                return Status.Success;

            Vector3 newTargetPosition = DistanceUtil.GetClosestTargetPosition(Unit.Value.gameObject, TargetGameObject.Value);
            if (Vector3.Distance(newTargetPosition, _lastPosition) >= MoveThreshold.Value)
            {
                _agent.SetDestination(newTargetPosition);
                _lastPosition = newTargetPosition;
                _watchdog.Reset(); // 목적지가 바뀌었으니 정체 판정을 처음부터 다시 센다
            }

            WarnIfNoPath();
            return HasGivenUpOnPath() || _watchdog.IsStuck(_agent) ? Status.Failure : Status.Running;
        }

        protected override void OnEnd()
        {
            if (_animator == null)
                return;

            UpdatePawnCarryAnimation();
            SetMoveParam(false);
        }

        /// <summary>
        /// Pawn의 운반 애니메이션 파라미터를 현재 자원 보유 상태에 맞게 갱신합니다.
        /// </summary>
        private void UpdatePawnCarryAnimation()
        {
            if (!_isPawn)
                return;

            bool hasSupplies = ((Pawn)Unit.Value).HasSupplies;
            PawnCarryVisuals.SetCarrying(
                _animator,
                TransportParameter.Value,
                SupplySO.Value != null ? PawnIndexParameter.Value : null,
                hasSupplies,
                SupplySO.Value != null ? SupplySO.Value.AnimationID : 0f);
        }

        /// <summary>
        /// 경로 계산이 실패하여 일정 시간 동안 경로를 찾지 못하면 실패 상태를 반환합니다.
        /// </summary>
        private bool HasGivenUpOnPath()
        {
            // 대기열에서 계산을 기다리는 중은 '경로 없음'이 아니다 — 예산 스케줄러가 밀리면
            // 그 지연이 그대로 포기 타이머에 쌓인다.
            if (_agent.IsOnPath || _agent.PathPending)
            {
                _noPathElapsed = 0f;
                return false;
            }

            _noPathElapsed += Time.deltaTime;
            return _noPathElapsed >= NoPathTimeout;
        }

        /// <summary>
        /// Unit의 이동 파라미터를 설정합니다.
        /// </summary>
        private void SetMoveParam(bool value)
            => MovementHelper.SetMoveParam(Unit.Value, MoveParam?.Value, value);

        /// <summary>
        /// 경로 계산에 실패하면 노드가 로그 없이 Running으로 대기하므로, 원인 추적용 경고를 1회 남깁니다.
        /// </summary>
        private void WarnIfNoPath()
        {
            // PathPending 중에 판정하면 출발하는 유닛마다 예외 없이 찍힌다 — 요청이 해결된 뒤에만 본다.
            if (_agent.IsOnPath || _agent.PathPending || _warnedNoPath)
                return;

            _warnedNoPath = true;
            Debug.LogWarning($"MoveToTargetGameObjectAction: {Unit.Value.name} → {TargetGameObject.Value.name} 경로 계산 실패 — 목적지가 경로 베이크 밖일 수 있습니다.", Unit.Value);
        }
    }
}
