using System;
using UnityEngine;

namespace Code.Sequences.Guide
{
    public enum GuideGestureKind
    {
        LeftClick,
        RightClick,
        LeftDrag,
        RightDrag,
        Key,    // 키캡만 띄운다. 포인터는 제자리
        Hover,  // 누르지 않고 가리키기만
    }

    public enum GuideAnchorSpace
    {
        World,           // 힌트 종류로 찾은 대상(성에서 가장 가까운 것) + 월드 오프셋
        UI,              // UI 힌트 종류로 찾은 칸 + 픽셀 오프셋
        Screen,          // 뷰포트 0~1 좌표
        WorldBoundsMin,  // 종류 전체를 감싸는 사각형의 좌하 + 월드 오프셋. 끌어서 담기용
        WorldBoundsMax,  // 그 우상
    }

    /// <summary>
    /// 안내 손이 갈 자리 하나입니다.
    /// </summary>
    [Serializable]
    public class GuideAnchor
    {
        public GuideAnchorSpace space;
        public GameObjectHighlightType world;
        public UIElementHighlightType ui;
        public Vector2 offset;
        public float clearRadius; // 0이면 그대로. 양수면 이 반경이 비는 가장 가까운 통행 칸으로 옮긴다 (건설 자리·이동 자리)
    }

    /// <summary>
    /// 안내 손이 보여 줄 동작 하나입니다. 끌기만 to를 쓰고, Key만 key를 씁니다.
    /// </summary>
    [Serializable]
    public class GuideGesture
    {
        public GuideGestureKind kind;
        public GuideAnchor from = new();
        public GuideAnchor to = new();
        public string key;
    }
}
