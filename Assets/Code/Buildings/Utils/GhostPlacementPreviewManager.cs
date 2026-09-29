using Code.Buildings.Construction;
using Code.Commands.Interfaces;
using Code.Commands.Types.Production;
using Code.Core.Input;
using Code.Players;
using UnityEngine;

namespace Code.Buildings.Utils
{
    /// <summary>
    /// 건물 건설 시 고스트(미리보기)의 생성, 이동, 유효성 검사를 관리하는 컴포넌트입니다.
    /// </summary>
    public class GhostPlacementPreviewManager : MonoBehaviour
    {
        private GhostPlacement _ghostInstance;
        private IConstructionCommand _activeCommand;
        private InputReader _inputReader;
        private SelectionController _selectionController;
        private LayerMask _floorLayer;

        public bool IsActive
            => _ghostInstance != null;

        /// <summary>
        /// 지금 고스트 자리가 배치 가능한지입니다. 고스트가 없으면 값이 의미 없습니다.
        /// </summary>
        public bool IsPlacementValid { get; private set; }

        public void Initialize(InputReader inputReader, SelectionController selectionController, LayerMask floorLayer)
        {
            _inputReader = inputReader;
            _selectionController = selectionController;
            _floorLayer = floorLayer;
        }

        public void ShowGhost(IConstructionCommand command)
        {
            DestroyGhost();
            _activeCommand = command;

            if (command.GhostPrefab == null)
                return;

            GameObject instance = Instantiate(command.GhostPrefab);
            _ghostInstance = instance.GetComponent<GhostPlacement>();
            Debug.Assert(_ghostInstance != null, "GhostPrefab에 GhostPlacement 컴포넌트가 없습니다.");

            BuildingFootprintIndicator indicator = instance.AddComponent<BuildingFootprintIndicator>();
            indicator.Initialize(command.BuildingSize);

            // 놓는 동안에는 이미 선 건물들의 발자국도 보여 준다 — 어디가 찼는지 모르고 놓으면
            // 배치 실패의 이유가 화면에 없다
            BuildingFootprintIndicator.ShowAll(true);
        }

        public void UpdatePreview()
        {
            if (_ghostInstance == null)
                return;

            // ESC 취소는 CommandExecutor가 단독으로 처리한다.
            // 여기서 고스트만 지우면 커맨드가 살아남아 다음 좌클릭에 미리보기 없이 실행된다

            if (!_inputReader.GetMousePosition(out RaycastHit2D hit, _floorLayer))
                return;

            Vector3 snappedPos = CommandExecutor.SnapToGrid(hit.point, _activeCommand.BuildingSize);
            _ghostInstance.transform.position = snappedPos;

            bool restrictionPass = _activeCommand.AllRestrictionPass(snappedPos, _activeCommand.BuildingSize);
            bool hasSupplies = true;

            if (_activeCommand is ConstructionBuildingCommandSO buildingCmd && UserSupplies.Instance != null)
                hasSupplies = UserSupplies.Instance.HasEnoughSupplies(
                    _selectionController.ControlOwner, buildingCmd.BuildingSO.Cost);

            IsPlacementValid = restrictionPass && hasSupplies;
            _ghostInstance.SetPassRestriction(IsPlacementValid);
        }

        public void DestroyGhost()
        {
            // 고스트보다 먼저 끈다. _forceVisible은 static이라, 고스트를 든 채로 씬이 내려가
            // 이 참조가 죽으면 아래 early return에 걸려 다음 판 내내 발자국이 전부 켜진 채로 남는다
            BuildingFootprintIndicator.ShowAll(false);

            if (_ghostInstance == null)
                return;

            Destroy(_ghostInstance.gameObject);
            _ghostInstance = null;
            _activeCommand = null;
        }
    }
}
