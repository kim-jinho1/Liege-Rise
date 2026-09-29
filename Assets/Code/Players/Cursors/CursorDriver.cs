using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.Players.Cursors
{
    /// <summary>
    /// 커서가 움직인 프레임마다 판정하고, 결과가 바뀐 프레임에만 실제로 갈아 끼웁니다.
    /// </summary>
    public sealed class CursorDriver : MonoBehaviour
    {
        [SerializeField] private CursorSetSO cursorSet;

        private const CursorKind NotApplied = (CursorKind)(-1); // 어느 값과도 다른 상태로 시작해 첫 프레임에 반드시 한 번 적용되게 한다

        // 판정 한 번이 레이캐스트 세 번 + 부모 탐색이다. 커서가 멈춰 있어도 그 밑을 유닛이 지나갈 수 있어
        // 아주 멈추지는 않고, 움직이지 않는 프레임만 이 간격으로 건너뛴다(CommandableHoverProbe와 같은 방식)
        private const float IdleInterval = 0.05f;

        private CursorContextResolver _resolver;
        private CursorKind _applied = NotApplied;
        private Vector2 _lastPointer = new(float.NaN, float.NaN);
        private float _nextIdleResolve;

        public void Initialize(CursorContextResolver resolver)
        {
            _resolver = resolver;
        }

        private void Update()
        {
            if (cursorSet == null || _resolver == null)
                return;

            Mouse mouse = Mouse.current;
            Vector2 pointer = mouse != null ? mouse.position.ReadValue() : _lastPointer;
            if (pointer == _lastPointer && Time.unscaledTime < _nextIdleResolve)
                return;

            _lastPointer = pointer;
            _nextIdleResolve = Time.unscaledTime + IdleInterval;

            CursorKind kind = _resolver.Resolve();
            if (kind == _applied)
                return; // 같은 값을 매 프레임 다시 넘기면 커서가 재업로드되며 깜빡인다

            _applied = kind;
            Apply(kind);
        }

        private void OnDisable()
        {
            RestoreSystemCursor();
            _applied = NotApplied;
            _lastPointer = new Vector2(float.NaN, float.NaN); // 다시 켜지면 커서가 멈춰 있어도 한 번은 판정한다
        }

        private void Apply(CursorKind kind)
        {
            if (!cursorSet.TryGet(kind, out Texture2D texture, out Vector2 hotspot))
            {
                RestoreSystemCursor();
                return;
            }

            Cursor.SetCursor(texture, hotspot, CursorMode.ForceSoftware);
        }

        private static void RestoreSystemCursor()
            => Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware);
    }
}
