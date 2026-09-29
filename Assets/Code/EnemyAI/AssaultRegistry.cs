using System.Collections.Generic;
using Code.Core.Bus;
using Code.GameEvents.Enemy;
using UnityEngine;

namespace Code.EnemyAI
{
    /// <summary>
    /// 진행 중인 모든 공세 부대의 static 목록입니다. 미니맵 UI가 조회합니다.
    /// </summary>
    public static class AssaultRegistry
    {
        /// <summary>
        /// 현재 진행 중인 공세 부대 목록입니다.
        /// </summary>
        public static IReadOnlyList<AssaultForce> Forces => Active;

        /// <summary>
        /// 이번 판에서 공세를 물리친 횟수입니다. 뒤늦게 구독한 조건이 "이미 왔다 갔는지"를 이걸로 압니다.
        /// </summary>
        public static int ClearedCount { get; private set; }

        /// <summary>
        /// 현재 진행 중인 공세 부대 목록입니다. 내부용입니다.
        /// </summary>
        private static readonly List<AssaultForce> Active = new();

        /// <summary>
        /// 부대를 목록에 등록합니다.
        /// </summary>
        public static void Register(AssaultForce force)
        {
            if (Active.Contains(force))
                return;

            Active.Add(force);
        }

        /// <summary>
        /// 공세 부대를 등록 해제합니다. 마지막 하나가 빠지면 공세를 물리쳤다고 알립니다.
        /// </summary>
        public static void Unregister(AssaultForce force)
        {
            // 0으로 떨어지는 전이에서만 알린다. 없던 부대를 지웠거나 아직 남았으면 신호가 아니다
            if (!Active.Remove(force) || Active.Count > 0)
                return;

            ClearedCount++;
            Bus<AssaultsClearedEvent>.Raise(Owner.Enemy, new AssaultsClearedEvent());
        }

        /// <summary>
        /// 판이 새로 시작할 때 static 상태를 비웁니다. _Core가 판마다 다시 올라오므로 EnemyAIManager.Awake가 부릅니다 —
        /// 지난 판의 ClearedCount가 남으면 다음 판의 "공세를 기다리는" 조건이 시작하자마자 끝납니다.
        /// 여기서는 알리지 않습니다 — 판이 시작하는 시점이라 물리친 것이 아닙니다.
        /// </summary>
        public static void ResetRun()
        {
            Active.Clear();
            ClearedCount = 0;
        }

        // 에디터 도메인 리로드 비활성화 환경 대비: 플레이 시작마다 목록 초기화
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
            => ResetRun();
    }
}
