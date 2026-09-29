using System.Collections.Generic;
using System.Linq;
using Code.Core.Bus;
using UnityEngine;

namespace Code.Bootstraps
{
    /// <summary>
    /// 게임 진입점입니다. 씬 내 모든 매니저의 Awake가 끝난 뒤(Start 시점),
    /// IGameStartListener를 구현한 매니저들을 InitOrder 순서대로 초기화합니다.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        private static readonly HashSet<IGameStartListener> Initialized = new();

        /// <summary>
        /// 모든 매니저의 Start(BGM·효과음 재생)보다 먼저 BroAudio를 초기화합니다.
        /// 수동 초기화 모드(BroAudio_InitManually)일 때만 호출이 필요합니다.
        /// </summary>
        private void Awake()
        {
#if BroAudio_InitManually
            Ami.BroAudio.BroAudio.Init();
#endif
        }

        private void Start() => RunGameStart();

        /// <summary>
        /// 로드된 모든 씬에서 IGameStartListener를 찾아 InitOrder 순으로 초기화합니다.
        /// 이미 초기화한 리스너는 건너뛰므로, _Core와 스테이지가 따로 올라와도 각자 1회만 실행됩니다.
        /// </summary>
        public static void RunGameStart()
        {
            IOrderedEnumerable<IGameStartListener> listeners = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude)
                .OfType<IGameStartListener>()
                .Where(listener => !Initialized.Contains(listener))
                .OrderBy(listener => listener.InitOrder);

            foreach (IGameStartListener listener in listeners)
            {
                Initialized.Add(listener);
                listener.OnGameStart();
            }
        }

        /// <summary>
        /// 게임 시작 상태를 초기화합니다.
        /// </summary>
        public static void ResetSession()
        {
            Initialized.Clear();
            BusRegistry.ClearAll();
        }

        /// <summary>
        /// 에디터에서 도메인 리로드를 끈 경우를 대비해 플레이 시작마다 목록을 비웁니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart()
            => Initialized.Clear();
    }
}
