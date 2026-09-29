using UnityEngine;

namespace Code.Units.Util
{
    public static class DistanceUtil
    {
        public static Vector3 GetClosestTargetPosition(GameObject self, GameObject target)
        {
            if (target.TryGetComponent(out Collider2D col2D))
                return col2D.ClosestPoint(self.transform.position);

            if (target.TryGetComponent(out Collider col3D))
                return col3D.ClosestPoint(self.transform.position);

            return target.transform.position;
        }
    }
}
