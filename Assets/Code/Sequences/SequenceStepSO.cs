using Code.Objectives;
using Code.Sequences.Guide;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Code.Sequences
{
    /// <summary>
    /// 시퀀스 단계 1개를 정의합니다. 다이얼로그, 하이라이트 힌트, 완료 조건을 포함합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SequenceStep", menuName = "Sequence/SequenceStep")]
    public class SequenceStepSO : ObjectiveSO
    {
        [FoldoutGroup("대사"), PropertyOrder(10)]
        [LabelText("대사 줄")]
        [ListDrawerSettings(ShowFoldout = false, ListElementLabelName = "speakerName",
            DraggableItems = true)]
        [field: SerializeField] public DialogLine[] DialogLines { get; private set; }

        [FoldoutGroup("하이라이트"), PropertyOrder(20)]
        [LabelText("게임오브젝트 힌트"), EnumToggleButtons]
        [field: SerializeField] public GameObjectHighlightType GameObjectHint { get; private set; }

        [FoldoutGroup("하이라이트"), PropertyOrder(20)]
        [LabelText("UI 힌트"), EnumToggleButtons]
        [field: SerializeField] public UIElementHighlightType UIElementHint { get; private set; }

        /// <summary>
        /// 안내 손이 반복해 보여 줄 동작입니다. 대사가 끝난 뒤 스텝이 끝날 때까지 돕니다. 없으면 비워 둡니다.
        /// </summary>
        [FoldoutGroup("안내 손"), PropertyOrder(30)]
        [LabelText("제스처")]
        [ListDrawerSettings(ShowFoldout = false, DraggableItems = true)]
        [field: SerializeField] public GuideGesture[] Gestures { get; private set; }

        /// <summary>
        /// 스텝 진입 시 실행할 효과들(카메라 이동, 유닛·자원 지급 등). 없으면 비워 둡니다.
        /// </summary>
        [FoldoutGroup("효과"), PropertyOrder(40)]
        [LabelText("진입 효과"), InlineEditor(InlineEditorModes.GUIAndHeader)]
        [ListDrawerSettings(ShowFoldout = false, DraggableItems = true)]
        [field: SerializeField] public SequenceStepEffectSO[] Effects { get; private set; }
    }
}
