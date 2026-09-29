using System.Collections.Generic;
using UnityEngine;

namespace Code.Sequences.Guide
{
    /// <summary>
    /// 앵커를 한 번 풀어 둔 것입니다. 대상은 움직이므로 스크린 좌표는 매 프레임 다시 계산합니다.
    /// </summary>
    public struct ResolvedAnchor
    {
        private IGuideHighlightable _target;
        private RectTransform _rect;
        private Camera _canvasCamera;
        private Vector3 _worldPoint;
        private Vector2 _fixedScreen;
        private Vector2 _offset;
        private GuideAnchorSpace _space;

        public static ResolvedAnchor OfTarget(IGuideHighlightable target, Vector2 offset)
            => new() { _space = GuideAnchorSpace.World, _target = target, _offset = offset };

        public static ResolvedAnchor OfWorldPoint(Vector3 point)
            => new() { _space = GuideAnchorSpace.World, _worldPoint = point };

        public static ResolvedAnchor OfRect(RectTransform rect, Vector2 offset)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Canvas root = canvas != null ? canvas.rootCanvas : null;
            Camera camera = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
            return new ResolvedAnchor { _space = GuideAnchorSpace.UI, _rect = rect, _canvasCamera = camera, _offset = offset };
        }

        public static ResolvedAnchor OfScreen(Vector2 screen)
            => new() { _space = GuideAnchorSpace.Screen, _fixedScreen = screen };

        /// <summary>
        /// UI 앵커의 칸이 그 스크린 좌표를 덮고 있는지입니다. 다른 종류는 false.
        /// </summary>
        public bool ContainsScreenPoint(Vector2 screen)
            => _space == GuideAnchorSpace.UI && _rect != null && _rect.gameObject.activeInHierarchy
               && RectTransformUtility.RectangleContainsScreenPoint(_rect, screen, _canvasCamera);

        /// <summary>
        /// 지금 프레임의 스크린 좌표입니다. 대상이 죽었거나 칸이 닫혔으면 false.
        /// </summary>
        public bool TryGetScreenPoint(Camera worldCamera, out Vector2 screen)
        {
            screen = default;
            switch (_space)
            {
                case GuideAnchorSpace.World:
                    if (worldCamera == null)
                        return false;

                    Vector3 world = _worldPoint;
                    if (_target != null)
                    {
                        if ((_target is Object unityObject && unityObject == null) || !_target.IsGuideAlive)
                            return false;

                        world = _target.GuidePosition + (Vector3)_offset;
                    }

                    screen = worldCamera.WorldToScreenPoint(world);
                    return true;

                case GuideAnchorSpace.UI:
                    if (_rect == null || !_rect.gameObject.activeInHierarchy)
                        return false;

                    Vector3 center = _rect.TransformPoint(_rect.rect.center);
                    screen = RectTransformUtility.WorldToScreenPoint(_canvasCamera, center) + _offset;
                    return true;

                case GuideAnchorSpace.Screen:
                    screen = _fixedScreen;
                    return true;

                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// 앵커 정의를 실제 대상으로 풉니다. 월드 대상은 강조와 같은 규칙(HighlightTargetFinder)으로 찾습니다.
    /// </summary>
    public sealed class GuideAnchorResolver
    {
        private readonly UiTargetResolver _ui = new();
        private readonly List<IGuideHighlightable> _scratch = new(16);

        // 오프셋이 붙은 월드 앵커(선 끝점·빈 땅)는 스텝 동안 한 자리에 못 박는다.
        // 대상이 움직일 때마다 선이 따라 움직이면 어디에 그으라는 건지 알 수 없다
        private readonly Dictionary<GuideAnchor, Vector3> _frozen = new();

        /// <summary>
        /// 못 박아 둔 자리를 잊습니다. 스텝이 바뀔 때 부릅니다.
        /// </summary>
        public void ClearFrozen()
            => _frozen.Clear();

        public bool TryResolve(GuideAnchor anchor, out ResolvedAnchor resolved)
        {
            resolved = default;
            if (anchor == null)
                return false;

            switch (anchor.space)
            {
                case GuideAnchorSpace.World:
                    return TryResolveWorld(anchor, out resolved);

                case GuideAnchorSpace.WorldBoundsMin:
                case GuideAnchorSpace.WorldBoundsMax:
                    return TryResolveBounds(anchor, out resolved);

                case GuideAnchorSpace.UI:
                    if (!_ui.TryResolve(anchor.ui, out RectTransform rect, allowFallback: false))
                        return false;

                    resolved = ResolvedAnchor.OfRect(rect, anchor.offset);
                    return true;

                case GuideAnchorSpace.Screen:
                    resolved = ResolvedAnchor.OfScreen(new Vector2(anchor.offset.x * Screen.width, anchor.offset.y * Screen.height));
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 그 종류의 대상 중 하나라도 스크린 좌표에서 radius 안에 있는지입니다. 플레이어가 누른 자리를 판정할 때 씁니다.
        /// </summary>
        public bool IsNearAnyTarget(GameObjectHighlightType type, Vector2 screen, Camera worldCamera, float radius)
        {
            if (worldCamera == null)
                return false;

            HighlightTargetFinder.Collect(type, _scratch);
            bool near = false;
            foreach (IGuideHighlightable target in _scratch)
            {
                Vector2 point = worldCamera.WorldToScreenPoint(target.GuidePosition);
                if ((point - screen).magnitude > radius)
                    continue;

                near = true;
                break;
            }

            _scratch.Clear();
            return near;
        }

        private bool TryResolveWorld(GuideAnchor anchor, out ResolvedAnchor resolved)
        {
            resolved = default;
            if (_frozen.TryGetValue(anchor, out Vector3 frozen))
            {
                resolved = ResolvedAnchor.OfWorldPoint(frozen);
                return true;
            }

            if (GuidePointFinder.IsAuthoredPoint(anchor.world))
            {
                if (!GuidePointFinder.TryGetAuthoredPoint(anchor.world, out Vector3 spot))
                    return false;

                // 손으로 찍은 자리는 그대로 짚는다. 빈 땅 찾기는 움직이는 대상 옆을 짚을 때를 위한 것이라,
                // 여기서까지 돌리면 저자가 고른 건설 자리가 조용히 딴 데로 옮겨 간다
                resolved = ResolvedAnchor.OfWorldPoint(Freeze(anchor, spot + (Vector3)anchor.offset));
                return true;
            }

            HighlightTargetFinder.Collect(anchor.world, _scratch);
            IGuideHighlightable target = HighlightTargetFinder.Nearest(_scratch, Reference());
            _scratch.Clear();
            if (target == null)
                return false;

            // 대상 자체를 짚는 앵커는 대상을 따라간다(일꾼이 걸어가도 손이 붙어 있어야 한다).
            // 대상 옆 자리를 짚는 앵커는 지금 자리에 못 박는다
            if (anchor.offset == Vector2.zero)
            {
                resolved = ResolvedAnchor.OfTarget(target, Vector2.zero);
                return true;
            }

            resolved = ResolvedAnchor.OfWorldPoint(Freeze(anchor, Clear(anchor, target.GuidePosition + (Vector3)anchor.offset)));
            return true;
        }

        // 고정 오프셋 자리에 나무·농장이 서 있을 수 있어 빈 칸으로 옮긴다
        private static Vector3 Clear(GuideAnchor anchor, Vector3 point)
            => anchor.clearRadius > 0f ? ClearSpotFinder.Find(point, anchor.clearRadius) : point;

        private Vector3 Freeze(GuideAnchor anchor, Vector3 point)
        {
            _frozen[anchor] = point;
            return point;
        }

        /// <summary>
        /// 종류 전체를 감싸는 사각형의 모서리입니다. 일꾼 셋을 한 상자에 담는 끌기를 보여 줄 때 씁니다.
        /// </summary>
        private bool TryResolveBounds(GuideAnchor anchor, out ResolvedAnchor resolved)
        {
            resolved = default;
            HighlightTargetFinder.Collect(anchor.world, _scratch);
            if (_scratch.Count == 0)
                return false;

            Vector3 min = _scratch[0].GuidePosition;
            Vector3 max = min;
            foreach (IGuideHighlightable target in _scratch)
            {
                min = Vector3.Min(min, target.GuidePosition);
                max = Vector3.Max(max, target.GuidePosition);
            }

            _scratch.Clear();
            Vector3 corner = anchor.space == GuideAnchorSpace.WorldBoundsMin ? min : max;
            resolved = ResolvedAnchor.OfWorldPoint(corner + (Vector3)anchor.offset);
            return true;
        }

        private static Vector3 Reference()
            => GuidePointFinder.TryGetPlayerCastle(out Vector3 castle) ? castle : Vector3.zero;
    }
}
