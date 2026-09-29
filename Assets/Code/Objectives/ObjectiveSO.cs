using Sirenix.OdinInspector;
using UnityEngine;

namespace Code.Objectives
{
    /// <summary>
    /// 단일 목표를 정의하는 ScriptableObject 기반 데이터 클래스입니다.
    /// </summary>
    public class ObjectiveSO : ScriptableObject
    {
        [Title("기본 정보")]
        [LabelText("제목"), PropertyOrder(0)]
        [field: SerializeField] public string Title { get; private set; }

        [LabelText("설명"), TextArea(2, 4), PropertyOrder(1)]
        [field: SerializeField] public string Description { get; private set; }

        [FoldoutGroup("완료 조건"), PropertyOrder(30)]
        [LabelText("조건"), InlineEditor(InlineEditorModes.GUIAndHeader)]
        [field: SerializeField] public ObjectiveConditionSO Condition { get; private set; }

        [FoldoutGroup("완료 조건"), PropertyOrder(30)]
        [LabelText("선택 목표(스킵 가능)")]
        [field: SerializeField] public bool IsOptional { get; private set; }
    }
}
