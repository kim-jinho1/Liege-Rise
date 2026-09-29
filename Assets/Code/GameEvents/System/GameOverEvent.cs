using Code.Core.Bus;

namespace Code.GameEvents.System
{
    /// <summary>
    /// 게임이 종료되었을 때 발생하는 이벤트입니다. 승자 정보를 포함합니다.
    /// </summary>
    public struct GameOverEvent : IEvent
    {
        /// <summary>
        /// 게임 승자 Owner입니다.
        /// </summary>
        public Owner WinnerOwner { get; private set; }

        public GameOverEvent(Owner winnerOwner)
        {
            WinnerOwner = winnerOwner;
        }
    }
}
