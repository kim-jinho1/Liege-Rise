using System.Collections.Generic;
using Code.Core;
using Code.Progression;
using UnityEngine;

namespace Code.Stages
{
    /// <summary>
    /// 게임 내 스테이지 목록을 정의하는 SO입니다. 스테이지 선택 UI에서 이 목록을 순회하며 버튼을 생성합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "StageDatabase", menuName = "Stages/Stage Database")]
    public class StageDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<StageSO> stages = new();

        // 읽는 쪽이 스테이지 씬 3곳과 _Core라 인스펙터 배선을 네 번 반복하지 않는다 — UITextSO와 같은 이유다
        private const string ResourcePath = "StageDatabase";

        private static StageDatabaseSO _cached;

        /// <summary>
        /// 등록된 스테이지 목록(정의된 진행 순서)입니다.
        /// </summary>
        public IReadOnlyList<StageSO> Stages => stages;

        /// <summary>
        /// 지금 진행 중인 스테이지의 정의입니다. 찾지 못하면 null입니다.
        /// </summary>
        public static StageSO Current
        {
            get
            {
                // 메뉴가 고른 정의가 지금 씬과 맞으면 그것이다 — 씬 번호로 찾으면 같은 씬을 쓰는 첫 정의가 잡힌다
                StageSO chosen = SceneLoader.CurrentStageAsset;
                return chosen != null && chosen.Scene == SceneLoader.CurrentStage
                    ? chosen
                    : Load().Find(SceneLoader.CurrentStage);
            }
        }

        /// <summary>
        /// 스테이지 목록을 읽습니다. 에셋이 없으면 빈 목록으로 돌고 한 번만 경고합니다.
        /// </summary>
        public static StageDatabaseSO Load()
        {
            if (_cached != null)
                return _cached;

            _cached = Resources.Load<StageDatabaseSO>(ResourcePath);
            if (_cached != null)
                return _cached;

            _cached = CreateInstance<StageDatabaseSO>();
            Debug.LogWarning($"Resources/{ResourcePath} 을 찾지 못해 스테이지 정의를 읽을 수 없습니다.");
            return _cached;
        }

        /// <summary>
        /// 해당 씬의 스테이지 정의를 찾습니다. 없으면 null입니다.
        /// </summary>
        public StageSO Find(SceneId scene)
        {
            foreach (StageSO stage in stages)
                if (stage != null && stage.Scene == scene)
                    return stage;

            return null;
        }

        /// <summary>
        /// 해금되었고 아직 클리어하지 않은 첫 스테이지를 목록 순서대로 반환합니다(이어하기용). 없으면 null.
        /// </summary>
        public StageSO GetNextPlayableStage()
        {
            foreach (StageSO stage in stages)
            {
                if (stage == null || stage.IsLocked)
                    continue;

                if (ProgressService.IsStageUnlocked(stage) && !ProgressService.IsStageCleared(stage.Scene))
                    return stage;
            }

            return null;
        }

        /// <summary>
        /// 목록에서 지정 스테이지 바로 다음에 오는 스테이지를 반환합니다. 없거나 수동 잠금이면 null.
        /// 진척 기록 시점과 무관하게 판정해야 하므로 클리어 여부를 보지 않습니다.
        /// </summary>
        public StageSO GetStageAfter(SceneId current)
        {
            for (int i = 0; i < stages.Count - 1; i++)
            {
                if (stages[i] == null || stages[i].Scene != current)
                    continue;

                StageSO next = stages[i + 1];
                return next != null && !next.IsLocked ? next : null;
            }

            return null;
        }
    }
}
