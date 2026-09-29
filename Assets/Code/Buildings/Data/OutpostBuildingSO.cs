using UnityEngine;

namespace Code.Buildings.Data
{
    /// <summary>
    /// 전초 건물 데이터입니다. 사각 범위 내 아군 분대를 폰을 소모해 자동 보충합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Outpost", menuName = "LnR/Buildings/Outpost", order = 21)]
    public class OutpostBuildingSO : BuildingSO
    {
        // 보충 범위 사각형의 한 변(월드)
        [field: SerializeField] public float Range { get; private set; } = 12f;

        // 자동 보충 주기(초당 1명)
        [field: SerializeField] public float ReinforceInterval { get; private set; } = 3f;

        // 다른 전초와의 최소 건설 거리
        [field: SerializeField] public float MinOutpostDistance { get; private set; } = 10f;
    }
}
