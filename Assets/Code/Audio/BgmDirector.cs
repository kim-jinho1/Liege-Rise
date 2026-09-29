using System;
using System.Threading;
using Ami.BroAudio;
using UnityEngine;
using BroAudioApi = Ami.BroAudio.BroAudio;

namespace Code.Audio
{
    /// <summary>
    /// 전황에 맞춰 배경음을 갈아 끼웁니다. 곡 목록과 값은 GameplayAudio가 인스펙터로 쥐고 넘깁니다.
    /// </summary>
    public sealed class BgmDirector
    {
        // 곡 목록이 비어 있어 재생을 못 했을 때 다음 시도까지 쉬는 시간
        private const float RetryDelay = 1f;

        private readonly SoundID[][] _tracks;
        private readonly int[] _lastIndex = { -1, -1, -1 };

        private readonly float _fadeIn;
        private readonly float _fadeOut;
        private readonly Vector2 _gapRange;
        private readonly float _tensionAfterSeconds;
        private readonly float _crisisMinSeconds;

        private BgmMood _baseline = BgmMood.Calm;
        private bool _inCrisis;
        private bool _crisisEndRequested;
        private float _crisisSince;
        private float _startTime;

        private SoundID _current;  // 지금 도는 곡. 판이 끝날 때 끊을 대상이다
        private bool _finished;

        public BgmDirector(SoundID[] calm, SoundID[] tension, SoundID[] crisis,
            float fadeIn, float fadeOut, Vector2 gapRange, float tensionAfterSeconds, float crisisMinSeconds)
        {
            _tracks = new[]
            {
                calm ?? Array.Empty<SoundID>(),
                tension ?? Array.Empty<SoundID>(),
                crisis ?? Array.Empty<SoundID>()
            };

            _fadeIn = fadeIn;
            _fadeOut = fadeOut;
            _gapRange = gapRange;
            _tensionAfterSeconds = tensionAfterSeconds;
            _crisisMinSeconds = crisisMinSeconds;
        }

        /// <summary>
        /// 어느 분위기든 틀 곡이 하나라도 있는지입니다. 하나도 없으면 루프를 시작할 이유가 없습니다.
        /// </summary>
        public bool HasAnyTrack()
            => HasTracks(BgmMood.Calm) || HasTracks(BgmMood.Tension) || HasTracks(BgmMood.Crisis);

        /// <summary>
        /// 공세가 출발했습니다. 곡이 끝나기를 기다리지 않고 곧바로 위험으로 넘어갑니다.
        /// </summary>
        public void EnterCrisis()
        {
            _crisisEndRequested = false;
            _crisisSince = Time.time;
            _inCrisis = true;

            // 공세가 왔다는 것은 더 이상 초반이 아니다. 물리쳐도 평화로는 돌아가지 않는다
            if (_baseline == BgmMood.Calm)
                _baseline = BgmMood.Tension;
        }

        /// <summary>
        /// 공세를 물리쳤습니다. 최소 유지 시간을 채운 뒤 기준 분위기로 돌아갑니다.
        /// </summary>
        public void ClearCrisis()
            => _crisisEndRequested = true;

        /// <summary>
        /// 판이 끝났습니다. 도는 곡을 끊고 다음 곡을 잇지 않습니다 —
        /// 승리·패배 소리가 배경음 위에 겹치면 둘 다 뭉갭니다.
        /// </summary>
        public void Finish(float fadeOut)
        {
            if (_finished)
                return;

            _finished = true;
            Stop(_current, fadeOut);
        }

        /// <summary>
        /// 곡을 하나씩 이어 틀며 분위기 변화를 따라갑니다. 판이 끝날 때까지 돕니다.
        /// </summary>
        public async void Run(CancellationToken token)
        {
            _startTime = Time.time;

            try
            {
                while (!_finished)
                {
                    BgmMood mood = ResolveMood();
                    bool interrupted = await PlayAsync(mood, token);

                    // 위험으로 들어가느라 끊었으면 곧바로 잇는다 — 공세가 왔는데 침묵이면 김이 샌다.
                    // 반대로 위험에서 빠져나올 때는 쉬어 준다. 그 침묵이 "끝났다"는 신호다
                    if (!interrupted || mood == BgmMood.Crisis)
                        await WaitGapAsync(ResolveMood(), token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// 곡 하나를 끝까지, 또는 위험 상태가 바뀔 때까지 재생합니다. 끊겼으면 true입니다.
        /// </summary>
        private async Awaitable<bool> PlayAsync(BgmMood mood, CancellationToken token)
        {
            SoundID track = PickTrack(mood);
            bool finished = false;

            try
            {
                IAudioPlayer player = track.IsValid() ? BroAudioApi.Play(track, _fadeIn) : null;
                if (player == null)
                {
                    await Awaitable.WaitForSecondsAsync(RetryDelay, token);
                    return false;
                }

                player.OnEnd(_ => finished = true);
                _current = track;
            }
            catch (Exception e)
            {
                // BroAudio 예외가 루프를 끊지 않게 격리한다
                Debug.LogWarning($"[BgmDirector] BGM 재생 실패(무시): {e.Message}");
                await Awaitable.WaitForSecondsAsync(RetryDelay, token);
                return false;
            }

            bool startedInCrisis = mood == BgmMood.Crisis;

            while (!finished)
            {
                if (_finished)
                    return true;

                Tick();

                // 위험으로 들어가거나 나올 때만 곡을 자른다. 평화 → 중간 같은 기준선 변화는
                // 다음 곡부터 반영한다 — 멀쩡한 곡을 중간에 끊을 이유가 없다
                BgmMood now = ResolveMood();
                if (now != mood && (now == BgmMood.Crisis || startedInCrisis))
                {
                    Stop(track, _fadeOut);
                    return true;
                }

                await Awaitable.NextFrameAsync(token);
            }

            return false;
        }

        /// <summary>
        /// 곡과 곡 사이의 침묵입니다. 그동안 분위기가 바뀌면 남은 시간을 버리고 곧바로 다음 곡으로 갑니다.
        /// 그냥 기다리면 공세가 침묵 중에 시작됐을 때 위험 곡이 최대 gapRange만큼 늦게 듭니다.
        /// </summary>
        private async Awaitable WaitGapAsync(BgmMood expected, CancellationToken token)
        {
            float until = Time.time + ResolveGap();

            while (Time.time < until)
            {
                if (_finished)
                    return;

                Tick();
                if (ResolveMood() != expected)
                    return;

                await Awaitable.NextFrameAsync(token);
            }
        }

        /// <summary>
        /// 시간에 따른 기준선 상승과 위험 해제 예약을 처리합니다.
        /// </summary>
        private void Tick()
        {
            if (_baseline == BgmMood.Calm && Time.time - _startTime >= _tensionAfterSeconds)
                _baseline = BgmMood.Tension;

            if (!_inCrisis || !_crisisEndRequested)
                return;

            // 공세가 순식간에 갈려도 최소 시간은 채운다. 안 그러면 곡이 켜졌다 꺼지며 딸꾹거린다
            if (Time.time - _crisisSince < _crisisMinSeconds)
                return;

            _inCrisis = false;
            _crisisEndRequested = false;
        }

        /// <summary>
        /// 지금 틀어야 할 분위기입니다. 곡이 없는 분위기는 건너뛰고 있는 쪽으로 물러섭니다.
        /// </summary>
        private BgmMood ResolveMood()
        {
            if (_inCrisis && HasTracks(BgmMood.Crisis))
                return BgmMood.Crisis;

            if (_baseline != BgmMood.Calm && HasTracks(BgmMood.Tension))
                return BgmMood.Tension;

            if (HasTracks(BgmMood.Calm))
                return BgmMood.Calm;

            return HasTracks(BgmMood.Tension) ? BgmMood.Tension : BgmMood.Crisis;
        }

        private bool HasTracks(BgmMood mood)
        {
            foreach (SoundID track in _tracks[(int)mood])
                if (track.IsValid())
                    return true;

            return false;
        }

        /// <summary>
        /// 이번에 틀 곡입니다. 곡이 둘 이상이면 그 분위기의 직전 곡을 피합니다.
        /// </summary>
        private SoundID PickTrack(BgmMood mood)
        {
            SoundID[] tracks = _tracks[(int)mood];
            if (tracks.Length == 0)
                return default;

            if (tracks.Length == 1)
                return tracks[0];

            int index = UnityEngine.Random.Range(0, tracks.Length);
            if (index == _lastIndex[(int)mood])
                index = (index + 1) % tracks.Length;

            _lastIndex[(int)mood] = index;
            return tracks[index];
        }

        private static void Stop(SoundID track, float fadeOut)
        {
            if (!track.IsValid())
                return;

            try
            {
                BroAudioApi.Stop(track, fadeOut);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BgmDirector] BGM 정지 실패(무시): {e.Message}");
            }
        }

        private float ResolveGap()
        {
            float min = Mathf.Max(0f, _gapRange.x);
            return UnityEngine.Random.Range(min, Mathf.Max(min, _gapRange.y));
        }
    }
}
