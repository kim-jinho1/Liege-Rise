using Code.Navigation;
using UnityEngine;

namespace Code.Units.Util
{
    /// <summary>
    /// 경로가 있는데도 실제로 나아가지 못하는 상태가 이어지는지 지켜보는 감시자입니다.
    /// 사용 전에 반드시 Reset을 호출해야 합니다.
    /// </summary>
    public struct MoveStallWatchdog
    {
        /// <summary>
        /// 진척 없이 이만큼 지나면 정체로 봅니다.
        /// </summary>
        public const float DefaultTimeout = 3f;

        private const float ProgressEpsilon = 0.05f;

        private float _lastRemaining;
        private float _lastProgressTime;

        /// <summary>
        /// 판정 기준을 현재 시점으로 되돌립니다. 목적지를 새로 잡을 때마다 호출합니다.
        /// </summary>
        public void Reset()
        {
            _lastRemaining = float.MaxValue;
            _lastProgressTime = Time.time;
        }

        /// <summary>
        /// 남은 거리가 줄지 않은 채 제한 시간을 넘겼는지 판정합니다.
        /// </summary>
        public bool IsStuck(PathAgent agent, float timeout = DefaultTimeout)
        {
            // 계산 전이라 RemainingDistance가 아직 옛 값이다 — 대기열 지연을 정체로 세면 안 된다.
            if (agent == null || agent.PathPending)
                return false;

            if (agent.RemainingDistance < _lastRemaining - ProgressEpsilon)
            {
                _lastRemaining = agent.RemainingDistance;
                _lastProgressTime = Time.time;
                return false;
            }

            return Time.time - _lastProgressTime > timeout;
        }
    }
}
