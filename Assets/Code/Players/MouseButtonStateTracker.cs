using UnityEngine.InputSystem;

namespace Code.Players
{
    /// <summary>
    /// 마우스 좌클릭의 프레임 상태(None/Pressed/Held/Released)를 추적합니다.
    /// </summary>
    public sealed class MouseButtonStateTracker
    {
        /// <summary>
        /// 현재 프레임의 좌클릭 상태입니다.
        /// </summary>
        public ButtonState State { get; private set; } = ButtonState.None;

        /// <summary>
        /// 마우스 좌클릭 상태를 갱신합니다. Mouse.current가 없으면 None으로 둡니다.
        /// </summary>
        public void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                State = ButtonState.None;
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
                State = ButtonState.Pressed;
            else if (mouse.leftButton.isPressed && !mouse.leftButton.wasPressedThisFrame)
                State = ButtonState.Held;
            else if (mouse.leftButton.wasReleasedThisFrame)
                State = ButtonState.Released;
            else
                State = ButtonState.None;
        }
    }
}
