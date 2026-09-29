using Code.Stages;
using UnityEngine.SceneManagement;

namespace Code.Core
{
    /// <summary>
    /// 씬 전환을 담당하는 정적 헬퍼입니다.
    /// </summary>
    public static class SceneLoader
    {
        /// <summary>
        /// 현재(또는 다음에 로드할) 스테이지입니다. _Core의 CoreSceneLoader가 이 값을 읽어 Additive 로드합니다.
        /// </summary>
        public static SceneId CurrentStage { get; private set; } = SceneId.Stage1;

        /// <summary>
        /// 메뉴에서 고른 스테이지 정의입니다. 한 씬을 여러 정의가 나눠 쓸 때(부스용 짧은 판) 씬 번호만으로는 못 가립니다.
        /// </summary>
        public static StageSO CurrentStageAsset { get; private set; }

        /// <summary>
        /// 개발용 fallback(스테이지 단독 실행)에서 현재 스테이지를 설정합니다.
        /// </summary>
        public static void SetCurrentStage(SceneId stage)
            => CurrentStage = stage;

        /// <summary>
        /// 스테이지 정의로 로드합니다. 같은 씬을 쓰는 다른 정의와 구분하려면 이쪽을 씁니다.
        /// </summary>
        public static void Load(StageSO stage)
        {
            if (stage == null)
                return;

            CurrentStageAsset = stage;
            Load(stage.Scene);
        }

        /// <summary>
        /// SceneId로 씬을 로드합니다. 스테이지는 _Core를 거쳐, MainMenu는 직접 로드합니다.
        /// </summary>
        public static void Load(SceneId scene)
        {
            if (!IsInBuildSettings(scene))
                return;

            if (scene == SceneId.MainMenu)
            {
                LoadByIndex((int)scene, fadeInAfter: true);
                return;
            }

            if (CurrentStageAsset != null && CurrentStageAsset.Scene != scene)
                CurrentStageAsset = null;

            CurrentStage = scene;
            LoadCore();
        }

        /// <summary>
        /// 해당 씬이 Build Settings에 등록돼 있는지 검사합니다.
        /// 아직 만들지 않은 스테이지를 눌렀을 때 예외 대신 원인을 남기고 넘어갑니다.
        /// </summary>
        private static bool IsInBuildSettings(SceneId scene)
        {
            return (int)scene < SceneManager.sceneCountInBuildSettings;
        }

        /// <summary>
        /// 현재 스테이지를 _Core 경유로 다시 로드합니다(재시작용).
        /// </summary>
        public static void ReloadCurrent()
            => LoadCore();

        /// <summary>
        /// _Core 씬을 로드합니다. 페이드 인은 스테이지 Additive 로드 완료 후 CoreSceneLoader가 처리합니다.
        /// </summary>
        private static void LoadCore()
        {
            LoadByIndex((int)SceneId.Core, fadeInAfter: false);
        }

        /// <summary>
        /// 전환 오버레이가 있으면 페이드 연출을 거쳐 로드하고, 없으면 즉시 로드합니다.
        /// </summary>
        private static void LoadByIndex(int index, bool fadeInAfter)
        {
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.LoadScene(index, fadeInAfter);
            else
                SceneManager.LoadSceneAsync(index);
        }
    }
}
