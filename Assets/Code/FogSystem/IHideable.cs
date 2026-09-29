using UnityEngine;

namespace Code.FogSystem
{
    /// <summary>
    /// 시야 시스템에 의해 가시성이 제어되는 오브젝트가 구현하는 인터페이스입니다.
    /// </summary>
    public interface IHideable
    {
        Transform Transform { get; }
        bool IsVisible { get; }
        void SetVisible(bool isVisible);

        delegate void VisibilityChangedEvent(IHideable hideable, bool isVisible);

        event VisibilityChangedEvent OnVisibilityChanged;
    }
}
