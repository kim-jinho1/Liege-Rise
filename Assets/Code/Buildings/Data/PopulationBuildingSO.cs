using Code.Buildings.Interfaces;
using Code.Units.Pawns;
using UnityEngine;

namespace Code.Buildings.Data
{
    /// <summary>
    /// 인구를 공급하는 건물의 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PopulationBuilding", menuName = "Buildings/Population Building", order = 12)]
    public class PopulationBuildingSO : BuildingSO, IPawnCapacityProvider, IPawnSupplier
    {
        /// <summary>
        /// 이 건물이 공급하는 폰 데이터입니다.
        /// </summary>
        [field: SerializeField] public PawnSO PawnSO { get; private set; }

        /// <summary>
        /// 이 건물이 유지하는 폰 최대 보유 수입니다.
        /// </summary>
        [field: SerializeField] public int MaxPawns { get; private set; } = 10; // 이 건물이 올리는 폰 상한
        [field: SerializeField] public int InitialPawns { get; private set; } // 지어질 때 세워 두는 수. 기본 0 — 폰은 버튼으로 뽑는다

        /// <summary>
        /// 폰 소모 후 재생성까지 걸리는 시간(초).
        /// </summary>

        /// <summary>
        /// 이 건물이 유지하는 폰 수가 곧 공급하는 인구 한도입니다.
        /// </summary>
        public int PawnCapacity => MaxPawns;

        public override object Clone()
        {
            return (PopulationBuildingSO)base.Clone();
        }
    }
}
