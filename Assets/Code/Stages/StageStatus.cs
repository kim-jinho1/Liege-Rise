using Code.Progression;

namespace Code.Stages
{
    /// <summary>
    /// 영지 하나의 진행 상태입니다.
    /// </summary>
    public enum StageStatus
    {
        Locked,
        Open,
        Cleared
    }

    /// <summary>
    /// StageStatus 판정 헬퍼입니다.
    /// </summary>
    public static class StageStatuses
    {
        /// <summary>
        /// 수동 잠금(StageSO.IsLocked)과 선행 영지 클리어를 종합해 상태를 판정합니다.
        /// </summary>
        public static StageStatus Of(StageSO stage)
        {
            if (stage == null || stage.IsLocked || !ProgressService.IsStageUnlocked(stage))
                return StageStatus.Locked;

            return ProgressService.IsStageCleared(stage.Scene) ? StageStatus.Cleared : StageStatus.Open;
        }
    }
}
