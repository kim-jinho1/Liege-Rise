using Code.Core.Bus;
using Code.FogSystem;

namespace Code.GameEvents.System
{
    /// <summary>
    /// 잔상(PlaceHolder)이 제거되었을 때 발행되는 이벤트입니다.
    /// </summary>
    public struct PlaceHolderDestroyEvent : IEvent
    {
        public PlaceHolder Placeholder { get; private set; }

        public PlaceHolderDestroyEvent(PlaceHolder placeholder)
        {
            Placeholder = placeholder;
        }
    }
}
