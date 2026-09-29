using Ami.BroAudio;
using Code.Players.Cursors;
using UnityEngine;

namespace Code.Economics
{
    [CreateAssetMenu(fileName = "Resource", menuName = "Units/Resource", order = 5)]
    public class ResourceSO : ScriptableObject
    {
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }
        // 이 자원 위에서 띄울 채집 커서. 커맨드가 밝힌 커서보다 우선한다
        [field: SerializeField] public CursorKind HoverCursor { get; private set; }

        // 캘 때 나는 소리. 나무는 도끼, 돌은 곡괭이라 자원마다 다르다
        [field: SerializeField] public SoundID GatherSound { get; private set; }

        [field: SerializeField] public int AmountPerGather { get; private set; } = 8;
        [field: SerializeField] public float BaseGatherTime { get; private set; } = 1.5f;
        [field: SerializeField] public float AnimationID { get; private set; } // 재생되는 IDLE, RUN 애니메이션 ID
        [field: SerializeField] public float GatherID { get; private set; }    // 재생되는 INTERACT 애니메이션 ID
        // 클립에서 도구가 닿는 지점. 소리가 여기서 난다 — 도끼·곡괭이는 6키 0.75초라 마지막 키가 0.833이다
        [field: SerializeField, Range(0f, 1f)] public float GatherBeatPhase { get; private set; } = 0.833f;
    }
}
