using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.Core.Input
{
    [CreateAssetMenu(fileName = "Input reader", menuName = "SO/InputReader", order = 5)]
    public class InputReader : ScriptableObject, Controls.IPlayerActions, Controls.IShortcutKeyActions, Controls.ISequenceActions
    {
        private Controls _controls;

        public Vector2 MousePosition { get; private set; }
        public Vector2 KeyboardMovement { get; private set; }
        public float ScrollDelta { get; private set; }

        /// <summary>
        /// 텍스트 입력 모드가 활성화되어 있는지 여부입니다.
        /// true인 동안에는 부대 지정·카메라 단축키 등 키 입력을 무시해야 합니다.
        /// </summary>
        public bool IsTextInputActive { get; private set; }

        public Action<bool> OnMouseLeftButton;
        public Action<bool> OnMouseRightButton;

        public Action OnBuildMenuButton; // 빌드 메뉴 단축키
        public Action OnEscapeKeyPressed; // EscapeKey키 입력
        public Action OnSkillKeyPressed; // 분대 지휘관 스킬 단축키 (Q)
        public Action OnPostureKeyPressed; // 분대 자세 순환 단축키 (E)

        public Action OnAdvancePressed;
        public Action OnMovePerformed; // 카메라 이동(WASD) 입력 — 튜토리얼 등에서 사용

        private const float DepthEpsilon = 0.0001f; // 거리가 같다고 볼 오차. 커서가 여러 콜라이더 안에 있을 때다

        // 겹친 콜라이더를 전부 받아 그중 하나를 고른다. List는 모자라면 스스로 늘어나 잘릴 일이 없다
        private readonly List<RaycastHit2D> _rayHits = new(32);

        private Camera _camera;

        // 에셋이라 에디터가 인스펙터에 띄우기만 해도 OnEnable이 돈다. 그 인스턴스는 도메인 리로드 때
        // OnDisable 없이 버려져 Controls의 Finalize가 "Disable을 안 불렀다"고 경고한다. 플레이 중에만 켠다
        private void OnEnable()
        {
            if (Application.isPlaying)
                Activate();
        }

        /// <summary>
        /// 플레이 진입 전에 이미 로드돼 있던 에셋은 OnEnable이 isPlaying 전에 돌아 건너뜁니다 — 여기서 켭니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ActivateLoadedReaders()
        {
            foreach (InputReader reader in Resources.FindObjectsOfTypeAll<InputReader>())
                reader.Activate();
        }

        private void Activate()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
                _controls.ShortcutKey.SetCallbacks(this);
                _controls.Sequence.SetCallbacks(this);
            }

            if(_camera == null)
                _camera = Camera.main;

            _controls.Enable();
        }

        private void OnDisable()
        {
            if (_controls == null)
                return;

            _controls.Disable();

            // 플레이를 끝낼 때 SO의 OnDisable은 이미 에디트 모드에서 불린다. Controls.Dispose()가
            // 안에서 Object.Destroy를 하므로 그대로 두면 "Destroy may not be called from edit mode"가
            // 매번 뜬다. 그때는 에디터가 곧 전부 정리하므로 참조만 놓는다 — 빌드에서는 늘 참이라 그대로 정리된다
            if (Application.isPlaying)
                _controls.Dispose();

            _controls = null;
        }

        /// <summary>
        /// 키보드 이동 입력을 처리합니다.
        /// </summary>
        public void OnMove(InputAction.CallbackContext context)
        {
            KeyboardMovement = context.ReadValue<Vector2>();
            if (KeyboardMovement.sqrMagnitude > 0.01f)
                OnMovePerformed?.Invoke();
        }

        /// <summary>
        /// 마우스 포인터 위치 입력을 처리합니다.
        /// </summary>
        public void OnAim(InputAction.CallbackContext context)
        {
            MousePosition = context.ReadValue<Vector2>();
        }

        /// <summary>
        /// 마우스 좌클릭 입력을 처리합니다.
        /// </summary>
        public void OnLeftButton(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnMouseLeftButton?.Invoke(true);
            else if (context.canceled)
                OnMouseLeftButton?.Invoke(false);
        }

        /// <summary>
        /// 마우스 우클릭 입력을 처리합니다.
        /// </summary>
        public void OnRightButton(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnMouseRightButton?.Invoke(true);
            else if (context.canceled)
                OnMouseRightButton?.Invoke(false);
        }

        /// <summary>
        /// 현재 스크롤 휠 델타 값을 반환하고 즉시 0으로 초기화합니다.
        /// 이 값은 한 프레임에 한 번만 소비되어야 합니다.
        /// </summary>
        public float ConsumeScrollDelta()
        {
            float val = ScrollDelta;
            ScrollDelta = 0f;
            return val;
        }

        /// <summary>스크롤 휠 입력을 처리합니다.</summary>
        public void OnScrollWheel(InputAction.CallbackContext context)
        {
            if (context.performed)
                ScrollDelta += context.ReadValue<Vector2>().y;
        }

        /// <summary>
        /// 마우스 포인터 아래에서 가리킨 것 하나를 돌려줍니다. 가장 가까운 것을 고릅니다.
        /// 단일 히트 오버로드는 겹친 것 중 무엇을 줄지 정해져 있지 않아, 분대처럼 다섯이 붙어 선 곳을
        /// 찍으면 가리킨 놈이 아니라 옆 놈이 잡혔습니다 — 명령이 엉뚱한 대상에게 갔습니다.
        /// 커서가 여러 콜라이더 안에 동시에 들어가면 화면 앞쪽(Y가 작은 쪽)을 고릅니다.
        /// </summary>
        public bool GetMousePosition(out RaycastHit2D hit, LayerMask targetLayerMask)
        {
            hit = default;
            // 꺼진 카메라도 null이 아니다. _Core의 부트 카메라를 물면 좌표가 통째로 어긋나고,
            // 이 SO는 플레이를 나가도 살아 있어 다음 판까지 그 카메라를 붙들고 간다
            if (_camera == null || !_camera.isActiveAndEnabled)
                _camera = Camera.main;

            if (_camera == null)
                return false;

            Vector2 screen = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(screen);
            Physics2D.GetRayIntersection(ray, _camera.farClipPlane, _rayHits, targetLayerMask);

            Vector2 point = _camera.ScreenToWorldPoint(screen);
            float bestDistance = float.MaxValue;
            float bestDepth = float.MaxValue;

            for (int i = 0; i < _rayHits.Count; i++)
            {
                Collider2D collider = _rayHits[i].collider;
                if (collider == null)
                    continue;

                // 커서가 콜라이더 안이면 0이다. 안에 든 것이 여럿이면 아래 깊이 비교가 가른다
                float distance = (collider.ClosestPoint(point) - point).sqrMagnitude;
                float depth = collider.bounds.center.y;

                if (distance > bestDistance + DepthEpsilon)
                    continue;

                if (distance > bestDistance - DepthEpsilon && depth >= bestDepth)
                    continue;

                bestDistance = distance;
                bestDepth = depth;
                hit = _rayHits[i];
            }

            return hit.collider != null;
        }

        /// <summary>
        /// 텍스트 입력 중 게임플레이 입력(이동/단축키)을 차단하거나 복원합니다.
        /// </summary>
        public void SetTextInputMode(bool active)
        {
            IsTextInputActive = active;

            if (_controls == null)
                return;

            if (active)
            {
                _controls.Player.Disable();
                _controls.ShortcutKey.Disable();
                KeyboardMovement = Vector2.zero;
                ScrollDelta = 0f;
            }
            else
            {
                _controls.Player.Enable();
                _controls.ShortcutKey.Enable();
            }
        }

        /// <summary>
        /// 빌드 메뉴 단축키 입력을 처리합니다.
        /// </summary>
        public void OnBuildMenu(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnBuildMenuButton?.Invoke();
        }

        /// <summary>
        /// ESC 키 입력을 처리합니다.
        /// </summary>
        public void OnEscapeKey(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnEscapeKeyPressed?.Invoke();
        }

        /// <summary>
        /// 분대 스킬 단축키(Q) 입력을 처리합니다.
        /// </summary>
        public void OnSkillKey(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnSkillKeyPressed?.Invoke();
        }

        /// <summary>
        /// 분대 자세 순환 단축키(E) 입력을 처리합니다.
        /// </summary>
        public void OnPostureKey(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnPostureKeyPressed?.Invoke();
        }

        public void OnAdvance(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnAdvancePressed?.Invoke();
        }
    }
}
