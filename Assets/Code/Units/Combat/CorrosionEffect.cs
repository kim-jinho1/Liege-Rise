using System;
using Code.Commandable.Interfaces;
using UnityEngine;

namespace Code.Units.Combat
{
    /// <summary>
    /// 부식 상태 이상 컴포넌트입니다. 피격 유닛에 AddComponent로 부착되어 일정 시간 동안 지속 데미지를 줍니다.
    /// 중복 적용 시 종료 시각이 연장됩니다.
    /// </summary>
    public class CorrosionEffect : MonoBehaviour
    {
        private IDamageable _target;
        private int _damagePerTick;
        private float _tickInterval;
        private float _endTime;
        private bool _isRunning;

        /// <summary>
        /// 부식 효과를 초기화하거나 갱신합니다.
        /// 이미 실행 중이면 종료 시각만 연장됩니다.
        /// </summary>
        public void Apply(IDamageable target, int damagePerTick, float tickInterval, float duration)
        {
            _target = target;
            _damagePerTick = damagePerTick;
            _tickInterval = tickInterval;
            _endTime = Time.time + duration;

            if (!_isRunning)
                RunAsync();
        }

        private async void RunAsync()
        {
            _isRunning = true;
            try
            {
                while (Time.time < _endTime && _target?.Transform != null)
                {
                    await Awaitable.WaitForSecondsAsync(_tickInterval, destroyCancellationToken);
                    if (_target?.Transform == null)
                        break;
                    _target.TakeDamage(_damagePerTick, _target.Transform.position, Vector3.zero);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isRunning = false;
                if (this != null)
                    Destroy(this);
            }
        }
    }
}
