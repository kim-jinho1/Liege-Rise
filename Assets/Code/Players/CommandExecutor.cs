using System.Collections.Generic;
using Code.Audio;
using Code.Buildings.Interfaces;
using Code.Buildings.Utils;
using Code.Commandable;
using Code.Commandable.Interfaces;
using Code.Commands.Base;
using Code.Commands.Interfaces;
using Code.Core.Bus;
using Code.Core.Input;
using Code.GameEvents.System;
using Code.GameEvents.Unit;
using Code.Navigation.Baking;
using Code.Players.Cursors;
using Code.Units.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Code.Players
{
    /// <summary>
    /// 플레이어의 입력을 받아 유닛에게 명령을 내리는 핵심 컴포넌트입니다.
    /// </summary>
    public class CommandExecutor : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private InputReader inputReader;
        [SerializeField] private SelectionController selectionController;

        [Header("Cursor")]
        [SerializeField] private CursorDriver cursorDriver;

        [Header("Layer Settings")]
        [SerializeField] private LayerMask selectableUnitLayer;
        [SerializeField] private LayerMask floorLayer;
        [SerializeField] private LayerMask interactableLayer;

        private BaseCommandSO _activeCommand;
        private GhostPlacementPreviewManager _ghostPreviewManager;
        private BattleLineDrawer _lineDrawer;
        private BusSubscriptionScope _subscriptionScope;
        private bool _pointerOverUI; // Update에서 잰다. 입력 콜백 안에서는 EventSystem에 물을 수 없다

        private readonly RightClickCommandResolver _rightClickResolver = new();
        private readonly ConstructorPicker _constructorPicker = new();
        private readonly List<AbstractCommandable> _commandableBuffer = new(64);
        private readonly List<ISelectable> _selectedBuffer = new(64);

        public bool HasActiveCommand
            => _activeCommand != null;

        private void Awake()
        {
            inputReader.OnMouseRightButton += HandleMouseRightButton;

            _ghostPreviewManager = gameObject.AddComponent<GhostPlacementPreviewManager>();
            _ghostPreviewManager.Initialize(inputReader, selectionController, floorLayer);

            AttachCursorDriver();

            // 공세선 드래그도 커서·고스트와 같은 방식으로 붙는다 — 씬 배선 없음
            _lineDrawer = gameObject.AddComponent<BattleLineDrawer>();
            _lineDrawer.Initialize(inputReader, selectionController);

            _subscriptionScope = new BusSubscriptionScope();
            _subscriptionScope.Subscribe<CommandSelectEvent>(selectionController.ControlOwner, HandleCommandSelect);
        }

        private void OnDestroy()
        {
            inputReader.OnMouseRightButton -= HandleMouseRightButton;
            _subscriptionScope?.Dispose();
        }

        private void Update()
        {
            // 입력 콜백 안에서 EventSystem에 물으면 Unity가 경고한다 — 그 자리에서는 지난 프레임 UI
            // 상태밖에 못 얻기 때문이다. 어차피 같은 값이니 여기서 미리 재 두고 콜백은 이것을 읽는다
            _pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            _ghostPreviewManager.UpdatePreview();

            if (HasActiveCommand && IsCancelPressed())
            {
                CancelActiveCommand();
                return;
            }

            HandleLeftClickToActivateCommand();
        }

        /// <summary>
        /// 진행 중인 커맨드를 고스트째로 취소합니다.
        /// 고스트만 지우면 커맨드가 남아 다음 좌클릭에 미리보기 없이 실행됩니다.
        /// </summary>
        private void CancelActiveCommand()
        {
            _ghostPreviewManager.DestroyGhost();
            _activeCommand = null;
            Bus<CommandSelectEvent>.Raise(selectionController.ControlOwner, new CommandSelectEvent(null));
        }

        private bool IsCancelPressed()
            => !inputReader.IsTextInputActive
               && Keyboard.current != null
               && Keyboard.current.escapeKey.wasReleasedThisFrame;

        private void HandleLeftClickToActivateCommand()
        {
            if (!HasActiveCommand || Mouse.current == null)
                return;

            if (_pointerOverUI)
                return;

            if (!Mouse.current.leftButton.wasReleasedThisFrame)
                return;

            if (inputReader.GetMousePosition(out RaycastHit2D hit,
                    floorLayer | interactableLayer | selectableUnitLayer))
                ActivateCommand(hit);
        }

        private void HandleCommandSelect(CommandSelectEvent evt)
        {
            _ghostPreviewManager.DestroyGhost();
            _activeCommand = evt.Command;

            if (_activeCommand == null)
                return;

            if (!_activeCommand.RequireClickToActivate)
                ActivateCommand(new RaycastHit2D());
            else if (_activeCommand is IConstructionCommand buildCommand)
                _ghostPreviewManager.ShowGhost(buildCommand);
        }

        private void ActivateCommand(RaycastHit2D hit)
        {
            _ghostPreviewManager.DestroyGhost();
            if (_activeCommand is IConstructionCommand constructionCommand)
            {
                Vector3 snapped = SnapToGrid(hit.point, constructionCommand.BuildingSize);
                hit = CreateHitAtPoint(hit, snapped);

                AbstractUnit constructor = _constructorPicker.Pick(selectionController, snapped);
                if (constructor != null)
                {
                    CommandContext context = new CommandContext(constructor, hit);
                    if (_activeCommand.CanHandle(context))
                        _activeCommand.Handle(context);
                    else
                        _activeCommand.NotifyRefusal(context); // 왜 안 지어졌는지는 커맨드가 안다
                }

                _activeCommand = null;
                return;
            }

            _commandableBuffer.Clear();
            foreach (ISelectable selectable in selectionController.SelectedUnits)
                if (selectable is AbstractCommandable commandable)
                    _commandableBuffer.Add(commandable);

            bool handled = false;
            for (int i = 0; i < _commandableBuffer.Count; i++)
            {
                CommandContext context = new CommandContext(_commandableBuffer[i], hit, i);
                if (!_activeCommand.CanHandle(context))
                    continue;

                _activeCommand.Handle(context);
                handled = true;
                if (_activeCommand.IsSingleUnitCommand)
                    break;
            }

            // 눌렀는데 아무도 받지 않았다 — 잠긴 이유를 커맨드 에셋이 들고 있으면 알린다
            if (!handled && _commandableBuffer.Count > 0)
                _activeCommand.NotifyRefusal(new CommandContext(_commandableBuffer[0], hit));

            _activeCommand = null;
        }

        private void HandleMouseRightButton(bool isPressed)
        {
            if (isPressed)
                return;

            // 끌어서 그은 선이면 그 우클릭은 이미 소비됐다
            if (_lineDrawer.TryCommit())
                return;

            // UI 위에서 놓은 우클릭은 UI 것이다. 안 막으면 지휘관 초상화를 눌러도 뒤의 땅으로 이동 명령이 나간다.
            // 여기는 입력 콜백 안이라 EventSystem에 직접 물으면 안 된다 — Update가 재 둔 값을 쓴다
            if (_pointerOverUI)
                return;

            _selectedBuffer.Clear();
            if (selectionController.SelectedUnits != null)
                foreach (ISelectable selectable in selectionController.SelectedUnits)
                    _selectedBuffer.Add(selectable);

            if (_selectedBuffer.Count == 0)
                return;

            if (!inputReader.GetMousePosition(out RaycastHit2D hit, floorLayer | interactableLayer))
                return;

            inputReader.GetMousePosition(out RaycastHit2D interactableHit, interactableLayer);
            int index = 0;

            foreach (ISelectable selectable in _selectedBuffer)
            {
                // 분대·지휘관도 AbstractCommandable이라 아래 범용 경로가 그대로 처리한다.
                // 건물은 우클릭 명령 대상이 아니다 — 클릭 선택 전용이다
                if (selectable is not AbstractCommandable commandable || commandable is IBuilding ||
                    commandable.Owner != selectionController.ControlOwner)
                    continue;

                RaycastHit2D targetHit = interactableHit.collider != null ? interactableHit : hit;
                CommandContext context = new CommandContext(commandable, targetHit, index, MouseButton.Right, hit.point);

                BaseCommandSO command = _rightClickResolver.Resolve(commandable, context);
                if (command == null)
                    continue;

                command.Handle(context);
                Bus<CommandIssuedEvent>.Raise(commandable.Owner, new CommandIssuedEvent(commandable.Owner, commandable, command));

                // 여러 유닛에 같은 명령이 나가도 소리는 한 번이면 된다 — 첫 대상에서만 낸다
                if (index == 0)
                    PlayOrderSound(command, hit.point);

                index++;
            }
        }

        /// <summary>
        /// 명령이 실제로 나갔음을 소리로 알립니다. 오디오가 씬에 없으면 조용히 넘어갑니다.
        /// </summary>
        private static void PlayOrderSound(BaseCommandSO command, Vector3 position)
        {
            if (CommandAudio.Instance != null)
                CommandAudio.Instance.PlayOrder(command.OrderSound, position);
        }

        /// <summary>
        /// 호버 커서를 이 오브젝트에 붙입니다. 고스트 미리보기와 같은 방식이라 씬 배선이 없습니다.
        /// </summary>
        private void AttachCursorDriver()
        {
            CursorContextResolver cursorResolver = new(
                inputReader, selectionController, _ghostPreviewManager,
                floorLayer, interactableLayer, selectableUnitLayer);

            cursorDriver.Initialize(cursorResolver);
        }

        public static Vector3 SnapToGrid(Vector3 worldPos, Vector2Int size)
        {
            return PathBaker.Instance == null ? worldPos : PathBaker.Instance.SnapToGrid(worldPos, size);
        }

        private static RaycastHit2D CreateHitAtPoint(RaycastHit2D original, Vector3 snappedPoint)
        {
            RaycastHit2D snappedHit = original;
            snappedHit.point = snappedPoint;
            return snappedHit;
        }
    }
}
