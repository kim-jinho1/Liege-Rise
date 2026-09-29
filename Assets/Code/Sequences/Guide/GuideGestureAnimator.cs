using System;
using System.Threading;
using UnityEngine;

namespace Code.Sequences.Guide
{
    /// <summary>
    /// 제스처 하나를 안내 손으로 연기합니다. 플레이어가 그 동작을 해 버리면 Interrupt로 끊고 다음 것으로 넘어갑니다.
    /// </summary>
    public sealed class GuideGestureAnimator
    {
        private const float MoveSeconds = 0.6f;
        private const float DragSeconds = 0.9f;
        private const float PressSeconds = 0.18f;
        private const float HoldSeconds = 0.5f;
        private const float EdgeMargin = 24f;

        private readonly GuideCursorView _view;
        private readonly GuideAnchorResolver _resolver;
        private readonly Func<Camera> _worldCamera;

        public bool Interrupted { get; private set; }

        public GuideGestureAnimator(GuideCursorView view, GuideAnchorResolver resolver, Func<Camera> worldCamera)
        {
            _view = view;
            _resolver = resolver;
            _worldCamera = worldCamera;
        }

        public void Interrupt()
            => Interrupted = true;

        /// <summary>
        /// 끊긴 연기의 흔적(끌기 상자·키캡·눌린 버튼)을 지우고 다음 연기를 받을 준비를 합니다.
        /// </summary>
        public void Reset()
        {
            Interrupted = false;
            _view.HideDrag();
            _view.HideKey();
            _view.SetButton(GuideButton.None, false);
        }

        /// <summary>
        /// 제스처 하나입니다. 앵커를 못 찾거나 도중에 끊기면 false.
        /// </summary>
        public async Awaitable<bool> PlayAsync(GuideGesture gesture, CancellationToken token)
        {
            // 키는 앵커가 없다. 먼저 풀면 늘 실패해 키캡이 한 번도 안 뜬다
            if (gesture.kind == GuideGestureKind.Key)
                return await KeyAsync(gesture.key, token);

            if (!_resolver.TryResolve(gesture.from, out ResolvedAnchor from))
                return false;

            switch (gesture.kind)
            {
                case GuideGestureKind.Hover:
                    _view.SetButton(GuideButton.None, false);
                    return await MoveToAsync(from, MoveSeconds, GuideGestureKind.Hover, default, token)
                           && await WaitAsync(0.8f, token);

                case GuideGestureKind.LeftClick:
                case GuideGestureKind.RightClick:
                    return await ClickAsync(from, ButtonOf(gesture.kind), token);

                case GuideGestureKind.LeftDrag:
                case GuideGestureKind.RightDrag:
                    return _resolver.TryResolve(gesture.to, out ResolvedAnchor to)
                           && await DragAsync(from, to, gesture.kind, token);

                default:
                    return false;
            }
        }

        private async Awaitable<bool> ClickAsync(ResolvedAnchor at, GuideButton button, CancellationToken token)
        {
            _view.SetButton(button, false);
            if (!await MoveToAsync(at, MoveSeconds, GuideGestureKind.Hover, default, token))
                return false;

            _view.SetButton(button, true);
            _view.PlayRing();
            bool held = await WaitAsync(PressSeconds, token);
            _view.SetButton(button, false);
            return held && await WaitAsync(HoldSeconds, token);
        }

        private async Awaitable<bool> DragAsync(ResolvedAnchor from, ResolvedAnchor to, GuideGestureKind kind, CancellationToken token)
        {
            GuideButton button = ButtonOf(kind);
            _view.SetButton(button, false);
            if (!await MoveToAsync(from, MoveSeconds, GuideGestureKind.Hover, default, token))
                return false;

            _view.SetButton(button, true);
            _view.PlayRing();
            await WaitAsync(PressSeconds, token);

            Vector2 start = _view.Position;
            bool arrived = await MoveToAsync(to, DragSeconds, kind, start, token);
            if (arrived)
                await WaitAsync(0.25f, token);

            _view.SetButton(button, false);
            _view.HideDrag();
            return arrived && await WaitAsync(HoldSeconds, token);
        }

        private async Awaitable<bool> KeyAsync(string key, CancellationToken token)
        {
            _view.SetButton(GuideButton.None, false);
            _view.ShowKey(key, false);
            bool ok = await WaitAsync(0.35f, token);
            _view.ShowKey(key, true);
            ok = ok && await WaitAsync(0.25f, token);
            _view.ShowKey(key, false);
            ok = ok && await WaitAsync(0.4f, token);
            _view.HideKey();
            return ok;
        }

        /// <summary>
        /// 목표까지 부드럽게 이동합니다. 목표는 움직일 수 있어 매 프레임 다시 읽고, 사라지거나 끊기면 false.
        /// </summary>
        private async Awaitable<bool> MoveToAsync(ResolvedAnchor anchor, float seconds, GuideGestureKind drag,
            Vector2 dragStart, CancellationToken token)
        {
            Vector2 start = _view.Position;
            float t = 0f;
            while (t < 1f)
            {
                if (Interrupted || !anchor.TryGetScreenPoint(_worldCamera(), out Vector2 goal))
                    return false;

                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / seconds);
                Vector2 position = Clamp(Vector2.Lerp(start, goal, Mathf.SmoothStep(0f, 1f, t)));
                _view.SetPosition(position);

                if (drag == GuideGestureKind.LeftDrag)
                    _view.ShowDragBox(dragStart, position);
                else if (drag == GuideGestureKind.RightDrag)
                    _view.ShowDragLine(dragStart, position);

                await Awaitable.NextFrameAsync(token);
            }

            return true;
        }

        /// <summary>
        /// 잠깐 멈춥니다. 그 사이 끊기면 바로 false로 돌아옵니다.
        /// </summary>
        private async Awaitable<bool> WaitAsync(float seconds, CancellationToken token)
        {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end)
            {
                if (Interrupted)
                    return false;

                await Awaitable.NextFrameAsync(token);
            }

            return !Interrupted;
        }

        private static GuideButton ButtonOf(GuideGestureKind kind)
            => kind is GuideGestureKind.LeftClick or GuideGestureKind.LeftDrag ? GuideButton.Left : GuideButton.Right;

        public static Vector2 Clamp(Vector2 screen)
            => new(Mathf.Clamp(screen.x, EdgeMargin, Screen.width - EdgeMargin),
                Mathf.Clamp(screen.y, EdgeMargin, Screen.height - EdgeMargin));
    }
}
