using System;
using Code.Animations;
using Code.Commandable.Interfaces;
using Code.Navigation;
using Code.Units.Combat;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Code.Units.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Attack target", story: "[Unit] attack [Target] until it dies", category: "Action/Units", id: "a83240b5912c0337fb93d26721a18f9a")]
    public partial class AttackTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractCombatUnit> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<AttackConfigSO> AttackConfig;
        [SerializeReference] public BlackboardVariable<ParameterSO> AttackParam;

        [SerializeReference] public BlackboardVariable<Transform> FirePositionTrm;
        [SerializeReference] public BlackboardVariable<float> MeleeOffset = new(0.8f);

        private Transform _targetTransform;
        private IDamageable _targetDamageable;
        private PathAgent _agent;
        private UnitAnimator _unitAnimator;
        private CombatSwingHelper _swing;
        private bool _isAnimationEnd;
        private bool _waitingForCooldown;

        protected override Status OnStart()
        {
            if (!InitializeNode())
                return Status.Failure;

            // 쿨다운을 기다리든 바로 휘두르든, 공격 노드에 들어온 순간 발을 멈춘다.
            // 스윙이 시작된 뒤에 멈추면 쿨다운 내내 미끄러지고, 애니메이터가 없는 유닛은
            // CombatSwingHelper가 인라인으로 데미지를 넣어 멈추기 전에 이미 때린다.
            if (_agent != null)
                _agent.IsStopped = true;

            _isAnimationEnd = false;
            if (!_swing.IsReady(Unit.Value.AttackDelayMultiplier))
            {
                _waitingForCooldown = true;
                return Status.Running;
            }

            _waitingForCooldown = false;
            BeginAttack();
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            // 애니메이션 이벤트를 못 받아 스윙이 굳었으면 여기서 풀어 준다 — 안 풀면 이 유닛은
            // 사거리 안에 선 채로 다시는 공격하지 않는다
            _swing.RecoverIfStalled();

            if (IsTargetGone())
                return _swing.IsSwinging ? Status.Running : Status.Success; // 타겟이 죽어도 재생 중인 스윙은 끝까지 재생한 뒤 종료한다

            if (!_waitingForCooldown)
                return _isAnimationEnd ? Status.Success : Status.Running;

            if (!_swing.IsReady(Unit.Value.AttackDelayMultiplier))
                return Status.Running;

            _waitingForCooldown = false;
            BeginAttack();
            return Status.Running;
        }

        protected override void OnEnd()
        {
            if (_agent != null)
                _agent.IsStopped = false;

            if (_swing == null)
                return;

            // 타겟이 스윙 도중 죽었다면 남은 클립을 끝까지 재생하도록 둔다(뚝 끊기는 연출 방지).
            _swing.End(IsTargetGone() && _swing.IsSwinging);
        }

        /// <summary>
        /// 타겟이 사라졌거나 죽는 중인지 판정합니다.
        /// 죽는 연출이 도는 동안에도 오브젝트는 살아 있으므로 Transform만 봐서는 안 됩니다 —
        /// 그러면 내가 죽인 순간 내 공격 모션이 중간에 끊깁니다.
        /// </summary>
        private bool IsTargetGone()
            => _targetDamageable == null || _targetDamageable.Transform == null || _targetDamageable.IsDead;

        private void BeginAttack()
        {
            _isAnimationEnd = false;
            if (!_swing.TryStart(_targetTransform, _targetDamageable))
                _waitingForCooldown = true;
        }

        private bool InitializeNode()
        {
            if (Unit.Value == null || Target.Value == null || AttackConfig.Value == null)
                return false;

            _targetTransform = Target.Value.transform;
            _agent = Unit.Value.Agent;
            _unitAnimator = Unit.Value.UnitAnimator;

            if (!_targetTransform.TryGetComponent(out _targetDamageable))
                return false;

            EnsureSwingHelper();
            return true;
        }

        /// <summary>
        /// 스윙 헬퍼를 초기화합니다.
        /// 이미 초기화되어 있으면 아무 작업도 하지 않습니다.
        /// </summary>
        private void EnsureSwingHelper()
        {
            if (_swing != null)
                return;

            _swing = new CombatSwingHelper(Unit.Value, _unitAnimator, AttackConfig.Value, AttackParam.Value,
                Unit.Value.IndexParam, Unit.Value.ClipCount, FirePositionTrm.Value, MeleeOffset.Value);
            _swing.SwingEnded += HandleSwingEnded;
        }

        private void HandleSwingEnded()
            => _isAnimationEnd = true;
    }
}
