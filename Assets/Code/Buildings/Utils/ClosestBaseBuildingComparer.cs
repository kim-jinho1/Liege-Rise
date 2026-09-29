using System.Collections.Generic;
using Code.Buildings.Base;
using UnityEngine;

namespace Code.Buildings.Utils
{
    public struct ClosestBaseBuildingComparer : IComparer<AbstractBuilding>
    {
        private Vector3 _targetPosition;

        public ClosestBaseBuildingComparer(Vector3 position)
        {
            _targetPosition = position;
        }

        public int Compare(AbstractBuilding x, AbstractBuilding y)
        {
            //반환값이 <0 이면 현재값이 비교대상보다 작고, 0이면 같음. >0 이면 현재값이 비교대상보다 큼
            return (x.transform.position - _targetPosition).sqrMagnitude
                .CompareTo((y.transform.position - _targetPosition).sqrMagnitude);
        }
    }
}
