using System;
using Code.Economics;
using Code.Military.Data;
using UnityEngine;

namespace Code.Buildings.Data
{
    /// <summary>
    /// 분대 생산 옵션 — 생산 가능한 분대 정의와 그 식량 비용의 짝입니다.
    /// </summary>
    [Serializable]
    public class SquadProductionOption
    {
        [field: SerializeField] public SquadSO Squad { get; private set; }
        [field: SerializeField] public ResourceCostSO Cost { get; private set; }
    }

    /// <summary>
    /// 폰과 식량을 소모해 분대를 생산하는 건물(병영)의 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SquadRecruitmentBuilding", menuName = "Buildings/Squad Recruitment Building", order = 13)]
    public class SquadRecruitmentBuildingSO : BuildingSO
    {
        /// <summary>
        /// 이 건물에서 생산 가능한 분대 목록과 분대별 비용입니다. null이면 생산 불가입니다.
        /// </summary>
        [field: SerializeField] public SquadProductionOption[] Options { get; private set; }

        /// <summary>
        /// 이 건물에서 생산 완료 시 분대가 집결하는 지점의 SpawnPoint 기준 오프셋입니다. (SpawnPoint 기준 오프셋)
        /// </summary>
        [field: SerializeField] public Vector2 RallyOffset { get; private set; } = new(0f, -1.5f);

        /// <summary>
        /// 이 병영이 늘려주는 최대 분대 수입니다.
        /// </summary>
        [field: SerializeField] public int MaxSquads { get; private set; } = 2;

        /// <summary>
        /// 지정 분대의 생산 옵션을 반환합니다. 생산 목록에 없으면 null.
        /// </summary>
        public SquadProductionOption FindOption(SquadSO squad)
        {
            if (Options == null || squad == null)
                return null;

            foreach (SquadProductionOption option in Options)
                if (option != null && option.Squad == squad)
                    return option;

            return null;
        }

        public override object Clone()
        {
            return (SquadRecruitmentBuildingSO)base.Clone();
        }
    }
}
