using System;
using System.Collections.Generic;
using System.Linq;
using Code.Commandable.Interfaces;
using Code.Core;
using Code.Core.Bus;
using Code.Core.Input;
using Code.GameEvents.Unit;
using Code.Units.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Code.Players
{
    public class SelectionController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private InputReader inputReader;
        [SerializeField] private CommandExecutor commandExecutor; // 커맨드 활성화 여부 확인용

        [Header("Selection Settings")]
        [SerializeField] private LayerMask selectableUnitLayer;

        [Header("Grid Selection Box")]
        [SerializeField] private float dragThreshold = 8f;

        [Space] [Header("Settings")]
        [SerializeField] private Owner controlOwner = Owner.Player;

        private readonly HashSet<ISelectable> _selectedUnits = new(12);
        private readonly HashSet<AbstractUnit> _aliveUnits = new(200);
        private readonly HashSet<ISelectable> _addedUnits = new(24);

        private readonly List<Collider2D> _overlapResults = new(64);
        private ContactFilter2D _selectionFilter;
        private SelectionQuery _query;

        private readonly MouseButtonStateTracker _buttons = new();

        // 필드 초기화자는 MonoBehaviour 생성자에서 돈다 — probe가 LayerMask를 읽으므로 Awake에서 만든다
        private ResourceHoverProbe _hoverProbe;
        private readonly CommandableHoverProbe _commandableHoverProbe = new();
        private Func<ISelectable> _resolveHover;

        private Vector2 _startingScreenPos;
        private Vector3 _startingWorldPos;
        private bool _wasMouseDownOnUI;
        private Camera _camera;
        private BusSubscriptionScope _subscriptionScope;
        private GridSelectionBoxView _selectionBoxView;

        private GridSelectionBoxView SelectionBoxView
            => _selectionBoxView != null ? _selectionBoxView : _selectionBoxView = GridSelectionBoxView.Instance;

        // CommandExecutor에서 접근할 수 있도록 프로퍼티 개방
        public IEnumerable<ISelectable> SelectedUnits => _selectedUnits;
        public IEnumerable<AbstractUnit> AliveUnits => _aliveUnits;
        public Owner ControlOwner => controlOwner;

        /// <summary>
        /// 현재 선택된 유닛/건물이 하나라도 있으면 true입니다.
        /// </summary>
        public bool HasSelection => _selectedUnits.Count > 0;

        private void Awake()
        {
            _camera = Camera.main;
            _hoverProbe = new ResourceHoverProbe();
            _query = new SelectionQuery(_camera, selectableUnitLayer, controlOwner);
            _resolveHover = PreferredUnderCursor; // 프레임마다 델리게이트를 새로 만들지 않는다

            _selectionFilter.SetLayerMask(selectableUnitLayer);
            _selectionFilter.useTriggers = true;   // 클릭 판정과 기준을 맞춘다

            _subscriptionScope = new BusSubscriptionScope();
            _subscriptionScope.SubscribeAll<UnitSelectEvent>(HandleUnitSelected);
            _subscriptionScope.SubscribeAll<UnitDeselectEvent>(HandleUnitDeselected);
            _subscriptionScope.Subscribe<UnitSpawnEvent>(controlOwner, HandleUnitSpawn);
            _subscriptionScope.Subscribe<UnitDeathEvent>(controlOwner, HandleUnitDeath);
        }

        private void OnDestroy()
        {
            _subscriptionScope?.Dispose();
            _hoverProbe?.Dispose();
            _commandableHoverProbe.Dispose();
        }

        private void Update()
        {
            // 입력 디바이스가 없으면(포커스 상실·디바이스 미연결) 프레임을 건너뜁니다.
            if (Mouse.current == null)
                return;

            _buttons.Update();
            HandleDragSelect();

            bool hoverSuppressed = _buttons.State == ButtonState.Held || IsBlocked || IsPointerOverUI();
            _hoverProbe.Update(_camera, hoverSuppressed);

            // 클릭이 무엇을 고를지 판정하는 그 함수를 그대로 넘긴다 — 호버와 클릭이 갈라지지 않는다.
            // 언제 다시 볼지는 프로브가 정한다
            _commandableHoverProbe.Update(hoverSuppressed || _camera == null,
                Mouse.current.position.ReadValue(), _resolveHover);
        }

        /// <summary>
        /// 현재 포인터가 UI 위에 있는지 안전하게 확인합니다. (EventSystem 미생성 시 false)
        /// </summary>
        private static bool IsPointerOverUI()
            => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private void HandleDragSelect()
        {
            switch (_buttons.State)
            {
                case ButtonState.Pressed:
                    HandleDragStart();
                    break;
                case ButtonState.Held:
                    HandleMouseDrag();
                    break;
                case ButtonState.Released:
                    HandleDragEnd();
                    break;
            }
        }

        private void HandleDragStart()
        {
            _startingScreenPos = inputReader.MousePosition;
            _startingWorldPos = _camera.ScreenToWorldPoint(_startingScreenPos);
            _startingWorldPos.z = 0f;
            _addedUnits.Clear();
            HideBox();
            _wasMouseDownOnUI = IsPointerOverUI();
        }

        private void HandleMouseDrag()
        {
            if (IsBlocked || _wasMouseDownOnUI)
                return;

            Vector2 mousePosition = inputReader.MousePosition;

            if (Vector2.Distance(_startingScreenPos, mousePosition) < dragThreshold)
            {
                _addedUnits.Clear();
                HideBox();
                return;
            }

            Vector3 currentWorldPos = _camera.ScreenToWorldPoint(mousePosition);
            currentWorldPos.z = 0f;
            _query.SnapToCells(_startingWorldPos, currentWorldPos, out Vector3 worldMin, out Vector3 worldMax);
            if (SelectionBoxView != null)
                SelectionBoxView.Show(worldMin, worldMax);

            _addedUnits.Clear();
            _query.CollectInBox(worldMin, worldMax, _selectionFilter, _overlapResults, _addedUnits);
        }

        private bool IsBlocked
            => commandExecutor.HasActiveCommand || inputReader.IsTextInputActive;

        private void HandleDragEnd()
        {
            bool shiftHeld = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
            if (!_wasMouseDownOnUI && !IsBlocked && !shiftHeld)
                DeselectAllUnits();

            HandleMouseLeftButton();

            foreach (ISelectable selectable in _addedUnits)
                selectable.Select();

            HideBox();
        }

        /// <summary>
        /// 선택 박스 시각화를 숨깁니다.
        /// </summary>
        private void HideBox()
        {
            if (SelectionBoxView != null)
                SelectionBoxView.Hide();
        }

        /// <summary>
        /// 현재 선택된 모든 유닛을 해제합니다.
        /// </summary>
        public void DeselectAllUnits()
        {
            foreach (ISelectable selectable in _selectedUnits.ToArray())
                selectable.DeSelect();
        }

        private void HandleMouseLeftButton()
        {
            if (_wasMouseDownOnUI)
                return;

            // 단일 선택 처리 (커맨드 또는 Route 생성 모드가 활성화되지 않았을 때만)
            if (IsBlocked || _addedUnits.Count != 0)
                return;

            ISelectable picked = PreferredUnderCursor();
            if (UnityRef.IsAlive(picked))
                picked.Select();
        }

        /// <summary>
        /// 커서 아래에서 고를 대상 하나입니다. 클릭과 호버가 이 답을 함께 씁니다.
        /// </summary>
        private ISelectable PreferredUnderCursor()
            => _query.PreferredAt(Mouse.current.position.ReadValue());

        private void HandleUnitSelected(UnitSelectEvent evt)
            => _selectedUnits.Add(evt.Unit);

        private void HandleUnitDeselected(UnitDeselectEvent evt)
            => _selectedUnits.Remove(evt.Unit);

        private void HandleUnitSpawn(UnitSpawnEvent evt)
            => _aliveUnits.Add(evt.Unit);

        private void HandleUnitDeath(UnitDeathEvent evt)
        {
            _aliveUnits.Remove(evt.Unit);
            _selectedUnits.Remove(evt.Unit);
        }

    }
}
