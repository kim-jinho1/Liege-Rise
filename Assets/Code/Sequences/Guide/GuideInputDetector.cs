using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Code.Sequences.Guide
{
    /// <summary>
    /// 플레이어가 안내 손이 보여 준 동작을 실제로 했는지 입력으로 판정합니다.
    /// 판정은 순서대로 하나씩 — 지금 차례인 제스처만 봅니다.
    /// </summary>
    public sealed class GuideInputDetector
    {
        private const float ClickSlop = 12f;      // 이 안에서 떼면 클릭, 넘으면 끌기
        private const float DragMin = 40f;
        private const float TargetRadius = 80f;   // 대상 중심에서 이만큼 안을 누르면 그 대상을 누른 것으로 본다
        private const float HoverRadius = 48f;

        private readonly GuideAnchorResolver _resolver;
        private Vector2 _leftDown;
        private Vector2 _rightDown;

        public GuideInputDetector(GuideAnchorResolver resolver)
            => _resolver = resolver;

        /// <summary>
        /// 매 프레임 부릅니다. 이번 프레임에 제스처가 완료됐거나, 게임 상태가 이미 그 결과인 상태면 true.
        /// </summary>
        public bool Check(GuideGesture gesture, GuideGesture next, Camera worldCamera)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || gesture == null)
                return false;

            if (IsAlreadySatisfied(gesture, next))
                return true;

            Vector2 position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
                _leftDown = position;
            if (mouse.rightButton.wasPressedThisFrame)
                _rightDown = position;

            switch (gesture.kind)
            {
                case GuideGestureKind.LeftClick:
                    return mouse.leftButton.wasReleasedThisFrame && IsClick(position, _leftDown)
                           && Hits(gesture.from, position, worldCamera);

                case GuideGestureKind.RightClick:
                    return mouse.rightButton.wasReleasedThisFrame && IsClick(position, _rightDown)
                           && Hits(gesture.from, position, worldCamera);

                case GuideGestureKind.LeftDrag:
                    return mouse.leftButton.wasReleasedThisFrame && (position - _leftDown).magnitude >= DragMin;

                case GuideGestureKind.RightDrag:
                    return mouse.rightButton.wasReleasedThisFrame && (position - _rightDown).magnitude >= DragMin;

                case GuideGestureKind.Key:
                    if (Keyboard.current == null)
                        return false;

                    if (gesture.key == "Arrows")
                        return Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame
                               || Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame;

                    if (gesture.key == "WASD")
                        return Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame
                               || Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame;

                    return TryParseKey(gesture.key, out Key key) && Keyboard.current[key].wasPressedThisFrame;

                case GuideGestureKind.Hover:
                    return _resolver.TryResolve(gesture.from, out ResolvedAnchor anchor)
                           && anchor.TryGetScreenPoint(worldCamera, out Vector2 target)
                           && (target - position).magnitude <= HoverRadius;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 누르라는 것이 이미 골라져 있거나(일꾼을 이미 고른 채 다음 스텝에 들어옴),
        /// 눌러야 할 칸이 사라지고 그다음 칸이 나타났으면(Q를 이미 눌러 건설 메뉴가 열림) 그 동작은 한 것으로 봅니다.
        /// </summary>
        private bool IsAlreadySatisfied(GuideGesture gesture, GuideGesture next)
        {
            if (gesture.kind != GuideGestureKind.LeftClick || gesture.from == null)
                return false;

            switch (gesture.from.space)
            {
                case GuideAnchorSpace.World:
                    return gesture.from.offset == Vector2.zero
                           && HighlightTargetFinder.IsAnySelected(gesture.from.world);

                case GuideAnchorSpace.UI:
                    return next != null && next.from != null && next.from.space == GuideAnchorSpace.UI
                           && !_resolver.TryResolve(gesture.from, out _)
                           && _resolver.TryResolve(next.from, out _);

                default:
                    return false;
            }
        }

        private static bool IsClick(Vector2 up, Vector2 down)
            => (up - down).magnitude <= ClickSlop;

        /// <summary>
        /// 누른 자리가 앵커가 가리키던 것인가입니다. 월드 대상은 같은 종류면 어느 것이든 인정합니다 —
        /// 손이 짚은 일꾼이 아니라 옆 일꾼을 눌러도 배운 것은 같습니다. 오프셋이 있으면 빈 땅이므로 자리를 묻지 않습니다.
        /// </summary>
        private bool Hits(GuideAnchor anchor, Vector2 position, Camera worldCamera)
        {
            if (anchor == null)
                return false;

            switch (anchor.space)
            {
                case GuideAnchorSpace.UI:
                    return _resolver.TryResolve(anchor, out ResolvedAnchor resolved)
                           && resolved.ContainsScreenPoint(position);

                case GuideAnchorSpace.World:
                    if (IsOverUI())
                        return false;

                    if (anchor.offset != Vector2.zero || GuidePointFinder.IsAuthoredPoint(anchor.world))
                        return true;

                    return _resolver.IsNearAnyTarget(anchor.world, position, worldCamera, TargetRadius);

                default:
                    return !IsOverUI();
            }
        }

        private static bool IsOverUI()
            => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        /// <summary>
        /// "Tab"·"Q"는 그대로, "Right"는 RightArrow로 읽습니다.
        /// </summary>
        private static bool TryParseKey(string name, out Key key)
        {
            key = Key.None;
            return !string.IsNullOrEmpty(name)
                   && (Enum.TryParse(name, true, out key) || Enum.TryParse(name + "Arrow", true, out key));
        }
    }
}
