using UnityEngine;

namespace Code.Buildings.Data
{
    /// <summary>
    /// 유닛 탑승형 타워 건물 데이터 SO입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "MannedTowerBuilding", menuName = "Buildings/MannedTowerBuilding")]
    public class MannedTowerBuildingSO : BuildingSO
    {
        // 탑승 유닛의 감지 범위 보너스. 기본 DetectRange에 더한다
        [field: SerializeField] public float RangeBonus { get; private set; } = 3f;

        public override object Clone()
        {
            return (MannedTowerBuildingSO)base.Clone();
        }
    }
}
