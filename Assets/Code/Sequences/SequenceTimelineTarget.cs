using Code.Core;
using UnityEngine;
using UnityEngine.Playables;

namespace Code.Sequences
{
    /// <summary>
    /// 시퀀스 연출 대상 PlayableDirector임을 나타내는 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(PlayableDirector))]
    public class SequenceTimelineTarget : MonoBehaviour
    {
        private PlayableDirector _director;

        private void Awake()
        {
            _director = GetComponent<PlayableDirector>();
            UnityRef.Alive(SequenceTimelinePlayer.Instance)?.Register(_director);
        }

        private void OnDestroy()
            => UnityRef.Alive(SequenceTimelinePlayer.Instance)?.Unregister(_director);
    }
}
