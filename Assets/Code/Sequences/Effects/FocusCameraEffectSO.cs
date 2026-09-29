using Code.FogSystem;
using Code.Players;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Code.Sequences.Effects
{
    /// <summary>
    /// 지정한 하이라이트 대상으로 카메라를 이동시키는 효과입니다(필요 시 줌도 조정).
    /// 대상 좌표는 SequenceHighlighter에 등록된 오브젝트에서 가져옵니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Eff_FocusCamera", menuName = "Sequence/Effects/FocusCamera")]
    public class FocusCameraEffectSO : SequenceStepEffectSO
    {
        [BoxGroup("카메라 포커스"), LabelText("대상"), EnumToggleButtons]
        [field: SerializeField] public GameObjectHighlightType Target { get; private set; }

        [BoxGroup("카메라 포커스"), LabelText("줌 변경")]
        [field: SerializeField] public bool OverrideZoom { get; private set; }

        [BoxGroup("카메라 포커스"), LabelText("줌 크기"), Indent]
        [ShowIf(nameof(OverrideZoom)), MinValue(1f)]
        [field: SerializeField] public float ZoomSize { get; private set; } = 6f;

        [BoxGroup("안개 걷기"), LabelText("밝힐 반경"), MinValue(0f)]
        [field: SerializeField] public float RevealRadius { get; private set; } = 3f;

        [BoxGroup("안개 걷기"), LabelText("밝히는 시간"), MinValue(0f)]
        [field: SerializeField] public float RevealSeconds { get; private set; } = 5f;

        public override void Execute()
        {
            if (SequenceHighlighter.Instance == null || !TryResolveFocusPosition(out Vector3 position))
                return;

            CameraController camera = FindAnyObjectByType<CameraController>();
            if (camera == null)
                return;

            camera.JumpTo(position);
            if (OverrideZoom)
                camera.ZoomSize = ZoomSize;

            // 보여 주려고 옮겨 놓고 안개에 덮여 있으면 아무것도 못 본다
            _ = FogReveal.AtAsync(position, RevealRadius, RevealSeconds);
        }

        /// <summary>
        /// 포커스 대상 좌표를 해석합니다. 본성(MainCastle)이 등록돼 있으면 본성에서 가장 가까운
        /// 대상으로, 아니면 등록된 첫 대상으로 포커스합니다(여러 자원 중 임의 선택 방지).
        /// </summary>
        private bool TryResolveFocusPosition(out Vector3 position)
        {
            SequenceHighlighter highlighter = SequenceHighlighter.Instance;
            if (highlighter.TryGetPosition(GameObjectHighlightType.MainCastle, out Vector3 castle))
                return highlighter.TryGetNearestPosition(Target, castle, out position);

            return highlighter.TryGetPosition(Target, out position);
        }
    }
}
