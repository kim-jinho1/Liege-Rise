using System;
using System.Collections.Generic;

namespace Code.Core.Bus
{
    /// <summary>
    /// Owner 채널별 이벤트 브로드캐스트 버스입니다. 닫힌 제네릭 타입(Bus&lt;구체이벤트&gt;)마다
    /// 독립적인 static 구독 딕셔너리를 가집니다.
    /// 계약: 메인 스레드 전용 — Raise/구독/해제는 Unity 이벤트 루프에서만 호출합니다.
    /// 구독은 반드시 대응하는 시점에 해제해야 하며(OnDestroy 등), 세션 리셋 시 BusRegistry가 일괄 초기화합니다.
    /// </summary>
    public static class Bus<T> where T : IEvent
    {
        public delegate void Event(T args);

        // 최초 사용 시 세션 리셋 대상으로 등록합니다(닫힌 제네릭 타입마다 1회).
        static Bus() => BusRegistry.Register(Clear);

        public static Dictionary<Owner, Event> OnEvent = new Dictionary<Owner, Event>()
        {
            { Owner.Invalid , null},
            { Owner.Player , null},
            { Owner.Unowned , null},
            { Owner.Enemy , null}
        };

        // OnEvent[owner]를 한 번 읽어 호출하므로, 핸들러가 도중에 구독을 바꿔도(델리게이트 불변)
        // 현재 디스패치는 스냅샷된 목록으로 안전하게 진행됩니다.
        public static void Raise(Owner owner, T evt) => OnEvent[owner]?.Invoke(evt);

        public static void RegisterForAll(Event handler)
        {
            foreach (Owner owner in Enum.GetValues(typeof(Owner)))
                OnEvent[owner] += handler;
        }

        public static void UnRegisterForAll(Event handler)
        {
            foreach (Owner owner in Enum.GetValues(typeof(Owner)))
                OnEvent[owner] -= handler;
        }

        /// <summary>
        /// 모든 Owner 슬롯의 구독을 해제합니다. 세션 리셋 시 BusRegistry가 호출합니다.
        /// </summary>
        public static void Clear()
        {
            foreach (Owner owner in Enum.GetValues(typeof(Owner)))
                OnEvent[owner] = null;
        }
    }
}
