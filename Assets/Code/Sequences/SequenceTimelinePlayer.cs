using System.Collections.Generic;
using Code.Core;
using UnityEngine;
using UnityEngine.Playables;

namespace Code.Sequences
{
    /// <summary>
    /// 시퀀스 연출용 PlayableDirector를 자신의 PlayableAsset(.playable) 기준으로 관리하는 싱글톤입니다.
    /// SequenceTimelineTarget이 자기 등록하며, 등록 딕셔너리 기반 O(1) 조회로 재생합니다.
    /// </summary>
    public class SequenceTimelinePlayer : MonoSingleton<SequenceTimelinePlayer>
    {
        private readonly Dictionary<PlayableAsset, PlayableDirector> _directors = new();

        /// <summary>
        /// SequenceTimelineTarget이 Awake에서 호출합니다. Director가 가진 PlayableAsset을 키로 등록합니다.
        /// </summary>
        public void Register(PlayableDirector director)
        {
            if (director == null || director.playableAsset == null)
                return;

            _directors[director.playableAsset] = director;
        }

        /// <summary>
        /// SequenceTimelineTarget이 OnDestroy에서 호출합니다.
        /// </summary>
        public void Unregister(PlayableDirector director)
        {
            if (director != null && director.playableAsset != null)
                _directors.Remove(director.playableAsset);
        }

        /// <summary>
        /// 지정 타임라인 에셋에 해당하는 Director를 처음부터 재생하고 끝날 때까지 대기합니다.
        /// 에셋이 없거나 매칭되는 Director가 없으면 즉시 반환합니다.
        /// </summary>
        public async Awaitable PlayAsync(PlayableAsset timeline)
        {
            if (timeline == null)
                return;

            if (!_directors.TryGetValue(timeline, out PlayableDirector director) || director == null)
                return;

            director.time = 0d;
            director.Play();

            while (director.state == PlayState.Playing && director.time < director.duration)
                await Awaitable.NextFrameAsync();
        }
    }
}
