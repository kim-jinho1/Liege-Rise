using UnityEngine;

namespace Code.Navigation.Pathfinding
{
    /// <summary>
    /// PathRequestScheduler의 대기열을 매 프레임 끝에 한 번 흘려보내는 구동체입니다.
    /// 씬 배선을 요구하면 배선이 빠진 채 조용히 죽으므로 런타임에 스스로 생성됩니다.
    /// </summary>
    [DefaultExecutionOrder(1000)] // 그 프레임의 명령이 전부 쌓인 뒤에 흘려보낸다
    public class PathRequestPump : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            GameObject host = new(nameof(PathRequestPump))
            {
                hideFlags = HideFlags.HideInHierarchy
            };

            host.AddComponent<PathRequestPump>();
            DontDestroyOnLoad(host);
        }

        private void LateUpdate()
            => PathRequestScheduler.Pump();
    }
}
