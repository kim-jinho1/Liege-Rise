using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Core.Bus
{
    /// <summary>
    /// Bus Event의 구독을 관리합니다. 씬 로드 시점에 모든 구독을 초기화합니다.
    /// </summary>
    public static class BusRegistry
    {
        private static readonly List<Action> Clears = new();

        /// <summary>
        /// Bus Event의 Clear 액션을 등록합니다. 닫힌 제네릭 타입마다 1회 호출됩니다.
        /// </summary>
        public static void Register(Action clear)
        {
            if (clear != null)
                Clears.Add(clear);
        }

        /// <summary>
        /// 등록된 모든 Bus Event의 구독을 초기화합니다. 세션(스테이지) 시작 시 호출합니다.
        /// </summary>
        public static void ClearAll()
        {
            foreach (Action clear in Clears)
                clear();
        }

        /// <summary>
        /// 씬 로드 시점에 모든 Bus Event의 구독을 초기화합니다. (Play 모드 시작 시에도 호출됨)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart()
            => ClearAll();
    }
}
