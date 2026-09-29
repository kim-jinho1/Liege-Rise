using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Code.Sequences
{
    [Serializable]
    public struct DialogLine
    {
        [HorizontalGroup("줄", 64), HideLabel, PreviewField(60, ObjectFieldAlignment.Center)]
        public Sprite speakerIcon;

        [VerticalGroup("줄/내용"), LabelText("화자")]
        public string speakerName;

        [LabelText("대사"), TextArea(3, 20)]
        public string text;
    }
}
