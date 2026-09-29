using UnityEngine;

namespace Code.Buildings.Data
{
    /// <summary>
    /// 자동 유닛 생산 건물 데이터 SO입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnerBuilding", menuName = "Buildings/SpawnerBuilding")]
    public class SpawnerBuildingSO : BuildingSO
    {
        [field: SerializeField] public float SpawnInterval { get; private set; }

        public override object Clone()
        {
            return (SpawnerBuildingSO)base.Clone();
        }
    }
}
