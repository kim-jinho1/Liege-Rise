using Code.Core.Bus;
using UnityEngine;

namespace Code.GameEvents.Enemy
{
    /// <summary>
    /// 주둔지 하나가 병력을 내보냈음을 알리는 이벤트입니다. 명령 자체는 주둔지가 직접 내립니다.
    /// </summary>
    public struct EnemyDispatchEvent : IEvent
    {
        /// <summary>
        /// 출격이 시작된 주둔지 위치입니다.
        /// </summary>
        public Vector3 Origin { get; private set; }

        /// <summary>
        /// 이번에 출격한 병력 수입니다.
        /// </summary>
        public int UnitCount { get; private set; }

        public EnemyDispatchEvent(Vector3 origin, int unitCount)
        {
            Origin = origin;
            UnitCount = unitCount;
        }
    }
}
