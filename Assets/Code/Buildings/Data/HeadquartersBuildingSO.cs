using Code.Buildings.Interfaces;
using Code.Units.Pawns;
using UnityEngine;

namespace Code.Buildings.Data
{
    /// <summary>
    /// 본부 건물의 ScriptableObject입니다.
    /// 폰 인구 한도를 공급하며, PawnSO를 지정하면 폰 자체도 공급합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "HeadquartersBuilding", menuName = "Buildings/Headquarters Building", order = 10)]
    public class HeadquartersBuildingSO : BuildingSO, IPawnCapacityProvider, IPawnSupplier
    {
        /// <summary>
        /// 이 본부가 공급하는 폰 인구 한도
        /// </summary>
        [field: Header("Headquarters")]
        [field: SerializeField] public int PawnCapacity { get; private set; } = 3;
        [field: SerializeField] public int SquadCapacity { get; private set; } = 3; // 본부가 주는 분대 상한. 나머지는 지휘관이 준다

        /// <summary>
        /// 이 본부가 직접 공급하는 폰 데이터입니다. 비워 두면 폰을 공급하지 않습니다.
        /// </summary>
        [field: Header("Pawn Supply (비워 두면 공급 안 함)")]
        [field: SerializeField] public PawnSO PawnSO { get; private set; }

        /// <summary>
        /// 이 본부가 동시에 유지하는 폰 최대 보유 수입니다. PawnCapacity를 넘기지 않는 것이 좋습니다.
        /// </summary>
        [field: SerializeField] public int InitialPawns { get; private set; } = 3; // 판 시작에 세워 두는 수. 재생성은 없다

        /// <summary>
        /// 폰 사망 후 재생성까지 걸리는 시간(초)입니다.
        /// </summary>

        /// <summary>
        /// 분대 보충 범위 사각형의 한 변 길이(월드 단위)입니다. 0이면 보충 거점이 아닙니다.
        /// </summary>
        [field: Header("Reinforce")]
        [field: SerializeField] public float ReinforceRange { get; private set; } = 14f;

        /// <summary>
        /// 분대원 1기를 보충하는 데 걸리는 시간(초)입니다.
        /// </summary>
        [field: SerializeField] public float ReinforceInterval { get; private set; } = 3f;

        public override object Clone()
        {
            return (HeadquartersBuildingSO)base.Clone();
        }
    }
}
