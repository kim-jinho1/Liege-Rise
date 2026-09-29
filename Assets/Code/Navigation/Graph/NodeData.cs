using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Navigation.Graph
{
    [Serializable]
    public struct NodeData : IEquatable<NodeData>
    {
        public TopographyType topographyType;
        public Vector3 worldPosition;
        public Vector3Int cellPosition;
        public List<LinkData> neighbors;

        // 고도. 값이 클수록 높은 층이며, 층이 다른 셀은 램프를 통해서만 이어집니다.
        public int level;

        // 건설 가능 여부 (PathBaker가 Bake 시 계산)
        public bool canBuildGeneral; // 일반 건물 건설 가능 (Flat)

        public NodeData(Vector3 worldPosition, Vector3Int cellPosition, TopographyType topographyType,
            int level, bool canBuildGeneral)
        {
            this.topographyType = topographyType;
            this.worldPosition = worldPosition;
            this.cellPosition = cellPosition;
            neighbors = new List<LinkData>();
            this.level = level;
            this.canBuildGeneral = canBuildGeneral;
        }

        /// <summary>
        /// 이웃 노드와의 간선을 추가합니다.
        /// </summary>
        public void AddNeighbor(NodeData neighbor)
        {
            neighbors.Add(new LinkData
            {
                startPosition = worldPosition,
                startCellPosition = cellPosition,
                endPosition = neighbor.worldPosition,
                endCellPosition = neighbor.cellPosition,
                cost = Vector3Int.Distance(cellPosition, neighbor.cellPosition)
            });
        }

        public override int GetHashCode() => cellPosition.GetHashCode();

        public bool Equals(NodeData other) => cellPosition == other.cellPosition;

        public override bool Equals(object obj)
        {
            if (obj is NodeData data) return Equals(data);
            return false;
        }

        public static bool operator ==(NodeData lhs, NodeData rhs) => lhs.Equals(rhs);
        public static bool operator !=(NodeData lhs, NodeData rhs) => !(lhs == rhs);
    }
}
