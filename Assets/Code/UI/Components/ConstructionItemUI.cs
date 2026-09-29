using Code.Commandable;
using Code.Commands.Base;
using Code.Commands.Types.Production;
using Code.Core;
using Code.Core.Bus;
using Code.Economics;
using Code.GameEvents.System;
using Code.Military.Commands;
using Code.Players;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Code.UI.Components
{
    /// <summary>
    /// 건설 가능한 건물 하나를 표시하는 아이템 슬롯입니다.
    /// 아이콘만 표시하고 이름·역할·필요 자원은 호버 시 BuildTooltip으로 보여줍니다.
    /// 클릭 시 CommandSelectEvent를 발행합니다.
    /// </summary>
    public class ConstructionItemUI : MonoBehaviour, IUIElement<BaseCommandSO>,
        IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI")]
        [SerializeField] private Image icon;
        [SerializeField] private Button button;
        [SerializeField] private Owner uiOwner = Owner.Player;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color lockedColor = Color.gray;

        private BuildTooltip _tooltip;
        private UnlockableSO _target;
        private Color _contentColor = Color.white;
        private readonly DelayedAction _hoverDelay = new();
        private Vector2 _pointerScreenPos;

        private void Awake()
        {
            Debug.Assert(icon != null && button != null, $"[{name}] icon or button is null!", this);
        }

        /// <summary>
        /// 호버 시 정보를 표시할 공유 툴팁을 주입합니다. ConstructionPanelController가 초기화 시 호출합니다.
        /// </summary>
        public void SetTooltip(BuildTooltip tooltip)
            => _tooltip = tooltip;

        /// <summary>
        /// 커맨드에 맞는 아이콘을 표시하고 구매 가능 여부에 따라 색과 상호작용을 정합니다.
        /// </summary>
        public void EnableFor(BaseCommandSO command)
        {
            if (command == null)
            {
                Disable();
                return;
            }

            _target = ResolveTarget(command);

            icon.sprite = command.Icon;
            icon.enabled = command.Icon != null;

            ResourceCostSO cost = _target != null ? _target.Cost : null;
            bool canAfford = cost == null
                             || (UserSupplies.Instance != null &&
                                 UserSupplies.Instance.HasEnoughSupplies(uiOwner, cost));

            _contentColor = canAfford ? normalColor : lockedColor;
            icon.color = _contentColor;
            button.interactable = canAfford;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => Bus<CommandSelectEvent>.Raise(uiOwner, new CommandSelectEvent(command)));
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 슬롯을 비활성화하고 초기화합니다.
        /// </summary>
        public void Disable()
        {
            _hoverDelay.Cancel();
            _tooltip?.Hide(this);

            _target = null;
            icon.enabled = false;
            button.onClick.RemoveAllListeners();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 호버 시작 — 지연 후 툴팁을 표시합니다.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltip == null || _target == null)
                return;

            _pointerScreenPos = eventData.position;
            _hoverDelay.Restart(_tooltip.HoverDelay, ShowTooltip);
        }

        /// <summary>
        /// 호버 종료 — 툴팁을 숨깁니다.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _hoverDelay.Cancel();
            _tooltip?.Hide(this);
        }

        private void OnDestroy()
            => _hoverDelay.Dispose();

        /// <summary>
        /// 커맨드가 가리키는 대상(건물·유닛)을 반환합니다. 이름·역할·비용의 출처입니다.
        /// </summary>
        public static UnlockableSO ResolveTarget(BaseCommandSO command)
            => command switch
            {
                ConstructionBuildingCommandSO buildCmd => buildCmd.BuildingSO,
                BuildUnitCommandSO buildUnitCmd => buildUnitCmd.Unit,
                BuildSquadCommandSO buildSquadCmd => buildSquadCmd.Squad,
                BuildCommanderCommandSO buildCommanderCmd => buildCommanderCmd.Commander,
                AppointCommanderCommandSO appointCmd => appointCmd.CommanderData,
                _ => null
            };

        // 호버 지연 후 이름·역할·필요 자원을 포인터 위쪽에 펼친다
        private void ShowTooltip()
        {
            if (_tooltip == null || _target == null)
                return;

            _tooltip.SetContent(_target, _contentColor);
            _tooltip.Show(this);

            // 건설 패널이 화면 하단이므로 pivot을 하단 중앙에 두어 포인터 위쪽으로 펼친다.
            _tooltip.SetPivot(new Vector2(0.5f, 0f));
            _tooltip.SetPosition(_pointerScreenPos);
        }
    }
}
