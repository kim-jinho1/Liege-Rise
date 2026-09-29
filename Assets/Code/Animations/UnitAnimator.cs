using System;
using UnityEngine;

namespace Code.Animations
{
    public class UnitAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField, Range(0f, 1f)] private float loopBeatPhase = 0.667f; // 클립에서 타격이 실제로 닿는 지점. 망치 3프레임 중 세 번째가 0.667이다

        public event Action OnAnimationEnd;
        public event Action OnAttackTrigger;

        private int _lastLoopState;
        private int _lastBeatIndex = -1;

        private void Awake()
        {
            Debug.Assert(animator != null, $"Animator is not assigned in {gameObject.name}");
        }

        /// <summary>
        /// 재생 중인 루프 클립이 타격 지점을 지났으면 true를 한 번 반환합니다.
        /// 클립에 애니메이션 이벤트가 없어 재생 위상으로 시점을 잡습니다.
        /// </summary>
        public bool ConsumeLoopBeat()
            => ConsumeLoopBeat(loopBeatPhase);

        /// <summary>
        /// 타격 지점을 클립마다 달리 줄 때 씁니다 — 망치는 3프레임 중 셋째, 도끼·곡괭이는 6프레임 클립이라 다릅니다.
        /// </summary>
        public bool ConsumeLoopBeat(float phase)
        {
            if (animator == null || !animator.isActiveAndEnabled)
                return false;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            // 타격은 phase, 1+phase, ... 에 있다. 몇 번째를 지났는지로 센다
            int beat = Mathf.FloorToInt(state.normalizedTime - phase) + 1;

            // 상태가 바뀌면 위상이 처음부터 다시 흐른다. 기준을 새로 잡고 그 프레임은 넘긴다.
            if (state.shortNameHash != _lastLoopState)
            {
                _lastLoopState = state.shortNameHash;
                _lastBeatIndex = beat;
                return false;
            }

            if (beat <= _lastBeatIndex)
                return false;

            _lastBeatIndex = beat;
            return true;
        }

        /// <summary>
        /// bool 타입 애니메이터 파라미터를 설정합니다.
        /// </summary>
        public void SetParameter(ParameterSO parameter, bool value)
            => animator.SetBool(parameter.HashValue, value);

        /// <summary>
        /// float 타입 애니메이터 파라미터를 설정합니다.
        /// </summary>
        public void SetParameter(ParameterSO parameter, float value)
            => animator.SetFloat(parameter.HashValue, value);

        /// <summary>
        /// int 타입 애니메이터 파라미터를 설정합니다.
        /// </summary>
        public void SetParameter(ParameterSO parameter, int value)
            => animator.SetInteger(parameter.HashValue, value);

        /// <summary>
        /// Trigger 타입 애니메이터 파라미터를 설정합니다.
        /// </summary>
        public void SetParameter(ParameterSO parameter)
            => animator.SetTrigger(parameter.HashValue);

        private void AnimationEnd()
            => OnAnimationEnd?.Invoke();

        private void AttackTrigger()
            => OnAttackTrigger?.Invoke();
    }
}
