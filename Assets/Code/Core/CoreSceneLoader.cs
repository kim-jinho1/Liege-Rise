using System;
using Code.Bootstraps;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Core
{
    /// <summary>
    /// 게임 진입점입니다. 씬 로드 시점에 _Core를 로드하고,
    /// 스테이지를 Additive로 불러온 뒤 활성 씬으로 지정합니다.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class CoreSceneLoader : MonoBehaviour
    {
        [SerializeField] private Camera bootCamera; // 카메라가 없어서 렌더링을 못하는 일을 막기 위한 임시 카메라

        private static bool _loadedAsFallback;

        private void Awake()
        {
            if (_loadedAsFallback)
            {
                _loadedAsFallback = false;
                return;
            }

            Bootstrap.ResetSession();
        }

        private async void Start()
        {
            try
            {
                await LoadStageAsync(SceneLoader.CurrentStage);

                // 스테이지 Main Camera가 올라온 뒤 임시 부트 카메라를 끕니다.
                if (bootCamera != null)
                    bootCamera.enabled = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{this}] {e.Message}");
            }
        }

        /// <summary>
        /// 스테이지를 Additive로 로드(이미 로드돼 있으면 생략)하고, 활성 씬으로 지정한 뒤
        /// 게임 시작 리스너를 초기화하고 화면을 페이드 인 합니다.
        /// </summary>
        private static async Awaitable LoadStageAsync(SceneId stage)
        {
            int index = (int)stage;

            if (!IsSceneLoaded(index))
            {
                AsyncOperation op = SceneManager.LoadSceneAsync(index, LoadSceneMode.Additive);
                while (op is { isDone: false })
                    await Awaitable.NextFrameAsync();
            }

            Scene stageScene = SceneManager.GetSceneByBuildIndex(index);
            if (stageScene.IsValid())
                SceneManager.SetActiveScene(stageScene);

            Bootstrap.RunGameStart();

            if (SceneTransition.Instance != null)
                SceneTransition.Instance.FadeIn();
        }

        /// <summary>
        /// 해당 빌드 인덱스 씬이 현재 로드되어 있는지 검사합니다.
        /// </summary>
        private static bool IsSceneLoaded(int buildIndex)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).buildIndex == buildIndex)
                    return true;

            return false;
        }

        /// <summary>
        /// 개발 편의용 fallback. 에디터에서 스테이지를 단독 실행했을 때 _Core를 자동으로 끌어옵니다.
        /// 정상 초기화 순서가 보장되지 않으므로(매니저가 늦게 구독) 콘솔에 경고를 남깁니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCoreLoaded()
        {
            int active = SceneManager.GetActiveScene().buildIndex;

            if (!SceneIds.IsStage(active))
                return; // MainMenu 또는 _Core 로 시작한 정상 경로

            if (IsSceneLoaded((int)SceneId.Core))
                return;

            SceneLoader.SetCurrentStage((SceneId)active);
            _loadedAsFallback = true;
            SceneManager.LoadSceneAsync((int)SceneId.Core, LoadSceneMode.Additive);
        }
    }
}
