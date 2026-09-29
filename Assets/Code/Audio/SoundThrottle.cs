using System;
using System.Collections.Generic;
using Ami.BroAudio;
using UnityEngine;
using BroAudioApi = Ami.BroAudio.BroAudio;

namespace Code.Audio
{
    /// <summary>
    /// 효과음이 한꺼번에 터지는 것을 막습니다.
    /// 분대 하나가 때리면 DamageTakenEvent가 다섯 번 나오고 분대가 여럿이면 프레임당 수십 번입니다 —
    /// 그대로 재생하면 소리가 아니라 잡음이 됩니다.
    /// </summary>
    public sealed class SoundThrottle
    {
        // 화면 가장자리에서 이만큼 더 나가야 안 들린다. 딱 잘라내면 가장자리에서 소리가 깜빡인다
        private const float ScreenMargin = 3f;

        private readonly Dictionary<int, float> _lastPlayed = new(16);

        private Camera _camera;

        /// <summary>
        /// 최소 간격을 지켰을 때만 재생합니다. 화면 밖 좌표는 아예 건너뜁니다.
        /// </summary>
        public bool Play(SoundID id, Vector3 position, float minInterval)
            => Play(id, position, minInterval, 0);

        /// <summary>
        /// 소리를 낸 주체별로 간격을 따로 잽니다. 같은 일을 여럿이 동시에 할 때
        /// 소리가 한 명분으로 묶여 현장이 조용해지는 것을 막습니다.
        /// </summary>
        public bool Play(SoundID id, Vector3 position, float minInterval, int sourceKey)
        {
            if (!IsOnScreen(position))
                return false;

            return Fire(id, minInterval, position, true, sourceKey);
        }

        /// <summary>
        /// 위치가 없는 소리(UI·전역 알림)를 최소 간격을 지켜 재생합니다.
        /// </summary>
        public bool Play(SoundID id, float minInterval)
            => Fire(id, minInterval, Vector3.zero, false, 0);

        private bool Fire(SoundID id, float minInterval, Vector3 position, bool positional, int sourceKey)
        {
            if (!id.IsValid())
                return false;

            // sourceKey가 0이면 소리 종류 하나로 묶인다 — 예전 동작 그대로다
            int key = HashCode.Combine(id.GetHashCode(), sourceKey);
            float now = Time.unscaledTime;

            if (_lastPlayed.TryGetValue(key, out float last) && now - last < minInterval)
                return false;

            _lastPlayed[key] = now;

            // BroAudio 예외가 이벤트 버스 전파를 끊지 않도록 격리한다 — GameplayAudio와 같은 이유다
            try
            {
                if (positional)
                    BroAudioApi.Play(id, position);
                else
                    BroAudioApi.Play(id);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SoundThrottle] 재생 실패(무시): {e.Message}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 카메라가 보고 있는 범위 안인지 봅니다. 카메라를 못 찾으면 전부 들리게 둡니다.
        /// </summary>
        private bool IsOnScreen(Vector3 position)
        {
            if (_camera == null)
                _camera = Camera.main;

            if (_camera == null)
                return true;

            Vector3 viewport = _camera.WorldToViewportPoint(position);
            float marginX = ScreenMargin / Mathf.Max(0.01f, _camera.orthographicSize * _camera.aspect * 2f);
            float marginY = ScreenMargin / Mathf.Max(0.01f, _camera.orthographicSize * 2f);

            return viewport.x > -marginX && viewport.x < 1f + marginX
                && viewport.y > -marginY && viewport.y < 1f + marginY;
        }
    }
}
