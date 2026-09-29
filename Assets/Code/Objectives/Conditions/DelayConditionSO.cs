using System;
using System.Threading;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 켜진 뒤 지정 시간이 지나면 완료되는 조건입니다. HUD를 잠깐 비춰 주고 넘어가는 안내 스텝에 씁니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_Delay", menuName = "Objectives/Conditions/Delay")]
    public class DelayConditionSO : ObjectiveConditionSO
    {
        [field: SerializeField, Min(0f)] public float Seconds { get; private set; } = 4f;

        public override ConditionTracker CreateTracker()
            => new DelayTracker(Seconds);
    }

    public class DelayTracker : ConditionTracker
    {
        private readonly float _seconds;
        private CancellationTokenSource _waiting;

        public DelayTracker(float seconds)
        {
            _seconds = seconds;
        }

        public override void Subscribe()
        {
            Unsubscribe();
            _waiting = new CancellationTokenSource();
            WaitAsync(_waiting.Token);
        }

        public override void Unsubscribe()
        {
            _waiting?.Cancel();
            _waiting?.Dispose();
            _waiting = null;
        }

        private async void WaitAsync(CancellationToken token)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(_seconds, token);
                Complete();
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
