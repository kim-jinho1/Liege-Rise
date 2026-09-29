using System;
using System.Threading;
using Code.Players;
using Code.Players.Cursors;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.Sequences.Guide
{
    /// <summary>
    /// 스텝의 제스처를 안내 손으로 하나씩 반복 재생합니다. 첫 재생은 카메라 입력을 잠가 시연을 보게 하고,
    /// 플레이어가 그 동작을 하면 다음 동작으로 넘어가며, 전부 따라 하면 사라집니다.
    /// </summary>
    public sealed class GuideCursorPlayer : MonoBehaviour
    {
        private const float LoopPauseSeconds = 1.5f;
        private const float RetrySeconds = 1f;
        private const string CursorSetPath = "CursorSet";

        private readonly GuideAnchorResolver _resolver = new();
        private GuideInputDetector _detector;
        private GuideGestureAnimator _animator;
        private GuideCursorView _view;
        private GuideGesture[] _gestures;
        private int _progress;
        private CancellationTokenSource _cts;
        private CameraController _camera;
        private Camera _worldCamera;
        private bool _cameraLocked;

        /// <summary>
        /// 지금 차례가 짚는 UI 칸입니다. 차례가 넘어갈 때마다 알려, 테두리가 손을 따라가게 합니다.
        /// 월드를 짚는 차례면 None입니다.
        /// </summary>
        public event Action<UIElementHighlightType> UiFocusChanged;

        /// <summary>
        /// 제스처를 처음부터 반복 재생합니다. 비어 있으면 아무것도 띄우지 않습니다.
        /// </summary>
        public void Play(GuideGesture[] gestures, bool lockCameraOnFirstLoop)
        {
            Stop();
            if (gestures == null || gestures.Length == 0)
                return;

            if (_view == null)
            {
                _view = GuideCursorView.Create(transform, Resources.Load<CursorSetSO>(CursorSetPath));
                _animator = new GuideGestureAnimator(_view, _resolver, () => WorldCamera);
                _detector = new GuideInputDetector(_resolver);
            }

            _gestures = gestures;
            _progress = 0;
            _resolver.ClearFrozen();
            _animator.Reset();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            RaiseUiFocus();
            _ = LoopAsync(lockCameraOnFirstLoop, _cts.Token);
        }

        /// <summary>
        /// 지금 차례가 UI를 짚으면 그 칸을, 아니면 None을 알립니다.
        /// </summary>
        private void RaiseUiFocus()
        {
            GuideAnchor anchor = _gestures != null && _progress < _gestures.Length ? _gestures[_progress].from : null;
            UiFocusChanged?.Invoke(anchor != null && anchor.space == GuideAnchorSpace.UI
                ? anchor.ui
                : UIElementHighlightType.None);
        }

        /// <summary>
        /// 재생을 멈추고 숨깁니다. 카메라를 잠갔다면 풉니다.
        /// </summary>
        public void Stop()
        {
            _gestures = null;
            CancelPlayback();
            SetCameraLocked(false);
        }

        private void OnDestroy()
            => Stop();

        /// <summary>
        /// 플레이어가 지금 차례의 동작을 했으면 다음 차례로 넘어갑니다. 손을 댔으면 시연은 끝난 것이라 카메라도 풉니다.
        /// </summary>
        private void Update()
        {
            if (_gestures == null || _progress >= _gestures.Length)
                return;

            GuideGesture next = _progress + 1 < _gestures.Length ? _gestures[_progress + 1] : null;
            if (!_detector.Check(_gestures[_progress], next, WorldCamera))
                return;

            _progress++;
            _animator.Interrupt();
            SetCameraLocked(false);
            RaiseUiFocus(); // 테두리도 손을 따라 다음 칸으로 옮긴다

            // 전부 따라 했다. 스텝 완료는 게임 조건이 판정하니 손만 치운다
            if (_progress >= _gestures.Length)
                CancelPlayback();
        }

        private async Awaitable LoopAsync(bool lockFirst, CancellationToken token)
        {
            try
            {
                bool first = true;
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    if (first && lockFirst)
                        SetCameraLocked(true);

                    _view.SetPosition(GuideGestureAnimator.Clamp(RealMousePosition()));
                    _view.SetShown(true);

                    // 지금 차례 하나만 보여 준다. 남은 것을 이어서 다 보여 주면 "한 번에 다 하라"로 읽힌다 —
                    // 플레이어가 이것을 하면 Update가 차례를 넘기고, 다음 바퀴가 다음 동작을 보여 준다
                    int shown = 0;
                    if (_progress < _gestures.Length && !_animator.Interrupted
                        && await _animator.PlayAsync(_gestures[_progress], token))
                        shown++;

                    if (first)
                    {
                        SetCameraLocked(false);
                        first = false;
                    }

                    // 플레이어가 한 동작을 해서 끊겼다. 쉬지 않고 다음 동작부터 다시 보여 준다
                    if (_animator.Interrupted)
                    {
                        _animator.Reset();
                        continue;
                    }

                    // 짚을 것이 아직 없다(메뉴가 안 열렸다 등). 숨기고 잠시 뒤 다시 본다
                    if (shown == 0)
                        _view.SetShown(false);

                    await Awaitable.WaitForSecondsAsync(shown == 0 ? RetrySeconds : LoopPauseSeconds, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void CancelPlayback()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            if (_view == null)
                return;

            _animator.Reset();
            _view.SetShown(false);
        }

        private void SetCameraLocked(bool locked)
        {
            if (_cameraLocked == locked)
                return;

            if (_camera == null)
                _camera = FindAnyObjectByType<CameraController>();

            if (_camera == null)
                return;

            _camera.InputLocked = locked;
            _cameraLocked = locked;
        }

        private Camera WorldCamera
        {
            get
            {
                if (_worldCamera == null)
                    _worldCamera = Camera.main;

                return _worldCamera;
            }
        }

        private static Vector2 RealMousePosition()
            => Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }
}
