using Code.Core.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.Players
{
    /// <summary>
    /// F1~F4로 카메라 시점(위치+줌)을 저장·호출하는 북마크 기능입니다.
    /// Ctrl+F로 현재 시점을 저장하고, F로 저장된 시점으로 즉시 이동합니다(세션 한정, 디스크 저장 없음).
    /// </summary>
    public class CameraBookmarkController : MonoBehaviour
    {
        [Header("Dependencies")] [SerializeField]
        private CameraController cameraController;

        [SerializeField] private InputReader inputReader;

        private const int SlotCount = 4;

        private static readonly Key[] FunctionKeys =
        {
            Key.F1, Key.F2, Key.F3, Key.F4,
        };

        private struct Bookmark
        {
            public bool HasValue;
            public Vector2 Position;
            public float Zoom;
        }

        private readonly Bookmark[] _bookmarks = new Bookmark[SlotCount];

        private void Update()
        {
            if (Keyboard.current == null || inputReader.IsTextInputActive)
                return;

            bool ctrl = Keyboard.current.ctrlKey.isPressed;

            for (int i = 0; i < SlotCount; i++)
            {
                if (!Keyboard.current[FunctionKeys[i]].wasPressedThisFrame)
                    continue;

                if (ctrl)
                    SaveBookmark(i);
                else
                    RecallBookmark(i);
            }
        }

        /// <summary>
        /// 현재 카메라 시점(위치+줌)을 지정 슬롯에 저장합니다.
        /// </summary>
        private void SaveBookmark(int slot)
        {
            if (cameraController == null)
                return;

            _bookmarks[slot] = new Bookmark
            {
                HasValue = true,
                Position = cameraController.FocusPosition,
                Zoom = cameraController.ZoomSize,
            };
        }

        /// <summary>
        /// 저장된 시점으로 카메라를 즉시 이동합니다. 슬롯이 비어 있으면 무시합니다.
        /// </summary>
        private void RecallBookmark(int slot)
        {
            if (cameraController == null || !_bookmarks[slot].HasValue)
                return;

            cameraController.JumpTo(_bookmarks[slot].Position);
            cameraController.ZoomSize = _bookmarks[slot].Zoom;
        }
    }
}
