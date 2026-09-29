using System;
using System.Collections.Generic;

namespace Code.Core.Bus
{
    /// <summary>
    /// 여러 Bus 이벤트를 한꺼번에 관리하고 Dispose 시 자동으로 모든 구독을 해제하는 헬퍼 클래스입니다.
    /// </summary>
    public class BusSubscriptionScope : IDisposable
    {
        private readonly List<Action> _unsubscribers = new();

        /// <summary>
        /// 특정 소유자의 이벤트를 구독합니다.
        /// </summary>
        public void Subscribe<T>(Owner owner, Bus<T>.Event handler) where T : IEvent
        {
            Bus<T>.OnEvent[owner] += handler;
            _unsubscribers.Add(() => Bus<T>.OnEvent[owner] -= handler);
        }

        /// <summary>
        /// 모든 소유자에 대해 이벤트를 구독합니다.
        /// </summary>
        public void SubscribeAll<T>(Bus<T>.Event handler) where T : IEvent
        {
            Bus<T>.RegisterForAll(handler);
            _unsubscribers.Add(() => Bus<T>.UnRegisterForAll(handler));
        }

        public void Dispose()
        {
            foreach (Action unsub in _unsubscribers)
                unsub?.Invoke();

            _unsubscribers.Clear();
        }
    }
}
