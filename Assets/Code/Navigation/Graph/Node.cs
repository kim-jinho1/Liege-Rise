using System;
using UnityEngine;

namespace Code.Navigation.Graph
{
    public class Node : IComparable<Node>
    {
        public float G, F;

        public Vector3 WorldPosition;
        public Vector3Int CellPosition;
        public NodeData NodeData;

        public Node ParentNode;

        public int CompareTo(Node other)
        {
            if (Mathf.Approximately(other.F, F))
                return 0;

            return other.F < F ? -1 : 1;
        }

        public override bool Equals(object obj)
        {
            if (obj is Node node)
                return Equals(node);

            return false;
        }

        private bool Equals(Node node)
        {
            if (node is null)
                return false;

            return CellPosition == node.CellPosition;
        }

        public override int GetHashCode()
            => CellPosition.GetHashCode();

        // 여기서만은 is null이 맞다 — == 로 쓰면 이 연산자가 자기 자신을 다시 부른다
        public static bool operator ==(Node lhs, Node rhs)
        {
            if (lhs is not null)
                return lhs.Equals(rhs);

            return rhs is null;
        }

        public static bool operator !=(Node lhs, Node rhs)
        {
            return !(lhs == rhs);
        }
    }
}
