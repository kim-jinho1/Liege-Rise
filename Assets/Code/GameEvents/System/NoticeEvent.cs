using Code.Core.Bus;
using UnityEngine;

namespace Code.GameEvents.System
{
    /// <summary>
    /// 배너 한 줄로 알릴 안내입니다. 무엇을 언제 알릴지는 발행하는 쪽이 정합니다.
    /// </summary>
    public struct NoticeEvent : IEvent
    {
        /// <summary>
        /// 배너에 띄울 문구입니다.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// 배너를 클릭했을 때 카메라가 갈 월드 좌표입니다.
        /// </summary>
        public Vector3 FocusPosition { get; private set; }

        /// <summary>
        /// 카메라가 갈 곳이 정해져 있는지 여부입니다.
        /// </summary>
        public bool HasFocus { get; private set; }

        public NoticeEvent(string message)
        {
            Message = message;
            FocusPosition = Vector3.zero;
            HasFocus = false;
        }

        public NoticeEvent(string message, Vector3 focusPosition)
        {
            Message = message;
            FocusPosition = focusPosition;
            HasFocus = true;
        }
    }
}
