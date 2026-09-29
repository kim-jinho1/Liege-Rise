using System;
using Code.Navigation.Baking;
using Code.Navigation.Graph;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Navigation.Pathfinding
{
    /// <summary>
    /// 베이크된 격자를 좌표로 묻는 순수 조회들입니다. 에이전트 상태를 보지 않으므로 어디서든 부를 수 있습니다.
    /// </summary>
    public static class NavSampler
    {
        /// <summary>
        /// 화면에 그릴 용도의 경로입니다. 길을 못 찾으면 두 점을 그대로 잇습니다 —
        /// 선이 사라지는 것보다 곧은 선이 낫습니다.
        /// </summary>
        public static Vector3[] VisualPath(Vector3 from, Vector3 to, BakedDataSO bakedData, Tilemap baseTilemap,
            float radius, int capacity)
        {
            Vector3[] buffer = new Vector3[capacity];
            int count = PathfindingEngine.CalculatePath(from, to, bakedData, baseTilemap, radius, buffer);

            if (count == 0)
                return new[] { from, to };

            Vector3[] result = new Vector3[count];
            Array.Copy(buffer, result, count);
            return result;
        }

        /// <summary>
        /// 좌표에서 가장 가까운 통행 가능 지점을 찾습니다. 그 칸이 이미 통행 가능하면 그대로 씁니다.
        /// </summary>
        public static bool SamplePosition(Vector3 worldPos, out Vector3 sampledPos, float maxRadius,
            BakedDataSO bakedData, Tilemap baseTilemap)
        {
            Vector3Int cell = baseTilemap.WorldToCell(worldPos);
            if (bakedData.TryGetNode(cell, out NodeData exactNode))
            {
                sampledPos = exactNode.worldPosition;
                return true;
            }

            int searchRange = Mathf.CeilToInt(maxRadius / Mathf.Max(baseTilemap.cellSize.x, 0.01f));
            float minDist = float.MaxValue;
            sampledPos = worldPos;
            bool found = false;

            for (int dx = -searchRange; dx <= searchRange; dx++)
            {
                for (int dy = -searchRange; dy <= searchRange; dy++)
                {
                    Vector3Int candidate = cell + new Vector3Int(dx, dy);
                    if (!bakedData.TryGetNode(candidate, out NodeData candidateNode))
                        continue;

                    float dist = Vector3.Distance(worldPos, candidateNode.worldPosition);
                    if (dist <= maxRadius && dist < minDist)
                    {
                        minDist = dist;
                        sampledPos = candidateNode.worldPosition;
                        found = true;
                    }
                }
            }

            return found;
        }
    }
}
