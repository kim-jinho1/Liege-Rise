using System.Collections.Generic;
using System.Linq;
using Code.Commandable;
using Code.Core.Bus;
using Code.GameEvents.Unit;
using Code.Military.Commanders;
using Code.Military.Squads;
using Code.UI.Components;
using Code.UI.Selection;
using Code.Units.Base;
using UnityEngine;

namespace Code.UI
{
    /// <summary>
    /// 상시 HUD 초기화와 단일 유닛/건물 선택 시 Selection UI 표시를 담당합니다.
    /// </summary>
    public class RuntimeUI : MonoBehaviour
    {
        [Header("Selection UI")]
        [SerializeField] private GameObject selectionRoot;
        [SerializeField] private GeneralInfoUI generalInfoUI;
        [SerializeField] private SelectionUI selectionUI;
        [SerializeField] private BuildingSelectedUI buildingSelectedUI;
        [SerializeField] private SquadSelectedUI squadSelectedUI;
        [SerializeField] private CommandPanel commandPanel;     // Slot 번호로 8칸을 채우는 공용 커맨드 패널. 대상 종류를 가리지 않는다

        [Header("Owner")]
        [SerializeField] private Owner owner = Owner.Player;

        [Header("Detail Section")]
        [SerializeField] private GameObject detailSlot;         // 기타 정보 칸들의 부모
        [SerializeField] private GameObject detailDivider;      // 기본 정보와 기타 정보 사이 구분선

        private readonly HashSet<AbstractCommandable> _selectedUnits = new(12);
        private readonly HashSet<Squad> _selectedSquads = new(8);
        private readonly List<AbstractUnit> _selectedUnitBuffer = new(12);
        private BusSubscriptionScope _subscriptionScope;

        private void Awake()
        {
            _subscriptionScope = new BusSubscriptionScope();
            // 선택 이벤트는 대상의 Owner 채널로 발행된다. 적을 클릭해도 이름·체력을 보여 주려면
            // 모든 채널을 받아야 한다 — 무엇을 보여 줄지는 ShowSingle/ShowSquad에서 소유자로 가른다.
            _subscriptionScope.SubscribeAll<UnitSelectEvent>(HandleUnitSelect);
            _subscriptionScope.SubscribeAll<UnitDeselectEvent>(HandleUnitDeselect);
            _subscriptionScope.SubscribeAll<UnitDeathEvent>(HandleUnitDeath);
            _subscriptionScope.SubscribeAll<SquadDeathEvent>(HandleSquadDeath);

            // 선택 이벤트가 한 번도 오지 않은 판 시작 시점에는 빈 패널이 떠 있으면 안 된다
            SetSelectionRootActive(false);
            SetDetailVisible(false);
        }

        private void OnDestroy()
        {
            _subscriptionScope?.Dispose();
        }

        /// <summary>선택 이벤트를 받아 선택 목록에 추가하고 UI를 갱신합니다.</summary>
        private void HandleUnitSelect(UnitSelectEvent evt)
        {
            if (evt.Unit is Squad squad)
                _selectedSquads.Add(squad);
            else if (evt.Unit is AbstractCommandable commandable)
                _selectedUnits.Add(commandable);

            RefreshSelectionUI();
        }

        /// <summary>선택 해제 이벤트를 받아 선택 목록에서 제거하고 UI를 갱신합니다.</summary>
        private void HandleUnitDeselect(UnitDeselectEvent evt)
        {
            bool removed = evt.Unit switch
            {
                Squad squad => _selectedSquads.Remove(squad),
                AbstractCommandable commandable => _selectedUnits.Remove(commandable),
                _ => false
            };

            // 모든 소유자를 구독하므로, 고르지도 않은 대상 때문에 패널을 다시 그리지 않는다
            if (removed)
                RefreshSelectionUI();
        }

        /// <summary>사망 이벤트를 받아 선택 목록에서 제거하고 UI를 갱신합니다.</summary>
        private void HandleUnitDeath(UnitDeathEvent evt)
        {
            if (_selectedUnits.Remove(evt.Unit))
                RefreshSelectionUI();
        }

        /// <summary>분대는 SquadDeathEvent로 사망을 알립니다. 목록에서 안 지우면 파괴된 분대를 계속 표시하려 듭니다.</summary>
        private void HandleSquadDeath(SquadDeathEvent evt)
        {
            if (evt.Squad is Squad squad && _selectedSquads.Remove(squad))
                RefreshSelectionUI();
        }

        /// <summary>
        /// 선택된 유닛/분대에 따라 적절한 하위 UI를 활성화합니다.
        /// </summary>
        private void RefreshSelectionUI()
        {
            if (_selectedUnits.Count == 0 && _selectedSquads.Count == 0)
            {
                DisableSelectionUI();
                return;
            }

            if (buildingSelectedUI == null || selectionUI == null)
                return;

            if (_selectedSquads.Count > 0)
            {
                ShowSquad(_selectedSquads.First());
                return;
            }

            if (squadSelectedUI != null)
                squadSelectedUI.Disable();
            if (_selectedUnits.Count == 1)
            {
                ShowSingle(_selectedUnits.First());
            }
            else
            {
                ShowMultiple();
                SetSelectionRootActive(false); // 다중 선택시 프로필만 보여주고 패널을 표시 하지 않습니다.
            }
        }

        /// <summary>
        /// 분대 패널을 엽니다. 분대 선택은 유닛·건물보다 우선합니다.
        /// 분대도 공용 컨테이너를 쓰므로 컨테이너를 닫으면 분대 정보까지 같이 사라집니다.
        /// </summary>
        private void ShowSquad(Squad squad)
        {
            if (squad.Owner != owner)
            {
                ShowForeign(squad);
                return;
            }

            SetSelectionRootActive(true);

            selectionUI.Disable();
            buildingSelectedUI.Disable();
            if (generalInfoUI != null)
                generalInfoUI.Bind(squad); // 이름·아이콘·체력(분대 총합)은 공용 패널이 전담
            if (squadSelectedUI != null)
                squadSelectedUI.EnableFor(squad);
            if (commandPanel != null)
                commandPanel.EnableFor(squad);
            SetDetailVisible(squadSelectedUI != null); // 분대는 인원·대형이 언제나 붙는다
        }

        /// <summary>
        /// 유닛·건물 하나를 표시합니다. 건물이면 패널을 대상 옆으로 옮깁니다.
        /// </summary>
        private void ShowSingle(AbstractCommandable selected)
        {
            if (selected.Owner != owner)
            {
                ShowForeign(selected);
                return;
            }

            SetSelectionRootActive(true);

            // 아이콘·이름·체력은 공용 정보 패널이 전담하고, 고유 패널은 자기 영역만 표시
            selectionUI.Disable();
            if (generalInfoUI != null)
                generalInfoUI.Bind(selected);
            if (commandPanel != null)
                commandPanel.EnableFor(selected);

            // 지휘관은 분대 패널을 빌려 거느린 분대를 카드로 보여 준다
            if (selected is Commander commander && squadSelectedUI != null)
            {
                buildingSelectedUI.Disable();
                squadSelectedUI.EnableForCommander(commander);
                SetDetailVisible(true);
                return;
            }

            if (squadSelectedUI != null)
                squadSelectedUI.Disable();
            buildingSelectedUI.EnableFor(selected);

            // 건물 종류에 따라 기타 정보가 없을 수 있다. 켠 쪽에 물어본다
            SetDetailVisible(buildingSelectedUI.HasDetail);
        }

        /// <summary>
        /// 남의 것을 선택했을 때 이름·체력만 보여 줍니다.
        /// 생산 대기열·분대원 칸·커맨드는 조작할 수 없는 정보라 열지 않습니다.
        /// </summary>
        private void ShowForeign(AbstractCommandable target)
        {
            SetSelectionRootActive(true);

            selectionUI.Disable();
            buildingSelectedUI.Disable();
            if (squadSelectedUI != null)
                squadSelectedUI.Disable();
            if (commandPanel != null)
                commandPanel.Disable();
            if (generalInfoUI != null)
                generalInfoUI.Bind(target);
            SetDetailVisible(false);
        }

        /// <summary>
        /// 여러 유닛을 한 줄로 표시합니다. 공용 정보 패널도, 배경 종이도 쓰지 않습니다.
        /// </summary>
        private void ShowMultiple()
        {
            SetSelectionRootActive(true);

            if (generalInfoUI != null)
                generalInfoUI.Unbind();
            buildingSelectedUI.Disable();
            SetDetailVisible(false); // 다중 선택은 기타 정보를 쓰지 않는다
            if (commandPanel != null)
                commandPanel.EnableForMany(_selectedUnits); // 전원이 가진 커맨드만 뜬다
            CollectSelectedUnits();

            if (_selectedUnitBuffer.Count > 0)
            {
                selectionUI.EnableForMultiple(_selectedUnitBuffer);
                return;
            }

            // 건물만 여러 개 고른 경우 — 그릴 것이 없으므로 컨테이너째 닫는다
            selectionUI.Disable();
            SetSelectionRootActive(false);
        }

        /// <summary>
        /// 선택된 유닛을 버퍼에 담습니다. 다중 선택 시 UI에 전달할 때 사용합니다.
        /// </summary>
        private void CollectSelectedUnits()
        {
            _selectedUnitBuffer.Clear();
            foreach (AbstractCommandable commandable in _selectedUnits)
                if (commandable is AbstractUnit unit)
                    _selectedUnitBuffer.Add(unit);
        }

        /// <summary>
        /// 선택 UI를 모두 비활성화하고 공용 정보 패널의 바인딩을 해제합니다.
        /// </summary>
        private void DisableSelectionUI()
        {
            if (generalInfoUI != null)
                generalInfoUI.Unbind();
            if (commandPanel != null)
                commandPanel.Disable();
            SetSelectionRootActive(false);
            SetDetailVisible(false);

            if (buildingSelectedUI == null || selectionUI == null)
                return;

            selectionUI.Disable();
            buildingSelectedUI.Disable();
            if (squadSelectedUI != null)
                squadSelectedUI.Disable();
        }

        /// <summary>
        /// 선택 패널 컨테이너를 열고 닫습니다. 하위 패널들은 각자 자기 영역만 켜므로
        /// 이 부모가 꺼져 있으면 무엇을 켜도 화면에 나오지 않습니다.
        /// </summary>
        private void SetSelectionRootActive(bool active)
        {
            if (selectionRoot != null && selectionRoot.activeSelf != active)
                selectionRoot.SetActive(active);
        }

        /// <summary>
        /// 기타 정보 칸과 그 앞의 구분선을 함께 켜고 끕니다.
        /// 빈 칸을 켜 둔 채로 두면 레이아웃이 간격만큼 넓어져 패널이 한쪽으로 쏠립니다.
        /// </summary>
        private void SetDetailVisible(bool visible)
        {
            if (detailSlot != null && detailSlot.activeSelf != visible)
                detailSlot.SetActive(visible);

            if (detailDivider != null && detailDivider.activeSelf != visible)
                detailDivider.SetActive(visible);
        }
    }
}
