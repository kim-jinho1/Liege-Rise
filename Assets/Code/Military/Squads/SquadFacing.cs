using UnityEngine;

namespace Code.Military.Squads
{
    /// <summary>
    /// 분대의 정면 방향을 관리하는 컴포넌트입니다. 정면 방향은 2D 평면에서의 회전 각도로 표현됩니다.
    /// </summary>
    public class SquadFacing : MonoBehaviour
    {
        /// <summary>
        /// 현재 정면 각도 (도, 0 = +Y).
        /// </summary>
        public float CurrentAngle { get; private set; }

        /// <summary>
        /// 목표 정면 각도 (도, 0 = +Y).
        /// </summary>
        public float DesiredAngle { get; private set; }

        /// <summary>
        /// 회전 속도 (도/초).
        /// </summary>
        public float RotationSpeed { get; set; } = 90f;

        /// <summary>
        /// 현재 정면 방향 벡터입니다.
        /// </summary>
        public Vector2 Direction
            => Quaternion.Euler(0f, 0f, CurrentAngle) * Vector3.up;

        private bool _rotationLocked;

        private void Update()
        {
            if (_rotationLocked)
                return;

            // 매 프레임마다 목표 각도로 회전하도록 CurrentAngle을 업데이트합니다.
            CurrentAngle = Mathf.MoveTowardsAngle(CurrentAngle, DesiredAngle, RotationSpeed * Time.deltaTime);
        }

        /// <summary>
        /// 분대가 향할 목표 방향을 지정합니다.
        /// </summary>
        public void SetDesired(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;

            DesiredAngle = Vector2.SignedAngle(Vector2.up, direction);
        }

        /// <summary>
        /// 현재 정면과 지정한 월드 방향 사이의 사잇각(0~180°)을 반환합니다.
        /// </summary>
        public float AngleTo(Vector2 worldDirection)
            => Vector2.Angle(Direction, worldDirection);

        /// <summary>
        /// 회전 잠금을 설정합니다 (타워 탑승). 잠금 중에는 목표 각도로 회전하지 않습니다.
        /// </summary>
        public void SetRotationLocked(bool locked)
            => _rotationLocked = locked;
    }
}
