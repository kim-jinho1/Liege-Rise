using Code.Units.Combat;
using UnityEngine;

namespace Code.Buildings.Data
{
    /// <summary>
    /// 자동 공격형 타워 건물 데이터 SO입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "AutoTowerBuilding", menuName = "Buildings/AutoTowerBuilding")]
    public class AutoTowerBuildingSO : BuildingSO
    {
        [field: SerializeField] public AttackConfigSO AttackConfig { get; private set; } // 공격 타워 공격 설정

        public override object Clone()
        {
            return (AutoTowerBuildingSO)base.Clone();
        }
    }
}
