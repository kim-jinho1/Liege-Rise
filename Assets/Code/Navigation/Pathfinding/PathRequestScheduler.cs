using System.Collections.Generic;
using UnityEngine;

namespace Code.Navigation.Pathfinding
{
    /// <summary>
    /// A* 경로 요청을 프레임당 정해진 건수만 처리하는 전역 스케줄러입니다.
    /// PathfindingEngine은 메인 스레드에서 정적 버퍼를 잠근 채 끝까지 도는 탓에,
    /// 공세 개시처럼 수십 기가 한 프레임에 목적지를 받으면 그 프레임이 통째로 튑니다.
    /// 요청을 대기열에 쌓고 예산만큼만 흘려보내 그 몰림을 여러 프레임에 나눠 담습니다.
    /// </summary>
    public static class PathRequestScheduler
    {
        /// <summary>
        /// 한 프레임에 처리할 경로 요청의 최대 건수입니다.
        /// 낮출수록 프레임은 고르지만 명령과 실제 출발 사이의 지연이 길어집니다.
        /// </summary>
        public static int BudgetPerFrame { get; set; } = DefaultBudgetPerFrame;

        /// <summary>
        /// 아직 처리되지 않고 대기 중인 경로 요청의 수입니다.
        /// </summary>
        public static int PendingCount => Pending.Count;

        private static readonly Queue<PathAgent> Pending = new();

        private const int DefaultBudgetPerFrame = 8;

        /// <summary>
        /// 에이전트를 대기열에 올립니다. 목적지는 에이전트가 들고 있으므로
        /// 대기 중에 명령이 바뀌어도 마지막 목적지가 그대로 반영됩니다.
        /// </summary>
        public static void Enqueue(PathAgent agent)
        {
            if (agent != null)
                Pending.Enqueue(agent);
        }

        /// <summary>
        /// 예산만큼 대기열을 흘려보냅니다.
        /// 파괴됐거나 요청이 취소된 에이전트는 예산을 쓰지 않고 건너뜁니다.
        /// </summary>
        /// <summary>
        /// 한 프레임에 쓸 길찾기 시간 상한(ms)입니다. 건수 예산은 큰 맵에서 한 건이 무거우면 프레임을 통째로 먹습니다 —
        /// 분대 하나가 이동하면 분대원 수만큼 요청이 한 프레임에 쌓이므로 시간으로 자릅니다. 최소 한 건은 처리합니다.
        /// </summary>
        public static float TimeBudgetMs { get; set; } = 2f;

        private static readonly System.Diagnostics.Stopwatch Clock = new();

        internal static void Pump()
        {
            int served = 0;
            Clock.Restart();

            while (served < BudgetPerFrame && Pending.Count > 0)
            {
                if (served > 0 && Clock.Elapsed.TotalMilliseconds >= TimeBudgetMs)
                    break;

                PathAgent agent = Pending.Dequeue();
                if (agent == null || !agent.PathPending)
                    continue;

                agent.ResolvePendingPath();
                served++;
            }
        }

        /// <summary>
        /// 플레이 모드에 다시 들어갈 때 남은 대기열을 비웁니다(도메인 리로드를 끈 경우 대비).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
            => Pending.Clear();
    }
}
