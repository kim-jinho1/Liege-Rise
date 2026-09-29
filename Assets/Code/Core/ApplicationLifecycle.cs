using UnityEngine;

namespace Code.Core
{
    /// <summary>
    /// 애플리케이션이 종료 중인지 알려줍니다.
    /// </summary>
    // 종료 순서는 정해져 있지 않아, OnDestroy에서 다른 객체를 건드리면 이미 헐린 것을 만진다
    public static class ApplicationLifecycle
    {
        public static bool IsQuitting { get; private set; }

        // 도메인 리로드를 끄면 정적 상태가 다음 플레이까지 살아남아 종료 플래그가 켜진 채로 시작한다
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            IsQuitting = false;

            // 같은 이유로 구독도 쌓이므로 먼저 뗀다
            Application.quitting -= HandleQuitting;
            Application.quitting += HandleQuitting;
        }

        private static void HandleQuitting() => IsQuitting = true;
    }
}
