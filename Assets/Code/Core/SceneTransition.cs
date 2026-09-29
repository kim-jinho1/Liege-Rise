using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Core
{
    /// <summary>
    /// 씬 전환 시 화면을 검게 페이드 아웃 → 로드 → 페이드 인 하는 영속 오버레이입니다.
    /// 게임 시작 시 Resources의 프리팹으로 1회 자동 생성되며 DontDestroyOnLoad로 유지됩니다.
    /// </summary>
    public class SceneTransition : MonoBehaviour
    {
        private const string PrefabPath = "SceneTransition";

        /// <summary>
        /// 현재 영속 전환 오버레이 인스턴스입니다. 없으면 null입니다.
        /// </summary>
        public static SceneTransition Instance { get; private set; }

        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private float fadeDuration = 0.4f;

        private bool _isTransitioning;

        /// <summary>
        /// 첫 씬 로드 전에 Resources에서 전환 오버레이를 1회 생성합니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
                return;

            SceneTransition prefab = Resources.Load<SceneTransition>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[SceneTransition] Resources/{PrefabPath} 프리팹을 찾지 못했습니다. 전환 연출 없이 진행합니다.");
                return;
            }

            Instantiate(prefab);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (fadeGroup != null)
            {
                fadeGroup.alpha = 0f;
                fadeGroup.blocksRaycasts = false;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// 페이드 아웃 후 지정한 빌드 인덱스 씬을 로드합니다.
        /// fadeInAfter가 false면 검은 화면을 유지하며(이후 FadeIn 호출 필요),
        /// _Core처럼 로드 후 추가로 스테이지를 얹어야 하는 경우에 사용합니다.
        /// </summary>
        public async void LoadScene(int buildIndex, bool fadeInAfter = true)
        {
            if (fadeGroup == null || _isTransitioning)
            {
                _ = SceneManager.LoadSceneAsync(buildIndex);
                return;
            }

            _isTransitioning = true;
            fadeGroup.blocksRaycasts = true;

            try
            {
                await Fade(1f);

                AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex);
                while (op is { isDone: false })
                    await Awaitable.NextFrameAsync();

                if (fadeInAfter)
                {
                    await Fade(0f);
                    fadeGroup.blocksRaycasts = false;
                }
            }
            finally
            {
                // 도중에 무엇이 터지든 깃발은 반드시 내린다. 여기 걸리면 다음 전환이 페이드 없이
                // 곧바로 로드되는 경로로 새고, 그 뒤로는 화면이 검은 채로 굳는다
                _isTransitioning = false;
            }
        }

        /// <summary>
        /// 검은 화면에서 다시 페이드 인 합니다. LoadScene(fadeInAfter:false) 이후
        /// 추가 로드(스테이지 Additive)가 끝난 시점에 호출합니다.
        /// </summary>
        public async void FadeIn()
        {
            if (fadeGroup == null)
                return;

            await Fade(0f);
            fadeGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// 현재 알파에서 목표 알파까지 unscaled 시간 기준으로 페이드합니다(일시정지 중에도 동작).
        /// </summary>
        private async Awaitable Fade(float target)
        {
            float start = fadeGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
                await Awaitable.NextFrameAsync();
            }

            fadeGroup.alpha = target;
        }
    }
}
