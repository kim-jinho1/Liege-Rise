using Code.Buildings.Data;
using Code.Buildings.Logic.EconomyBuilding;
using UnityEngine;

namespace Code.Commands.Types.Production
{
    /// <summary>
    /// 전초 전용 건설 커맨드입니다. 기본 배치 제약에 더해 기존 전초와의
    /// 최소 거리(OutpostBuildingSO.MinOutpostDistance)를 검사합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Construction Outpost", menuName = "Units/Commands/Construction Outpost", order = 1)]
    public class OutpostConstructionCommandSO : ConstructionBuildingCommandSO
    {
        public override bool AllRestrictionPass(Vector3 worldCenter, Vector2Int size)
        {
            if (!base.AllRestrictionPass(worldCenter, size))
                return false;

            if (BuildingSO is not OutpostBuildingSO outpostSO)
                return true;

            float minSqr = outpostSO.MinOutpostDistance * outpostSO.MinOutpostDistance;
            foreach (OutpostBuilding outpost in OutpostRegistry.OutpostBuildings)
            {
                if (outpost != null &&
                    (outpost.transform.position - worldCenter).sqrMagnitude < minSqr)
                    return false;
            }

            return true;
        }
    }
}
