using UnityEngine;

namespace Code.MapGeneration
{
    /// <summary>
    /// 자원이나 적 주둔지를 뿌릴 원형 구역입니다.
    /// 위치와 크기는 작성자가 씬에서 정하고, 무엇을 얼마나 놓을지는 Composition이 매 판 굴립니다.
    /// </summary>
    public class SpawnZoneAnchor : MapAnchor
    {
        // 이 구역의 랜덤 구성 규칙. 자원 구역이든 적 진지든 프리팹 목록만 다르다
        [field: SerializeField] public ZoneCompositionSO Composition { get; private set; }

        // 같은 이름표를 단 앵커끼리 후보로 묶이고 MapLayout의 ZoneGroup이 그중 몇 곳만 고른다.
        // 비워 두면 이 앵커는 매 판 위 Composition으로 그대로 채워진다
        [field: SerializeField] public string GroupKey { get; private set; }

        // 맵 반대편 짝을 가리키는 이름표. 같은 값을 단 앵커는 함께 뽑히고 함께 빠져 대칭이 유지된다.
        // 비워 두면 짝 없이 혼자 뽑힌다 — 맵 중앙처럼 반대편이 없는 자리에 쓴다
        [field: SerializeField] public string PairKey { get; private set; }

        protected override Color GizmoColor => Color.green;

        protected override string GizmoLabel => BuildLabel();

        private string BuildLabel()
        {
            if (string.IsNullOrWhiteSpace(GroupKey))
                return Composition != null ? $"Zone [{Composition.name}]" : "Zone [구성 미지정]";

            return string.IsNullOrWhiteSpace(PairKey)
                ? $"Zone 후보 [{GroupKey}]"
                : $"Zone 후보 [{GroupKey} · {PairKey}]";
        }
    }
}
