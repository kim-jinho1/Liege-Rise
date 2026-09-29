using Code.Core.Bus;

namespace Code.GameEvents.System
{
    /// <summary>
    /// 건설 패널이 열리거나 닫힐 때 발행되고 일시정지 등 다른 시스템이 패널 열림 상태에 반응하는 용도입니다.
    /// </summary>
    public struct BuildPanelToggledEvent : IEvent
    {
        public Owner Owner { get; private set; }
        public bool IsOpen { get; private set; }

        public BuildPanelToggledEvent(bool isOpen)
        {
            Owner = Owner.Player;
            IsOpen = isOpen;
        }
    }
}
