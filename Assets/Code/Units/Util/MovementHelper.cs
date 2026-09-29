using Code.Animations;
using Code.Navigation;
using Code.Units.Base;

namespace Code.Units.Util
{
    /// <summary>
    /// 유닛(Unit) 이동 관련 유틸리티 클래스입니다.
    /// </summary>
    public static class MovementHelper
    {
        /// <summary>
        /// 이동 애니메이션 파라미터를 설정합니다. (UnitAnimator가 null이면 무시)
        /// </summary>
        public static void SetMoveParam(AbstractUnit unit, ParameterSO moveParam, bool value)
        {
            if (moveParam == null || unit == null || unit.UnitAnimator == null)
                return;

            unit.UnitAnimator.SetParameter(moveParam, value);
        }

        /// <summary>
        /// 이동 애니메이션 파라미터를 해제합니다. (UnitAnimator가 null이면 무시)
        /// </summary>
        public static bool HasArrived(PathAgent agent)
            => agent != null && !agent.PathPending && agent.RemainingDistance <= agent.StoppingDistance;
    }
}
