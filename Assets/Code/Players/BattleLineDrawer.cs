using System.Collections.Generic;
using Code.Commandable.Interfaces;
using Code.Core.Input;
using Code.Military.Commanders;
using Code.Military.Data;
using Code.Military.Squads;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Code.Players
{
    /// <summary>
    /// 지휘관을 선택한 채 우클릭 드래그하면 공세선을 긋습니다. Tab은 끄는 중에도, 그은 뒤에도 방향을 뒤집습니다.
    /// 누른 자리에서 조금이라도 끌어야 선이고, 끌지 않은 우클릭은 CommandExecutor의 커맨드 그대로입니다.
    /// 놓는 순간의 판정은 CommandExecutor가 TryCommit으로 묻습니다 — 입력 콜백이 Update보다 먼저 와서
    /// 여기서 놓임을 감지하면 커맨드가 먼저 나가 버립니다.
    /// </summary>
    public class BattleLineDrawer : MonoBehaviour
    {
        private const float DragThresholdPixels = 10f;

        private InputReader _inputReader;
        private SelectionController _selection;
        private Camera _camera;
        private CameraController _cameraController;
        private SquadMarkerStyleSO _style;
        private BattleLineView _preview;
        private readonly BattleFront _front = new();
        private readonly Vector2[] _frontPoints = new Vector2[BattleFront.MaxPoints];
        private readonly List<Commander> _commanders = new(8);

        private bool _pressed;
        private bool _dragging;
        private bool _flipped;
        private Vector2 _startScreen;
        private Vector2 _startWorld;
        private Vector2 _endWorld;

        public bool IsDragging => _dragging;

        public void Initialize(InputReader inputReader, SelectionController selection)
        {
            _inputReader = inputReader;
            _selection = selection;
            _camera = Camera.main;
            _style = SquadMarkerStyleSO.Load();
            _preview = BattleLineView.Create("__BattleLinePreview");
        }

        private void OnDestroy()
        {
            if (_preview != null)
                Destroy(_preview.gameObject);

            SetEdgePanSuppressed(false);
        }

        // 끌다가 마우스가 화면 끝에 닿으면 카메라가 밀리며 놓는 자리가 달아난다. 긋는 동안만 가장자리 이동을 잠근다
        private void SetEdgePanSuppressed(bool suppressed)
        {
            if (_camera == null || !_camera.isActiveAndEnabled)
                _camera = Camera.main;
            if (_camera == null)
                return;

            // 카메라는 스테이지 씬마다 바뀌므로 ToWorld처럼 게으르게 잡는다. 드래그 시작·끝에만 부른다
            if (_cameraController == null || _cameraController.gameObject != _camera.gameObject)
                _cameraController = _camera.GetComponent<CameraController>();

            if (_cameraController != null)
                _cameraController.EdgePanSuppressed = suppressed;
        }

        private void Update()
        {
            if (Mouse.current == null || _inputReader == null)
                return;

            ButtonControl right = Mouse.current.rightButton;
            if (right.wasPressedThisFrame)
                BeginPress();
            else if (right.isPressed && _pressed)
                UpdateDrag();
            else if (!_pressed)
                FlipDrawnLines();
        }

        /// <summary>
        /// 다 긋고 나서 누른 Tab입니다. 끄는 동안에만 먹으면 방향을 잘못 잡았을 때 처음부터 다시 그어야 합니다.
        /// </summary>
        private void FlipDrawnLines()
        {
            if (_inputReader.IsTextInputActive)
                return;

            if (Keyboard.current == null || !Keyboard.current.tabKey.wasPressedThisFrame)
                return;

            CollectSelectedCommanders();
            for (int i = 0; i < _commanders.Count; i++)
                if (_commanders[i] != null && _commanders[i].HasLine)
                    _commanders[i].SetLine(_commanders[i].Line.Flip());
        }

        /// <summary>
        /// 우클릭을 놓았을 때 부릅니다. 끌고 있었으면 선을 넘기고 true — 그 우클릭은 커맨드로 가면 안 됩니다.
        /// </summary>
        public bool TryCommit()
        {
            if (!_pressed)
                return false;

            bool wasDragging = _dragging;
            _pressed = false;
            _dragging = false;
            _preview.Hide();
            SetEdgePanSuppressed(false);

            if (!wasDragging)
                return false;

            BattleLine line = MakeLine();
            BindSelectedSquads();
            for (int i = 0; i < _commanders.Count; i++)
                if (_commanders[i] != null)
                    _commanders[i].SetLine(line);

            return true;
        }

        /// <summary>
        /// 같이 담아 그었으면 그 분대들의 선입니다 — 임명·배속 단계를 따로 두지 않습니다.
        /// 첫 지휘관이 받고, 다른 지휘관 밑에 있던 분대는 Assign이 옮겨 옵니다.
        /// </summary>
        private void BindSelectedSquads()
        {
            if (_commanders.Count == 0 || _commanders[0] == null)
                return;

            foreach (ISelectable selectable in _selection.SelectedUnits)
            {
                if (selectable is Squad squad && !squad.IsDead && squad.Owner == _selection.ControlOwner)
                    _commanders[0].Roster.Assign(squad);
            }
        }

        private void BeginPress()
        {
            _pressed = false;
            _dragging = false;

            if (_inputReader.IsTextInputActive || _camera == null)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            CollectSelectedCommanders();
            if (_commanders.Count == 0)
                return;

            _pressed = true;
            _flipped = false;
            _startScreen = Mouse.current.position.ReadValue();
            _startWorld = ToWorld(_startScreen);
        }

        private void UpdateDrag()
        {
            Vector2 screen = Mouse.current.position.ReadValue();

            if (!_dragging)
            {
                if (Vector2.Distance(_startScreen, screen) < DragThresholdPixels)
                    return;

                _dragging = true;
                SetEdgePanSuppressed(true);
            }

            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
                _flipped = !_flipped;

            _endWorld = ToWorld(screen);
            BattleLine line = MakeLine();
            if (!line.IsValid)
                return;

            // 미리보기도 분대 무리에서 출발한다 — 첫 지휘관의 분대 무게중심이 출발점이다
            bool hasOrigin = _commanders[0] != null
                             && _front.Build(_commanders[0].Roster.Squads, line.Along, _frontPoints) > 0;
            // 미리보기에는 구간 라벨을 붙이지 않는다 — 긋는 동안 글자가 따라다니면 어지럽다
            _preview.Show(line, 0f, _style.LinePreviewColor, _front.Centroid, hasOrigin);
        }

        /// <summary>
        /// 선택 중인 아군 지휘관만 모읍니다. 지휘관이 하나도 없으면 우클릭은 평소 커맨드입니다.
        /// </summary>
        private void CollectSelectedCommanders()
        {
            _commanders.Clear();
            foreach (ISelectable selectable in _selection.SelectedUnits)
            {
                if (selectable is Commander commander && !commander.IsDead
                    && commander.Owner == _selection.ControlOwner)
                    _commanders.Add(commander);
            }
        }

        /// <summary>
        /// 지금 끈 선입니다. 기본 방향은 지휘관에게서 멀어지는 쪽이고 Tab이 그것을 뒤집습니다 —
        /// 수직 방향을 점 순서로 정하면 어느 쪽에서 끌었느냐에 따라 매번 Tab을 눌러야 합니다.
        /// </summary>
        private BattleLine MakeLine()
        {
            BattleLine raw = new(_startWorld, _endWorld, false);
            bool towardCommander = _commanders.Count > 0 && _commanders[0] != null
                && Vector2.Dot(raw.Normal, raw.Center - (Vector2)_commanders[0].transform.position) < 0f;

            return new BattleLine(_startWorld, _endWorld, towardCommander != _flipped);
        }

        /// <summary>
        /// 화면 좌표를 월드로 옮깁니다.
        /// 카메라를 한 번 캐시해 두면 안 됩니다. _Core가 먼저 서고 스테이지가 additive로 얹히므로,
        /// Initialize 시점의 Camera.main은 스테이지 카메라가 아닐 수 있습니다(부트 카메라 등).
        /// 그걸 붙들면 화면 어디를 끌든 선이 엉뚱한 자리에 그려집니다.
        /// </summary>
        private Vector2 ToWorld(Vector2 screen)
        {
            if (_camera == null || !_camera.isActiveAndEnabled)
                _camera = Camera.main;

            return _camera != null ? (Vector2)_camera.ScreenToWorldPoint(screen) : screen;
        }
    }
}
