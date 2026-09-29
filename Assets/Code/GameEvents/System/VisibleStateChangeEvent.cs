using Code.Core.Bus;
using UnityEngine;

namespace Code.GameEvents.System
{
    /// <summary>
    /// 건물/유닛의 가시성 상태가 변경되었을 때 발행되는 이벤트입니다.
    /// PlaceHolderManager가 구독하여 잔상을 생성/제거합니다.
    /// </summary>
    public struct VisibleStateChangeEvent : IEvent
    {
        public Owner Owner { get; private set; }
        public Transform Transform { get; private set; }
        public SpriteRenderer Renderer { get; private set; }
        public bool IsVisible { get; private set; }

        public VisibleStateChangeEvent(Owner owner, Transform transform, SpriteRenderer renderer, bool isVisible)
        {
            Owner = owner;
            Transform = transform;
            Renderer = renderer;
            IsVisible = isVisible;
        }
    }
}
