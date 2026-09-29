using Code.Animations;
using UnityEngine;

namespace Code.Units.Util
{
    /// <summary>
    /// 8방향 공격 애니메이션의 방향 인덱스를 계산해 애니메이터 파라미터에 적용하는 유틸리티입니다.
    /// 좌/우 반전은 SpriteRenderer flipX가 담당하므로 인덱스는 정면 기준 5개만 사용합니다.
    /// </summary>
    public static class AttackDirectionUtil
    {
        /// <summary>
        /// 타겟을 향하는 월드 방향으로 공격 방향 인덱스를 계산해 파라미터에 설정합니다.
        /// </summary>
        public static void Apply(ParameterSO param, UnitAnimator animator, Vector3 worldDir)
        {
            if (param == null || animator == null)
                return;

            animator.SetParameter(param, ResolveIndex(worldDir));
        }

        /// <summary>
        /// 방향 벡터를 5개 수직 구획(0=Down, 1=Up, 2=Side, 3=UpSide, 4=DownSide)으로 분류합니다.
        /// 좌우 구분은 flipX가 담당하므로 x는 절댓값만 사용합니다.
        /// </summary>
        public static int ResolveIndex(Vector3 dir)
        {
            float angle = Mathf.Atan2(dir.y, Mathf.Abs(dir.x)) * Mathf.Rad2Deg;

            return angle switch
            {
                >= 67.5f => 1,
                <= -67.5f => 0,
                >= 22.5f => 3,
                <= -22.5f => 4,
                _ => 2
            };
        }
    }
}
