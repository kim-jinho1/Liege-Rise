using System;
using UnityEngine;

namespace Code.Stages
{
    /// <summary>
    /// 한 막에서 열리는 맵 사각형입니다. 구역은 누적되므로 뒤 구역이 앞 구역을 품습니다.
    /// </summary>
    [Serializable]
    public struct MapRegion
    {
        [field: SerializeField] public Vector2 Min { get; private set; }

        [field: SerializeField] public Vector2 Max { get; private set; }
    }
}
