using Code.Core.Bus;
using Code.Core.Input;
using Code.Military.Commanders;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.Players
{
    /// <summary>
    /// 숫자키로 지휘관을 고릅니다. 숫자는 깃발 번호(Commander.Ordinal)와 같습니다 —
    /// 화면 오른쪽 초상 줄에 적힌 그 번호입니다.
    /// 부대 지정(Control Group)은 없앴습니다. 병력이 전부 지휘관에게 배속돼 움직이므로 숫자키를
    /// 둘이 나눠 쓸 이유가 없습니다.
    /// </summary>
    public class CommanderSelectHotkeys : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private SelectionController selectionController;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private InputReader inputReader;

        [Header("Settings")]
        [SerializeField] private float doubleTapThreshold = 0.3f; // 이 안에 같은 숫자를 다시 누르면 그 지휘관에게 카메라가 간다

        // 깃발 번호는 1부터다. 지휘관이 아홉을 넘길 일은 없다
        private static readonly Key[] DigitKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        };

        private int _lastOrdinal = -1;
        private float _lastTapTime = -1f;

        private void Update()
        {
            if (Keyboard.current == null || inputReader == null || inputReader.IsTextInputActive)
                return;

            for (int i = 0; i < DigitKeys.Length; i++)
            {
                if (Keyboard.current[DigitKeys[i]].wasPressedThisFrame)
                    HandleOrdinalKey(i + 1);
            }
        }

        /// <summary>
        /// 그 번호의 지휘관을 고릅니다. 없는 번호면 아무 일도 하지 않습니다 —
        /// 고른 것을 지우면 죽은 지휘관의 번호를 눌렀을 때 선택이 통째로 날아갑니다.
        /// </summary>
        private void HandleOrdinalKey(int ordinal)
        {
            Commander commander = Find(ordinal);
            if (commander == null || selectionController == null)
                return;

            if (!Keyboard.current.shiftKey.isPressed)
                selectionController.DeselectAllUnits();

            commander.Select();

            bool doubleTap = _lastOrdinal == ordinal
                             && Time.unscaledTime - _lastTapTime <= doubleTapThreshold;

            if (doubleTap && cameraController != null)
                cameraController.JumpTo(commander.transform.position);

            _lastOrdinal = ordinal;
            _lastTapTime = Time.unscaledTime;
        }

        /// <summary>
        /// 이 진영에서 그 번호를 달고 살아 있는 지휘관입니다.
        /// </summary>
        private Commander Find(int ordinal)
        {
            Owner controlOwner = selectionController != null ? selectionController.ControlOwner : Owner.Player;

            foreach (Commander commander in Commander.AliveCommanders)
            {
                if (commander != null && !commander.IsDead
                    && commander.Owner == controlOwner && commander.Ordinal == ordinal)
                    return commander;
            }

            return null;
        }
    }
}
