using System;
using Code.Animations;
using Code.Audio;
using Code.Economics;
using Code.Environments;
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
    [NodeDescription(name: "Gather supplies", story: "[Unit] gather [Amount] supplies", category: "Action", id: "c4c6ca3afcce0f2ab7587c7803bb3341")]
    public partial class GatherSuppliesAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractUnit> Unit;
        [SerializeReference] public BlackboardVariable<int> Amount;
        [SerializeReference] public BlackboardVariable<ResourceSO> SupplySO;
        [SerializeReference] public BlackboardVariable<ParameterSO> InteractParameter;
        [SerializeReference] public BlackboardVariable<ParameterSO> PawnIndexParameter;
        [SerializeReference] public BlackboardVariable<ParameterSO> TransportParameter;

        private float _enterTime;
        private UnitAnimator _animator;
        private ResourceObject _resource;
        private ResourceSO _cachedResource;
        private Pawn _pawn;
        private bool _began;

        protected override Status OnStart()
        {
            _began = false;
            if (Unit.Value is not Pawn pawn || TransportParameter.Value == null || InteractParameter.Value == null)
                return Status.Failure;

            _pawn = pawn;
            _resource = pawn.TargetResource;
            if (_resource == null || !_resource.CanGather(pawn))
                return Status.Failure;

            _began = true;

            _cachedResource = _resource.ResourceSO;
            SupplySO.Value = _cachedResource;

            _animator = pawn.UnitAnimator;
            _animator.SetParameter(PawnIndexParameter.Value, _cachedResource.GatherID);

            // 도착하자마자 휘두르기 시작한다. 되돌리는 곳은 OnEnd 하나뿐이다
            _animator.SetParameter(InteractParameter.Value, true);

            // 채집 중에는 이동이 멈추므로 자원 본체 방향으로 facing을 고정합니다.
            pawn.Agent?.FaceTowards(_resource.transform.position);

            _enterTime = Time.time;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (_pawn.TargetResource != _resource)
                return Status.Failure;

            // 폰 클립에는 애니메이션 이벤트가 없어 OnAnimationEnd가 영원히 오지 않는다.
            // 망치질과 같은 방식으로 재생 위상에서 타격 시점을 읽는다.
            // 간격은 폰마다 따로 잰다 — 소리 종류로 묶으면 여럿이 캘 때 한 명분만 들린다
            if (_animator.ConsumeLoopBeat(_cachedResource.GatherBeatPhase))
                EconomyAudio.PlayGather(_cachedResource, _pawn.transform.position, _pawn.GetHashCode());

            return _cachedResource.BaseGatherTime + _enterTime <= Time.time
                ? Status.Success
                : Status.Running;
        }

        protected override void OnEnd()
        {
            if (_animator != null)
                _animator.SetParameter(InteractParameter.Value, false);

            if (_began && CurrentStatus == Status.Success && _resource != null)
                Amount.Value = _resource.Collect();

            RefreshCarryVisuals();

            if (_resource == null)
                return;

            // 성공이든 실패든 자리는 비운다. 예약이 없으면 아무 일도 하지 않는다.
            _resource.ReleaseGatherPoint(_pawn);

            // 고갈된 목표만 놓는다. 남아 있으면 유지해서 입금 후 같은 자원으로 돌아온다.
            // 채집 중에 사용자가 다른 자원을 지정했다면 그 목표를 덮어쓰지 않는다.
            if (_pawn != null && _pawn.TargetResource == _resource && _resource.IsDepleted)
                _pawn.TargetResource = null;
        }

        /// <summary>
        /// 실제 보유량에 맞춰 운반 시각 상태를 갱신합니다.
        /// 채집이 중단·실패해 빈손인데도 짐 든 모습으로 남는 것을 막습니다.
        /// </summary>
        private void RefreshCarryVisuals()
        {
            if (_animator == null)
                return;

            if (Amount.Value <= 0)
            {
                PawnCarryVisuals.Clear(_animator, TransportParameter.Value, PawnIndexParameter.Value);
                return;
            }

            float carryIndex = _cachedResource != null ? _cachedResource.GatherID : 0f;
            PawnCarryVisuals.SetCarrying(_animator, TransportParameter.Value, PawnIndexParameter.Value,
                true, carryIndex);
        }
    }
}
