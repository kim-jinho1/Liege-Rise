using Code.Economics;
using UnityEngine;

namespace Code.Buildings.Data
{
    /// <summary>
    /// 목장 건물 데이터입니다. 양이 자라고, 상주 폰이 그 양을 잡아 식량으로 바꿉니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Farm", menuName = "LnR/Buildings/Farm", order = 20)]
    public class FarmBuildingSO : BuildingSO
    {
        [field: Header("Farm")]
        [field: SerializeField] // 동시에 상주 가능한 최대 폰 수
        public int MaxPawns { get; private set; } = 3;

        [field: SerializeField] // 상주 폰 1기당 초당 도축 진행량
        public float FoodPerPawnPerSecond { get; private set; } = 0.4f;

        [field: SerializeField] // 생산할 식량 자원
        public ResourceSO FoodResource { get; private set; }

        [field: Header("양")]
        [field: SerializeField] // 울타리 안에 동시에 있을 수 있는 양의 수
        public int MaxSheep { get; private set; } = 3;

        [field: SerializeField] // 양 한 마리가 자라는 데 걸리는 시간(초)
        public float SheepGrowInterval { get; private set; } = 5f;

        [field: SerializeField] // 양 한 마리를 잡아 얻는 식량
        public int FoodPerSheep { get; private set; } = 8;
    }
}
