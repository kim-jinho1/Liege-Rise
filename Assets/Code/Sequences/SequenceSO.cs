using Sirenix.OdinInspector;
using UnityEngine;

namespace Code.Sequences
{
    /// <summary>
    /// 순차 진행되는 시퀀스 스텝 묶음입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Sequence", menuName = "Sequence/Sequence")]
    public class SequenceSO : ScriptableObject
    {
        [Title("시퀀스 스텝")]
        [LabelText("스텝 목록")]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "Title",
            DraggableItems = true)]
        [InlineEditor(Expanded = false)]
        [field: SerializeField] public SequenceStepSO[] Steps { get; private set; }
    }
}
