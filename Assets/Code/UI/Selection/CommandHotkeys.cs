using Code.UI.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.UI.Selection
{
    /// <summary>
    /// 커맨드 칸의 단축키 배정과 입력 확인을 맡습니다.
    /// Q/E가 이미 다른 바인딩에 잡혀 있어 액션 에셋 대신 키보드를 직접 읽습니다.
    /// </summary>
    public static class CommandHotkeys
    {
        // 4열 x 2행 그리드라 Slot 번호가 곧 자리다. 커맨드가 바뀌어도 자리가 같으면 키도 같다.
        // WASD는 카메라, 1~9는 지휘관이라 그 둘레에서 왼손이 안 움직이고 닿는 여덟 키다
        private static readonly Key[] SlotKeys =
        {
            Key.Q, Key.E, Key.R, Key.F,
            Key.Z, Key.X, Key.C, Key.V
        };

        private static readonly string[] SlotKeyLabels = { "Q", "E", "R", "F", "Z", "X", "C", "V" };

        /// <summary>
        /// Slot 번호에 배정된 단축키 라벨입니다. 범위를 벗어나면 null입니다.
        /// </summary>
        public static string LabelForSlot(int slot)
            => slot >= 0 && slot < SlotKeyLabels.Length ? SlotKeyLabels[slot] : null;

        /// <summary>
        /// 눌린 키에 해당하는 칸을 한 개만 실행합니다.
        /// </summary>
        public static void Poll(BuildUnitButtonUI[] slots)
        {
            if (slots == null || Keyboard.current == null)
                return;

            int count = Mathf.Min(slots.Length, SlotKeys.Length);
            for (int i = 0; i < count; i++)
            {
                if (slots[i] == null || !Keyboard.current[SlotKeys[i]].wasPressedThisFrame)
                    continue;

                slots[i].InvokeByHotkey();
                return;
            }
        }
    }
}
