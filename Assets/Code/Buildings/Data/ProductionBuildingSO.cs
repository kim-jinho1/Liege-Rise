using UnityEngine;

namespace Code.Buildings.Data
{
    /// <summary>
    /// 유닛 생산건물 데이터 SO입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ProductionBuilding", menuName = "Buildings/ProductionBuilding")]
    public class ProductionBuildingSO : BuildingSO
    {
        public override object Clone()
        {
            return (ProductionBuildingSO)base.Clone();
        }
    }
}
