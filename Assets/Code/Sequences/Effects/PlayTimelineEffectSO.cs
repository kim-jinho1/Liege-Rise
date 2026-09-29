using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;

namespace Code.Sequences.Effects
{
    /// <summary>
    /// 지정한 타임라인(.playable)을 재생하는 효과입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Eff_PlayTimeline", menuName = "Sequence/Effects/PlayTimeline")]
    public class PlayTimelineEffectSO : SequenceStepEffectSO
    {
        [BoxGroup("타임라인 연출"), LabelText("타임라인 에셋"), Required]
        [field: SerializeField] public PlayableAsset Timeline { get; private set; }

        /// <summary>
        /// 지정 타임라인을 재생하고 완료될 때까지 대기합니다.
        /// </summary>
        public override Awaitable ExecuteAsync()
        {
            return SequenceTimelinePlayer.Instance == null ? null : SequenceTimelinePlayer.Instance.PlayAsync(Timeline);
        }
    }
}
