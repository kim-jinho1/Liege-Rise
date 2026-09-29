using Code.Core.Bus;
using UnityEngine;

namespace Code.Buildings.Interfaces
{
    /// <summary>
    /// 분대를 보충해 줄 수 있는 거점입니다. 전초와 본부 계열 건물이 구현합니다.
    /// </summary>
    public interface IReinforcePoint
    {
        /// <summary>
        /// 이 거점의 소유자입니다. 같은 편 분대만 보충합니다.
        /// </summary>
        Owner Owner { get; }

        /// <summary>
        /// 분대원 1기를 보충하는 데 걸리는 시간(초)입니다.
        /// </summary>
        float ReinforceInterval { get; }

        /// <summary>
        /// 보충 범위 사각형의 한 변 길이입니다. 0이면 보충 거점이 아닙니다.
        /// </summary>
        float ReinforceSize { get; }

        /// <summary>
        /// 지정 위치가 이 거점의 보충 범위 안에 있는지 확인합니다.
        /// </summary>
        bool Covers(Vector3 position);
    }
}
