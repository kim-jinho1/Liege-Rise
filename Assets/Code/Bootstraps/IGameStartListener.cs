namespace Code.Bootstraps
{
    /// <summary>
    /// Bootstrap이 게임 시작 시점에 호출하는 초기화 진입점입니다.
    /// 모든 매니저의 Awake가 완료된 뒤(Start 시점), InitOrder 오름차순으로 OnGameStart가 호출됩니다.
    /// </summary>
    public interface IGameStartListener
    {
        /// <summary>
        /// 호출 우선순위입니다. 낮을수록 먼저 실행됩니다.
        /// </summary>
        int InitOrder { get; }

        /// <summary>
        /// 게임 시작 시 1회 호출되는 초기화 로직입니다.
        /// </summary>
        void OnGameStart();
    }
}
