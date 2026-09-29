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
    /// <summary>
    /// 타겟에 한 번 공격 후 자폭합니다. 자폭 유닛 전용 BT 액션입니다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "OneAttackTarget", story: "[Unit] one attack [Target] then die", category: "Action/Units", id: "f4b8dd8883e2185ad9c56a244e1ef5ca")]
    public partial class OneAttackTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractCombatUnit> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<AttackConfigSO> AttackConfig;
        [SerializeReference] public BlackboardVariable<ParameterSO> AttackParam;

        [SerializeReference] public BlackboardVariable<Transform> FirePositionTrm;
        [SerializeReference] public BlackboardVariable<float> MeleeOffset = new(0.8f);

        private IDamageable _targetDamageable;
        private PathAgent _agent;
        private UnitAnimator _unitAnimator;
        private CombatSwingHelper _swing;
        private bool _isAnimationEnd;
        private bool _didAttack;
        private bool _didStartSwing;

        protected override Status OnStart()
        {
            if (!InitializeNode())
                return Status.Failure;

            _isAnimationEnd = false;
            _didAttack = false;
            _agent.IsStopped = true;

            // 인덱스 파라미터/타겟 방향 전환 없이 단발 스윙(원본 동작 유지).
            _didStartSwing = _swing.TryStart(null, _targetDamageable);
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            // 애니메이션 이벤트를 못 받아 굳은 스윙을 풀어 준다(AttackTargetAction과 같은 이유)
            _swing.RecoverIfStalled();

            // 보급 차감 실패로 스윙이 시작되지 못했다면 보급이 닿을 때까지 재시도한다.
            if (!_didStartSwing)
            {
                _didStartSwing = _swing.TryStart(null, _targetDamageable);
                return Status.Running;
            }

            return _isAnimationEnd ? Status.Success : Status.Running;
        }

        protected override void OnEnd()
        {
            _swing?.End();

            if (_didAttack && Unit.Value != null)
                Unit.Value.Die();
        }

        private bool InitializeNode()
        {
            if (Unit.Value == null || Target.Value == null || AttackConfig.Value == null)
                return false;

            _agent = Unit.Value.Agent;
            _unitAnimator = Unit.Value.UnitAnimator;

            if (!Target.Value.TryGetComponent(out _targetDamageable))
                return false;

            EnsureSwingHelper();
            return true;
        }

        /// <summary>
        /// 스윙 헬퍼를 초기화합니다.
        /// 이미 초기화되어 있으면 무시합니다.
        /// </summary>
        private void EnsureSwingHelper()
        {
            if (_swing != null)
                return;

            _swing = new CombatSwingHelper(Unit.Value, _unitAnimator, AttackConfig.Value, AttackParam.Value,
                null, 1, FirePositionTrm.Value, MeleeOffset.Value);
            _swing.DamageDealt += HandleDamageDealt;
            _swing.SwingEnded += HandleSwingEnded;
        }

        private void HandleDamageDealt()
            => _didAttack = true;

        private void HandleSwingEnded()
            => _isAnimationEnd = true;
    }
}
