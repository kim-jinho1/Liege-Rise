using UnityEngine;

namespace Code.MapGeneration
{
    /// <summary>
    /// 절차적 배치 규칙 전체를 담는 데이터입니다.
    /// 실제 위치는 씬의 MapAnchor들이 들고 있고, 이 에셋은 "무엇을 놓을지"만 정합니다.
    /// 맵 프리셋을 교체하려면 이 에셋만 갈아끼웁니다.
    /// </summary>
    [CreateAssetMenu(fileName = "MapLayout", menuName = "SO/Map/MapLayout", order = 0)]
    public class MapLayoutSO : ScriptableObject
    {
        // 플레이어 기지 구성. 뽑힌 짝의 한쪽에 배치된다
        [field: SerializeField] public BaseCompositionSO PlayerBase { get; private set; }

        // 적 기지 구성. 플레이어의 반대편 슬롯에 배치된다
        [field: SerializeField] public BaseCompositionSO EnemyBase { get; private set; }

        // 구역 앵커를 후보로 묶어 그중 몇 곳만 골라 쓰는 규칙. 여기 없는 앵커는 매 판 그대로 채워진다
        [field: SerializeField] public ZoneGroupSO[] Groups { get; private set; } = System.Array.Empty<ZoneGroupSO>();

        // 종류와 무관하게 모든 배치물이 서로 유지할 최소 거리 (월드 단위)
        [field: SerializeField] public float GlobalMinSpacing { get; private set; } = 2f;

        // 한 항목을 놓을 때 훑어볼 후보 수의 상한. 구역의 후보 수보다 크게 두면 구역 전체를 훑는다
        [field: SerializeField] public int MaxAttemptsPerItem { get; private set; } = 512;
    }
}
