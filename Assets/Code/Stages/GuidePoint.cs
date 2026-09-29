using System;
using Code.Sequences;
using UnityEngine;

namespace Code.Stages
{
    /// <summary>
    /// 튜토리얼이 짚을 자리 하나입니다. 지도를 보고 찍은 월드 좌표입니다.
    /// 코드가 성 기준 오프셋으로 계산하면 지형을 못 봐서 나무 위나 물가를 짚습니다.
    /// </summary>
    [Serializable]
    public class GuidePoint
    {
        [field: SerializeField] public GameObjectHighlightType Key { get; private set; }
        [field: SerializeField] public Vector2 Position { get; private set; }
    }
}
