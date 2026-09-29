using Code.Buildings.Base;
using Code.Buildings.Construction;
using Code.Buildings.Logic.EconomyBuilding;
using Code.Buildings.Logic.ProductionBuilding;
using Code.Commandable;
using UnityEngine;

namespace Code.UI.Selection
{
    /// <summary>
    /// 건물 선택 시 상태에 따라 하위 패널을 분기합니다.
    /// 커맨드 버튼은 CommandPanel이 Slot 번호대로 전담하므로 여기서 다루지 않습니다.
    /// 아이콘·이름·체력은 공용 정보 패널(GeneralInfoUI)이 전담합니다.
    /// </summary>
    public class BuildingSelectedUI : MonoBehaviour, IUIElement<AbstractCommandable>
    {
        [Header("UI Elements")]
        [SerializeField] private GenerateUnitUI generateUnitUI;
        [SerializeField] private UnderConstructionUI underConstructionUI;
        [SerializeField] private PawnStationUI pawnStationUI;

        /// <summary>
        /// 기타 정보 칸에 실제로 표시한 것이 있는지 여부입니다. EnableFor 직후에 유효합니다.
        /// 건설 진행 바는 기본 정보 블록에 있으므로 여기 포함되지 않습니다.
        /// </summary>
        public bool HasDetail { get; private set; }

        /// <summary>
        /// 선택된 대상에 따라 적절한 하위 UI를 활성화합니다.
        /// </summary>
        public void EnableFor(AbstractCommandable commandable)
        {
            CleanupPrevious();

            switch (commandable)
            {
                // 분대 병영: 대기 목록·진행 바 (진행도는 폰 도착 기준)
                case SquadRecruitmentBuilding squadBarracks:
                    generateUnitUI?.EnableFor(squadBarracks);
                    break;

                // 농장: 상주 일꾼 수 표시
                case FarmBuilding farm:
                    pawnStationUI?.EnableFor(farm);
                    break;

                // 건설 더미: 건설 진행 UI
                case ConstructionDummy dummy:
                    underConstructionUI?.EnableFor(dummy);
                    break;

                // 그 밖의 건물(본성·집 등)은 공용 생산 큐를 쓴다. 폰이 여기서 나온다 — 큐가 비면 패널이 스스로 숨는다
                case AbstractBuilding building:
                    generateUnitUI?.EnableFor(building);
                    break;
            }

            // 하위 패널이 스스로 뜨지 않기로 정할 수 있어(상주 인원 없음·생산 정보 없음) 결과를 되묻는다
            HasDetail = (generateUnitUI != null && generateUnitUI.IsShowing)
                        || (pawnStationUI != null && pawnStationUI.IsShowing);
        }

        /// <summary>
        /// 하위 패널을 모두 숨깁니다.
        /// </summary>
        public void Disable()
            => CleanupPrevious();

        private void CleanupPrevious()
        {
            HasDetail = false;
            generateUnitUI?.Disable();
            underConstructionUI?.Disable();
            pawnStationUI?.Disable();
        }
    }
}
