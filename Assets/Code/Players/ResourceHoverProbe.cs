using System;
using Code.Core;
using Code.Economics;
using Code.Environments;
using Code.UI.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.Players
{
    /// <summary>
    /// 마우스 아래 자원 오브젝트를 찾아 호버 외곽선과 월드 툴팁을 함께 구동합니다.
    /// 노드마다 컴포넌트를 두면 노드 수만큼 Update가 돌고 "프레임당 한 번"을 static으로
    /// 흉내내야 합니다. 판정 주체를 하나로 두면 그 장치가 통째로 필요 없습니다.
    /// </summary>
    public sealed class ResourceHoverProbe : IDisposable
    {
        private const string ResourceLayerName = "Supplies";

        private readonly Collider2D[] _overlapBuffer = new Collider2D[16];
        private readonly DelayedAction _hoverDelay = new();
        private readonly ContactFilter2D _filter;

        private Camera _camera;
        private ResourceObject _hovered;
        private bool _tooltipShown;
        private int _lastShownAmount;

        public ResourceHoverProbe()
        {
            ContactFilter2D filter = new() { useTriggers = true };

            int mask = LayerMask.GetMask(ResourceLayerName);
            if (mask != 0)
                filter.SetLayerMask(mask);

            _filter = filter;
        }

        /// <summary>
        /// 호버 대상과 툴팁을 해제합니다.
        /// </summary>
        public void Dispose()
        {
            SetHovered(null);
            _hoverDelay.Dispose();
        }

        /// <summary>
        /// 마우스 아래 자원 오브젝트를 찾아 외곽선과 툴팁을 갱신합니다.
        /// suppressed가 true면(드래그 중·UI 위) 호버를 해제합니다.
        /// </summary>
        public void Update(Camera camera, bool suppressed)
        {
            _camera = camera;

            SetHovered(suppressed || camera == null || Mouse.current == null ? null : FindHovered());

            // 호버 중 자원량이 변하면 표시 중인 수치를 따라 갱신한다
            if (!_tooltipShown || _hovered == null || _hovered.RemainingAmount == _lastShownAmount)
                return;

            _lastShownAmount = _hovered.RemainingAmount;
            if (Tooltip != null)
                Tooltip.UpdateCount(_lastShownAmount, _hovered.MaxAmount);
        }

        private void SetHovered(ResourceObject resource)
        {
            // 파괴된 대상을 Unity의 ==는 null과 같다고 보고한다 — 그대로 쓰면 고갈된 노드에
            // 붙은 툴팁이 영영 안 꺼진다. 교체 판정만 ReferenceEquals로 한다
            if (ReferenceEquals(_hovered, resource))
                return;

            if (_hovered != null)
                _hovered.SetHovered(false);

            _hovered = resource;

            if (_hovered != null)
                _hovered.SetHovered(true);

            _hoverDelay.Cancel();
            _tooltipShown = false;
            if (Tooltip != null)
                Tooltip.Hide();

            if (_hovered != null && Tooltip != null)
                _hoverDelay.Restart(Tooltip.HoverDelay, ShowTooltip);
        }

        private ResourceObject FindHovered()
        {
            Vector2 mouseWorld = _camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            int count = Physics2D.OverlapPoint(mouseWorld, _filter, _overlapBuffer);

            // 콜라이더가 아니라 오브젝트로 잡는다 — 노드는 프리팹의 CircleCollider2D와
            // ResourceObject가 런타임에 붙이는 선택용 BoxCollider2D를 함께 들고 있다
            for (int i = 0; i < count; i++)
            {
                if (!_overlapBuffer[i].TryGetComponent(out ResourceObject node))
                    continue;

                // 고갈된 밑둥과 안개 속 자원은 커서에 반응하지 않는다
                if (node.IsDepleted || !node.IsVisible)
                    continue;

                return node;
            }

            return null;
        }

        private void ShowTooltip()
        {
            if (_hovered == null || _camera == null || Tooltip == null || Mouse.current == null)
                return;

            ResourceSO resourceSO = _hovered.ResourceSO;
            TooltipData data = new(
                resourceSO.Icon,
                resourceSO.DisplayName,
                _hovered.RemainingAmount,
                _hovered.MaxAmount);

            Vector3 mouseWorld = _camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 offset = Tooltip.Offset;

            Tooltip.Show(data, new Vector2(mouseWorld.x + offset.x, mouseWorld.y + offset.y));
            _lastShownAmount = _hovered.RemainingAmount;
            _tooltipShown = true;
        }

        // 툴팁은 _Core에 있고 이 probe는 맵 씬에서 먼저 돌 수 있다
        private static WorldTooltip Tooltip
            => WorldTooltip.Instance;
    }
}
