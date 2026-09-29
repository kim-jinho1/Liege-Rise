using Code.Core.Bus;
using Code.GameEvents.Sequence;
using Code.GameEvents.System;
using Code.Stages;
using UnityEngine;

namespace Code.Progression
{
    /// <summary>
    /// 게임 씬에 배치되어 스테이지 승리를 감지하고 진척에 클리어를 기록합니다.
    /// 전투 스테이지는 GameOverEvent(플레이어 승), 시퀀스 스테이지는 AllObjectivesCompletedEvent로 클리어를 판정합니다.
    /// </summary>
    public class StageProgressRecorder : MonoBehaviour
    {
        // 이 씬이 나타내는 스테이지
        [SerializeField] private StageSO thisStage;

        private bool _recorded;

        private void Awake()
        {
            Bus<GameOverEvent>.RegisterForAll(HandleGameOver);
            Bus<AllObjectivesCompletedEvent>.OnEvent[Owner.Player] += HandleSequenceCompleted;
        }

        private void OnDestroy()
        {
            Bus<GameOverEvent>.UnRegisterForAll(HandleGameOver);
            Bus<AllObjectivesCompletedEvent>.OnEvent[Owner.Player] -= HandleSequenceCompleted;
        }

        private void HandleGameOver(GameOverEvent evt)
        {
            if (evt.WinnerOwner == Owner.Player)
                Record();
        }

        private void HandleSequenceCompleted(AllObjectivesCompletedEvent evt)
            => Record();

        /// <summary>
        /// 이 스테이지의 클리어를 한 번만 기록합니다(중복 기록 방지).
        /// </summary>
        private void Record()
        {
            if (_recorded)
                return;

            // 부스판은 같은 씬을 다른 정의로 연다. 씬에 박힌 thisStage로 기록하면 진짜 판을 깬 것이 된다
            if (StageDatabaseSO.Current != null && StageDatabaseSO.Current != thisStage)
                return;

            _recorded = true;

            // Time.timeSinceLevelLoad는 additive 로드에서 안 돌아간다 — 판마다 다시 재는 쪽을 쓴다
            ProgressService.RecordStageClear(thisStage, RunStats.ElapsedSeconds);
        }
    }
}
