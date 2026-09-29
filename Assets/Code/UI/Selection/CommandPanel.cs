using System.Collections.Generic;
using Code.Buildings.Logic.ProductionBuilding;
using Code.Buildings.Logic.Shared;
using Code.Commandable;
using Code.Commands.Base;
using Code.Commands.Types.Production;
using Code.Core.Bus;
using Code.Core.Input;
using Code.Economics;
using Code.GameEvents.Economy;
using Code.Military.Commands;
using Code.Military.Data;
using Code.Military.Squads;
using Code.UI.Components;
using UnityEngine;

namespace Code.UI.Selection
{
    /// <summary>
    /// 선택 대상의 커맨드를 Slot 번호대로 고정 칸에 배치하는 공용 패널입니다.
    /// 커맨드 타입으로 분기하지 않으므로 새 커맨드는 에셋만 만들면 나타납니다.
    /// 칸은 비어 있어도 자리를 지킵니다 — 대상이 바뀌어도 버튼 위치가 흔들리지 않게 하기 위함입니다.
    /// </summary>
    public class CommandPanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject panel;
        [SerializeField] private BuildUnitButtonUI[] slots;  // 배열 인덱스가 곧 Slot 번호다. 0번 칸부터 순서대로 넣을 것
        [SerializeField] private BuildTooltip tooltip; // 칸이 공유하는 호버 툴팁이다. 건설 패널이 쓰는 것을 그대로 연결해도 된다
        [SerializeField] private Owner uiOwner = Owner.Player;
        [SerializeField] private InputReader inputReader;   // 텍스트 입력 중 단축키를 막기 위해 본다

        private readonly SharedCommandSet _shared = new();
        private BusSubscriptionScope _scope;

        /// <summary>
        /// 선택 대상이 상주 인원을 가진다면 배치·회수 직후 버튼 잠금이 바로 풀리도록 구독해 둡니다.
        /// </summary>
        private PawnStationHost _watchedHost;

        /// <summary>
        /// 자원이 변하면 칸의 구매 가능 여부가 달라지므로 구독해 둡니다.
        /// </summary>
        private void Awake()
        {
            _scope = new BusSubscriptionScope();
            _scope.Subscribe<SupplyEvent>(uiOwner, HandleSupplyChanged);

#if UNITY_EDITOR
            // 툴팁은 씬에만 있어 통째로 지워져도 컴파일이 통과한다. 실제로 한 번 사라져 호버가 조용히 죽었다
            if (tooltip == null)
                Debug.LogWarning($"[{name}] Tooltip이 배선되지 않아 칸에 호버해도 아무것도 뜨지 않습니다.", this);
#endif

            if (slots == null)
                return;

            RectTransform anchor = transform as RectTransform;
            for (int i = 0; i < slots.Length; i++)
            {
                BuildUnitButtonUI slot = slots[i];
                if (slot == null)
                    continue;

                slot.SetTooltip(tooltip);

                // 아랫줄 칸에 호버해도 윗줄을 가리지 않도록 패널 전체를 기준으로 삼는다
                slot.SetTooltipAnchor(anchor);
            }
        }

        private void OnDestroy()
        {
            Unwatch();
            _scope?.Dispose();
        }

        /// <summary>
        /// 대상의 상주 인원 변화를 구독합니다. 배치·회수 직후 버튼 잠금이 바로 풀리도록 하기 위함입니다.
        /// </summary>
        private void Watch(AbstractCommandable target)
        {
            Unwatch();

            if (target == null)
                return;

            PawnStationHost host = target.GetComponent<PawnStationHost>();
            if (host == null)
                return;

            _watchedHost = host;
            _watchedHost.CountChanged += Refresh;
        }

        private void Unwatch()
        {
            if (_watchedHost == null)
                return;

            _watchedHost.CountChanged -= Refresh;
            _watchedHost = null;
        }

        /// <summary>
        /// 대상 하나의 커맨드를 칸에 배치합니다.
        /// </summary>
        public void EnableFor(AbstractCommandable target)
        {
            if (target == null)
            {
                Disable();
                return;
            }

            Fill(target, target.AvailableCommands);
        }

        /// <summary>
        /// 여러 대상을 선택했을 때 전원이 가진 커맨드만 칸에 배치합니다.
        /// 공유하는 커맨드가 없으면 칸은 전부 빈 채로 남습니다.
        /// </summary>
        public void EnableForMany(IReadOnlyCollection<AbstractCommandable> targets)
        {
            AbstractCommandable first = SharedCommandSet.First(targets);
            if (first == null)
            {
                Disable();
                return;
            }

            _shared.Collect(targets, first);

            // 실행 대상은 대표 하나다. 선택 전체에 일괄 적용하는 것은 별도 작업이다
            Fill(first, _shared.Commands);
        }

        /// <summary>
        /// 패널을 닫고 모든 칸을 비웁니다.
        /// </summary>
        public void Disable()
        {
            ClearSlots();
            Unwatch();

            // 패널이 닫히면 어느 칸이 띄웠든 툴팁은 남으면 안 된다
            tooltip?.ForceHide();

            if (panel != null)
                panel.SetActive(false);
        }

        /// <summary>
        /// 지금 꽂혀 있는 커맨드 중 조건에 맞는 첫 칸의 사각형입니다. 패널이 닫혀 있으면 false.
        /// 시퀀스 안내가 "Q 칸"·"농장 칸"을 태그 없이 찾을 때 씁니다.
        /// </summary>
        public bool TryFindSlot(System.Func<BaseCommandSO, bool> predicate, out RectTransform rect)
        {
            rect = null;
            if (slots == null || panel == null || !panel.activeInHierarchy)
                return false;

            foreach (BuildUnitButtonUI slot in slots)
            {
                if (slot == null || slot.Command == null || !slot.isActiveAndEnabled || !predicate(slot.Command))
                    continue;

                rect = slot.transform as RectTransform;
                return rect != null;
            }

            return false;
        }

        /// <summary>
        /// 칸의 잠금 상태만 다시 계산합니다. 배치는 건드리지 않습니다.
        /// </summary>
        public void Refresh()
        {
            if (slots == null)
                return;

            foreach (BuildUnitButtonUI slot in slots)
                slot?.Refresh();
        }

        /// <summary>
        /// 커맨드 목록을 Slot 번호대로 칸에 꽂습니다. Slot이 음수인 것은 우클릭 전용이라 제외합니다.
        /// </summary>
        private void Fill(AbstractCommandable target, IReadOnlyList<BaseCommandSO> commands)
        {
            ClearSlots();
            Watch(target);

            if (panel != null)
                panel.SetActive(true);

            if (commands == null || slots == null)
                return;

            foreach (BaseCommandSO command in commands)
            {
                // 프리팹에 사라진 에셋을 가리키는 칸이 남아 있어 null로 역직렬화된다
                if (command == null)
                    continue;

                if (command.Slot < 0 || !command.IsUnlockedByProgress())
                    continue;

                if (command.Slot >= slots.Length)
                {
                    Debug.LogWarning(
                        $"{name}: '{command.name}'의 Slot {command.Slot}이 칸 수({slots.Length})를 넘어 표시되지 않습니다.", this);

                    continue;
                }

                BuildUnitButtonUI slot = slots[command.Slot];
                if (slot == null)
                    continue;

                slot.EnableFor(command, target, CostOf(command, target));
                slot.SetHotkeyLabel(CommandHotkeys.LabelForSlot(command.Slot));
            }
        }

        /// <summary>
        /// 커맨드가 표시할 자원 비용입니다. 없으면 null이고 칸에 비용이 뜨지 않습니다.
        /// </summary>
        private static ResourceCostSO CostOf(BaseCommandSO command, AbstractCommandable target)
            => command switch
            {
                // 보충은 커맨드가 아니라 분대원 하나의 값을 쓴다
                ReinforceSquadCommandSO when target is Squad squad
                    => (squad.AbstractCommandableSO as SquadSO)?.MemberSO?.Cost,

                // 병영은 생산할 분대에 따라 값이 달라진다
                BuildSquadCommandSO squadCommand when target is SquadRecruitmentBuilding barracks
                    => barracks.GetCost(squadCommand.Squad),

                BuildCommanderCommandSO commanderCommand when target is SquadRecruitmentBuilding barracks
                    => barracks.GetCost(commanderCommand.Commander),

                _ => ConstructionItemUI.ResolveTarget(command)?.Cost
            };

        private void ClearSlots()
        {
            if (slots == null)
                return;

            foreach (BuildUnitButtonUI slot in slots)
            {
                slot?.Disable();
                slot?.SetHotkeyLabel(null); // 빈 칸에 키만 떠 있으면 눌러도 안 되는 키로 보인다
            }
        }

        private void Update()
        {
            if (panel != null && !panel.activeSelf)
                return;

            // 이름을 입력하는 중이면 단축키가 글자를 먹는다
            if (inputReader != null && inputReader.IsTextInputActive)
                return;

            CommandHotkeys.Poll(slots);
        }

        private void HandleSupplyChanged(SupplyEvent _)
            => Refresh();
    }
}
