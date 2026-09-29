using System;
using Code.Commandable;
using Code.Commandable.Interfaces;
using UnityEngine;

namespace Code.Players
{
    /// <summary>
    /// 커서 아래 유닛·건물의 외곽선을 켜고 끕니다.
    /// 무엇이 커서 아래에 있는지는 스스로 판정하지 않고 클릭 선택과 같은 답을 받아 씁니다 —
    /// 규칙을 두 벌로 만들면 호버와 클릭이 다른 대상을 가리킵니다.
    /// </summary>
    public sealed class CommandableHoverProbe : IDisposable
    {
        // 호버는 60Hz로 다시 볼 이유가 없다. 커서가 멈춰 있어도 그 밑을 유닛이 지나갈 수 있어
        // 아주 멈추지는 않고, 이 간격으로만 다시 본다
        private const float IdleInterval = 0.1f;

        private AbstractCommandable _hovered;
        private Vector2 _lastPointer = new(float.NaN, float.NaN);
        private float _nextIdleProbe;

        /// <summary>
        /// 커서 위치가 달라졌거나 간격이 지났을 때만 판정을 다시 부릅니다.
        /// suppressed면 판정하지 않고 외곽선을 끕니다.
        /// </summary>
        public void Update(bool suppressed, Vector2 pointer, Func<ISelectable> resolve)
        {
            if (suppressed || resolve == null)
            {
                SetHovered(null);
                return;
            }

            bool moved = pointer != _lastPointer;
            if (!moved && Time.unscaledTime < _nextIdleProbe)
                return;

            _lastPointer = pointer;
            _nextIdleProbe = Time.unscaledTime + IdleInterval;
            SetHovered(resolve());
        }

        public void Dispose()
            => SetHovered(null);

        private void SetHovered(ISelectable candidate)
        {
            AbstractCommandable target = candidate as AbstractCommandable;

            // 파괴된 대상을 Unity의 ==는 null과 같다고 보고한다 — 교체 판정만 ReferenceEquals로 한다
            if (ReferenceEquals(_hovered, target))
                return;

            if (_hovered != null)
                _hovered.SetHoveredVisual(false);

            _hovered = target;

            if (_hovered != null)
                _hovered.SetHoveredVisual(true);
        }
    }
}
