using Code.Core.Bus;
using UnityEngine;

namespace Code.GameEvents.System
{
    /// <summary>
    /// 맵 구역이 열려 카메라와 미니맵이 비출 범위가 넓어졌을 때 발생하는 이벤트입니다.
    /// </summary>
    public struct MapRegionOpenedEvent : IEvent
    {
        /// <summary>
        /// 지금 열려 있는 범위의 최소 좌표입니다.
        /// </summary>
        public Vector2 Min { get; private set; }

        /// <summary>
        /// 지금 열려 있는 범위의 최대 좌표입니다.
        /// </summary>
        public Vector2 Max { get; private set; }

        /// <summary>
        /// 지금까지 열린 마지막 구역의 인덱스입니다.
        /// </summary>
        public int OpenedIndex { get; private set; }

        public MapRegionOpenedEvent(Vector2 min, Vector2 max, int openedIndex)
        {
            Min = min;
            Max = max;
            OpenedIndex = openedIndex;
        }
    }
}
