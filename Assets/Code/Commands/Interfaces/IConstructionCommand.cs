using UnityEngine;

namespace Code.Commands.Interfaces
{
    public interface IConstructionCommand
    {
        GameObject GhostPrefab { get; }

        /// <summary>
        /// 건물 크기 (타일 단위 X, Y)
        /// </summary>
        Vector2Int BuildingSize { get; }

        /// <summary>
        /// 격자 스냅된 중앙 좌표에서 모든 배치 제한 조건을 검사
        /// </summary>
        bool AllRestrictionPass(Vector3 worldCenter, Vector2Int size);
    }
}
