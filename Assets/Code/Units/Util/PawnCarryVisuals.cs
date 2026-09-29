using Code.Animations;

namespace Code.Units.Util
{
    /// <summary>
    /// Pawn(일꾼)의 운반 상태 관련 시각적 처리 유틸리티 클래스입니다.
    /// </summary>
    public static class PawnCarryVisuals
    {
        /// <summary>
        /// 운반 상태를 설정합니다. carrying이 true이고 indexParam이 있으면 자원 인덱스도 갱신합니다.
        /// </summary>
        public static void SetCarrying(UnitAnimator animator, ParameterSO transportParam, ParameterSO indexParam,
            bool carrying, float carryIndex)
        {
            if (animator == null)
                return;

            if (transportParam != null)
                animator.SetParameter(transportParam, carrying ? 1f : 0f);

            if (carrying && indexParam != null)
                animator.SetParameter(indexParam, carryIndex);
        }

        /// <summary>
        /// 운반 상태를 해제합니다. (운반 플래그·자원 인덱스 모두 0)
        /// </summary>
        public static void Clear(UnitAnimator animator, ParameterSO transportParam, ParameterSO indexParam)
        {
            if (animator == null)
                return;

            if (transportParam != null)
                animator.SetParameter(transportParam, 0f);

            if (indexParam != null)
                animator.SetParameter(indexParam, 0f);
        }
    }
}
