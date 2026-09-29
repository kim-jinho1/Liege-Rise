using System;
using Code.Buildings.Base;
using UnityEngine;

namespace Code.Buildings.Utils
{
    /// <summary>
    /// 건물 수명에 묶인 비동기 대기 확장 메서드 모음입니다.
    /// </summary>
    public static class BuildingAwaitExtensions
    {
        /// <summary>
        /// 한 프레임 대기합니다. BT 틱 내부에서 유닛을 파괴하지 않도록 지연할 때 사용합니다.
        /// 대기 중 건물이 파괴되면 false를 반환합니다.
        /// </summary>
        public static async Awaitable<bool> WaitOneFrameAlive(this AbstractBuilding building)
        {
            try
            {
                await Awaitable.NextFrameAsync(building.destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            return true;
        }
    }
}
