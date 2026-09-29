using System;
using System.Threading;
using UnityEngine;

namespace Code.Core
{
    /// <summary>
    /// 지연 후 한 번 실행하는 취소 가능한 예약입니다. 다시 걸면 이전 예약은 버려집니다.
    /// 호버 툴팁처럼 "잠깐 기다렸다가, 그 사이 상황이 바뀌면 없던 일로" 하는 곳에 씁니다.
    /// </summary>
    public sealed class DelayedAction : IDisposable
    {
        private CancellationTokenSource _cts;

        /// <summary>
        /// 지연 후 실행할 동작을 겁니다. 걸려 있던 예약이 있으면 취소됩니다.
        /// </summary>
        public void Restart(float seconds, Action action)
        {
            Cancel();

            if (action == null)
                return;

            _cts = new CancellationTokenSource();
            RunAsync(seconds, action, _cts.Token);
        }

        /// <summary>
        /// 걸려 있는 예약을 취소합니다. 없으면 아무 일도 하지 않습니다.
        /// </summary>
        public void Cancel()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public void Dispose()
            => Cancel();

        private static async void RunAsync(float seconds, Action action, CancellationToken token)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(seconds, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // 대기 중에 오브젝트가 사라졌으면 토큰이 취소돼 있다
            if (!token.IsCancellationRequested)
                action();
        }
    }
}
