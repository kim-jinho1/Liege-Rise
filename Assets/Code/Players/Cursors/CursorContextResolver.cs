using Code.Buildings.Interfaces;
using Code.Buildings.Utils;
using Code.Commandable;
using Code.Commandable.Interfaces;
using Code.Commands.Base;
using Code.Commands.Types.Production;
using Code.Core;
using Code.Core.Input;
using Code.Environments;
using Code.FogSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;

namespace Code.Players.Cursors
{
    /// <summary>
    /// 마우스 아래 상황을 우선순위대로 훑어 지금 써야 할 커서를 고릅니다.
    /// </summary>
    public sealed class CursorContextResolver
    {
        private readonly InputReader _input;
        private readonly SelectionController _selection;
        private readonly GhostPlacementPreviewManager _ghost;
        private readonly RightClickCommandResolver _commands = new();

        private readonly LayerMask _floorLayer;
        private readonly LayerMask _interactableLayer;
        private readonly LayerMask _selectableLayer;

        public CursorContextResolver(InputReader input, SelectionController selection,
            GhostPlacementPreviewManager ghost,
            LayerMask floorLayer, LayerMask interactableLayer, LayerMask selectableLayer)
        {
            _input = input;
            _selection = selection;
            _ghost = ghost;
            _floorLayer = floorLayer;
            _interactableLayer = interactableLayer;
            _selectableLayer = selectableLayer;
        }

        /// <summary>
        /// 이번 프레임에 써야 할 커서를 판정합니다.
        /// </summary>
        public CursorKind Resolve()
        {
            if (IsPointerOverUI())
                return CursorKind.Default;

            // 건설 중에는 고스트가 빨개지는 그 판정을 그대로 쓴다 — 새로 계산하지 않는다
            if (_ghost != null && _ghost.IsActive)
                return _ghost.IsPlacementValid ? CursorKind.Build : CursorKind.Invalid;

            CursorKind fromCommand = ResolveFromRightClick();
            if (fromCommand != CursorKind.Default)
                return fromCommand;

            return HasSelectableUnderPointer() ? CursorKind.Select : CursorKind.Default;
        }

        /// <summary>
        /// 지금 우클릭하면 실행될 커맨드를 찾아 그 커맨드가 밝힌 커서를 돌려줍니다.
        /// </summary>
        private CursorKind ResolveFromRightClick()
        {
            AbstractCommandable commandable = FindRepresentative();
            if (commandable == null)
                return CursorKind.Default;

            if (!_input.GetMousePosition(out RaycastHit2D groundHit, _floorLayer | _interactableLayer))
                return CursorKind.Default;

            _input.GetMousePosition(out RaycastHit2D interactableHit, _interactableLayer);

            // 시야를 잃은 대상은 콜라이더가 살아 있어도 없는 것으로 친다 —
            // 막지 않으면 안개 위에 커서만 문질러 적 위치를 알아낼 수 있다
            bool targetVisible = interactableHit.collider != null
                                 && FogVisibility.IsVisibleNow(interactableHit.collider);

            RaycastHit2D targetHit = targetVisible ? interactableHit : groundHit;

            CommandContext context = new(commandable, targetHit, 0, MouseButton.Right, groundHit.point);
            BaseCommandSO command = _commands.Resolve(commandable, context);

            if (command == null)
                return CursorKind.Default;

            // 자원 커서는 지금 우클릭이 실제로 채집일 때만 이긴다.
            // 전투 유닛으로 나무를 가리키면 나갈 명령은 이동인데 도끼가 뜨면 커서가 거짓말을 한다.
            // 판정은 여기서 새로 하지 않는다 — 위에서 이미 정해진 커맨드가 무엇인지만 본다
            CursorKind fromTarget = command is GatherCommandSO ? ResolveFromTarget(targetHit) : CursorKind.Default;
            return fromTarget != CursorKind.Default ? fromTarget : command.HoverCursor;
        }

        /// <summary>
        /// 채집 대상이 자기 커서를 들고 있으면 커맨드보다 우선합니다.
        /// 채집 커맨드 하나가 나무와 돌을 다 처리하므로 도끼와 곡괭이는 자원만 가를 수 있습니다.
        /// </summary>
        private static CursorKind ResolveFromTarget(RaycastHit2D hit)
        {
            if (hit.collider == null || !hit.collider.TryGetComponent(out ResourceObject resource))
                return CursorKind.Default;

            return resource.IsDepleted || resource.ResourceSO == null
                ? CursorKind.Default
                : resource.ResourceSO.HoverCursor;
        }

        /// <summary>
        /// 선택된 것 중 명령을 받을 수 있는 첫 대상을 고릅니다.
        /// </summary>
        private AbstractCommandable FindRepresentative()
        {
            if (_selection == null || _selection.SelectedUnits == null)
                return null;

            foreach (ISelectable selectable in _selection.SelectedUnits)
                if (selectable is AbstractCommandable commandable and not IBuilding
                    && commandable.Owner == _selection.ControlOwner)
                    return commandable;

            return null;
        }

        private bool HasSelectableUnderPointer()
        {
            if (!_input.GetMousePosition(out RaycastHit2D hit, _selectableLayer))
                return false;

            // 인터페이스에 is not null을 쓰면 파괴된 대상이 통과해 커서만 "선택"으로 바뀐다
            return FogVisibility.IsVisibleNow(hit.collider)
                   && UnityRef.IsAlive(hit.collider.GetComponentInParent<ISelectable>());
        }

        private static bool IsPointerOverUI()
            => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
