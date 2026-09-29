using UnityEngine;

namespace Code.Sequences
{
    /// <summary>
    /// 시퀀스 안내가 짚을 수 있는 대상입니다. 외곽선을 켜고 자리를 알려 줍니다.
    /// </summary>
    public interface IGuideHighlightable
    {
        Vector3 GuidePosition { get; }
        bool IsGuideAlive { get; }
        void SetGuideHighlight(bool on);
    }
}
