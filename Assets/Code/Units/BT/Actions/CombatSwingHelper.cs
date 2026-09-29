using System;
using Code.Animations;
using Code.Commandable.Interfaces;
using Code.Units.Combat;
using Code.Units.Util;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Code.Units.BT.Actions
{
    /// <summary>
    /// 전투 유닛의 1회 스윙(공격 애니메이션 → 데미지 해결 → 종료) 생명주기를 담당합니다.
    /// 애니메이션 이벤트 구독/해제를 캡슐화해 노드 전환 시 단일 정리(End)를 보장합니다.
    /// </summary>
    public sealed class CombatSwingHelper
    {
        private readonly AbstractCombatUnit _unit;
        private readonly UnitAnimator _anim;
        private readonly AttackConfigSO _config;
        private readonly ParameterSO _attackParam;
        private readonly ParameterSO _indexParam;
        private readonly int _clipCount;
        private readonly Transform _firePosition;
        private readonly float _meleeOffset;

        // 스윙 종료 신호를 이만큼 못 받으면 끝난 것으로 친다. 공격 클립은 전부 1초 미만이다
        private const float MaxSwingSeconds = 2f;

        private IDamageable _target;
        private bool _swinging;
        private bool _swingEnded;
        private float _swingStartTime;
        private float _lastAttackTime = float.NegativeInfinity;

        /// <summary>
        /// 현재 스윙 진행 중인지 여부입니다.
        /// </summary>
        public bool IsSwinging => _swinging;

        /// <summary>
        /// 스윙 애니메이션이 종료됐을 때(무애니 유닛은 즉시) 발생합니다.
        /// </summary>
        public event Action SwingEnded;

        /// <summary>
        /// 타격 시점에 데미지가 실제로 해결됐을 때 발생합니다.
        /// </summary>
        public event Action DamageDealt;

        /// <summary>
        /// 스윙 헬퍼를 생성합니다. 공격에 필요한 참조를 캐싱합니다.
        /// </summary>
        public CombatSwingHelper(AbstractCombatUnit unit, UnitAnimator anim, AttackConfigSO config,
            ParameterSO attackParam, ParameterSO indexParam, int clipCount, Transform firePosition, float meleeOffset)
        {
            _unit = unit;
            _anim = anim;
            _config = config;
            _attackParam = attackParam;
            _indexParam = indexParam;
            _clipCount = clipCount;
            _firePosition = firePosition;
            _meleeOffset = meleeOffset;
        }

        /// <summary>
        /// 쿨다운이 끝나 다음 스윙을 시작할 수 있는지 반환합니다.
        /// </summary>
        public bool IsReady(float attackDelayMultiplier)
            => !_swinging && _config != null
                          && Time.time >= _lastAttackTime + _config.AttackDelay * attackDelayMultiplier;

        /// <summary>
        /// 지정 타겟을 향해 스윙을 시작합니다. 애니메이터가 없으면 즉시 데미지를 해결합니다.
        /// </summary>
        public bool TryStart(Transform targetTransform, IDamageable target)
        {
            _target = target;
            _swinging = true;
            _swingEnded = false;
            _swingStartTime = Time.time;

            if (_anim == null)
            {
                ResolveDamage();
                _lastAttackTime = Time.time;
                _swinging = false;
                SwingEnded?.Invoke();
                return true;
            }

            // 이전(특히 중단된) 스윙 구독이 남아 있을 수 있으므로 재구독 전 방어적으로 해제합니다.
            _anim.OnAttackTrigger -= ResolveDamage;
            _anim.OnAnimationEnd -= HandleSwingEnd;

            _anim.OnAttackTrigger += ResolveDamage;
            _anim.OnAnimationEnd += HandleSwingEnd;
            if (_clipCount > 1 && _indexParam != null)
                _anim.SetParameter(_indexParam, Random.Range(0f, _clipCount));

            if (targetTransform != null)
                _unit.SetFacingDirection(targetTransform.position - _unit.transform.position);

            if (_attackParam != null)
                _anim.SetParameter(_attackParam, true);

            return true;
        }

        /// <summary>
        /// 스윙 종료 신호를 못 받은 채 너무 오래 흘렀으면 끝난 것으로 처리합니다.
        /// 스윙의 시작·타격·종료는 애니메이션 이벤트가 몹니다. 애니메이터가 컬링되면
        /// (안개가 렌더러를 끄면 그렇게 됩니다) 그 이벤트가 오지 않고, 그러면 _swinging이
        /// 참으로 굳어 IsReady가 영영 거짓이 됩니다 — 그 유닛은 사거리 안에 서서 다시는
        /// 공격하지 않습니다. 스스로 빠져나올 경로가 없으므로 여기서 끊어 줍니다.
        /// </summary>
        public void RecoverIfStalled()
        {
            if (!_swinging || Time.time - _swingStartTime < MaxSwingSeconds)
                return;

            HandleSwingEnd();
        }

        /// <summary>
        /// 스윙을 종료합니다. letCurrentSwingFinish가 true면 현재 스윙이 끝날 때까지 기다립니다.
        /// </summary>
        public void End(bool letCurrentSwingFinish = false)
        {
            if (_anim == null)
                return;

            _anim.OnAttackTrigger -= ResolveDamage;

            if (letCurrentSwingFinish && _swinging && !_swingEnded)
                return;

            _anim.OnAnimationEnd -= HandleSwingEnd;
            if (!_swingEnded && _attackParam != null)
                _anim.SetParameter(_attackParam, false);

            _swinging = false;
        }

        /// <summary>
        /// 스윙 애니메이션 종료 콜백입니다. 쿨다운을 갱신하고 정리합니다.
        /// </summary>
        private void HandleSwingEnd()
        {
            _swingEnded = true;
            _lastAttackTime = Time.time;
            _swinging = false;
            if (_attackParam != null)
                _anim.SetParameter(_attackParam, false);

            SwingEnded?.Invoke();
            End();
        }

        /// <summary>
        /// 타격 시점에 데미지를 해결합니다(애니 트리거 콜백).
        /// </summary>
        private void ResolveDamage()
        {
            if (_target?.Transform == null || _target.IsDead || _unit == null || _unit.IsDead || _config == null)
                return; // 죽는 중인 대상은 아직 Transform이 살아 있다. 한 대 더 때리면 사망 처리가 두 번 돈다

            AttackResolver.Resolve(_unit.Owner, _unit.transform, _target, _firePosition,
                _config, _unit.DamageMultiplier, _meleeOffset);
            DamageDealt?.Invoke();
        }
    }
}
