using Code.Commandable;
using Code.Commandable.Interfaces;
using Code.Commands.Base;
using Code.Core;
using Code.Core.Bus;
using Code.Economics;
using Code.GameEvents.System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Code.UI.Components
{
    /// <summary>
    /// 커맨드 한 칸을 표시하는 버튼 UI입니다.
    /// 아이콘만 표시하고 이름·비용은 호버 시 BuildTooltip으로 보여줍니다.
    /// 클릭 시 커맨드를 실행합니다.
    /// </summary>
    public class BuildUnitButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Elements")]
        [SerializeField] private Image icon;
        [SerializeField] private Button button;
        [SerializeField] private Color disabledColor = Color.gray;
        [SerializeField] private TextMeshProUGUI hotkeyText;   // 칸에 배정된 단축키 표시

        private BaseCommandSO _command;
        private ISelectable _target;

        private BuildTooltip _tooltip;
        private string _tooltipName;
        private string _tooltipDescription;
        private ResourceCostSO _cost;
        private Color _contentColor = Color.white;

        private readonly DelayedAction _hoverDelay = new();
        private Vector2 _pointerScreenPos;

        // 툴팁을 이 사각형 위로 밀어 올린다. 매 프레임 배열을 새로 만들지 않도록 캐시한다.
        private RectTransform _tooltipAnchor;
        private readonly Vector3[] _anchorCorners = new Vector3[4];

        private const float TooltipMargin = 8f;

        /// <summary>
        /// 지금 이 칸에 꽂힌 커맨드입니다. 빈 칸이면 null.
        /// </summary>
        public BaseCommandSO Command => _command;

        /// <summary>
        /// 호버 시 정보를 표시할 공유 툴팁을 주입합니다. CommandPanel이 초기화 시 호출합니다.
        /// </summary>
        public void SetTooltip(BuildTooltip tooltip)
            => _tooltip = tooltip;

        /// <summary>
        /// 툴팁이 절대 가리면 안 되는 사각형을 지정합니다. 보통 커맨드 패널 전체입니다.
        /// </summary>
        public void SetTooltipAnchor(RectTransform anchor)
            => _tooltipAnchor = anchor;

        /// <summary>
        /// 칸에 배정된 단축키를 표시합니다. 빈 문자열이면 라벨을 숨깁니다.
        /// </summary>
        public void SetHotkeyLabel(string label)
        {
            if (hotkeyText == null)
                return;

            hotkeyText.SetText(label ?? string.Empty);
            hotkeyText.enabled = !string.IsNullOrEmpty(label);
        }

        /// <summary>
        /// 단축키로 이 칸을 누릅니다. 비활성 칸은 무시합니다.
        /// </summary>
        public void InvokeByHotkey()
        {
            if (_command == null || !button.interactable)
                return;

            HandleClick();
        }

        /// <summary>
        /// 커맨드를 칸에 꽂습니다. 비용은 호출자가 정합니다 — 병영처럼 대상에 따라 값이 달라지는 커맨드가 있습니다.
        /// </summary>
        public void EnableFor(BaseCommandSO command, ISelectable target, ResourceCostSO cost)
        {
            _command = command;
            _target = target;
            _cost = cost;

            // 이름·역할은 커맨드가 가리키는 대상에서 가져온다. 대상이 없는 커맨드는 커맨드 이름을 쓴다
            UnlockableSO info = ConstructionItemUI.ResolveTarget(command);
            _tooltipName = info != null ? info.Name : command.Name;
            _tooltipDescription = info != null ? info.Description : string.Empty;

            SetIcon(command.Icon);

#if UNITY_EDITOR
            // 이름 라벨을 뺀 뒤로 아이콘 없는 커맨드는 그냥 빈 칸이 된다. 조용히 사라지지 않도록 저작 단계에서 잡는다
            if (command.Icon == null)
                Debug.LogWarning($"[{name}] 커맨드 '{command.name}'(Slot {command.Slot})에 아이콘이 없어 빈 칸으로 보입니다.", command);
#endif

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);

            Refresh();
        }

        /// <summary>
        /// 칸을 비웁니다. 빈 칸도 자리를 지켜야 하므로 오브젝트는 끄지 않습니다.
        /// </summary>
        public void Disable()
        {
            _hoverDelay.Cancel();
            _tooltip?.Hide(this);

            _command = null;
            _target = null;
            _cost = null;
            _tooltipName = null;
            _tooltipDescription = null;

            SetIcon(null);

            button.interactable = false;
            button.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// 칸의 잠금 상태를 갱신합니다. 살 수 없으면 버튼을 끄고 아이콘을 회색으로 바꿉니다.
        /// </summary>
        public void Refresh()
        {
            if (_command == null)
                return;

            bool canAfford = !_command.IsLocked(BuildContext());

            // 잠겨도 눌리게 둔다 — 눌렀을 때 "왜 안 되는지"를 알려야 한다. 어둡게는 색으로 보인다
            button.interactable = true;

            _contentColor = canAfford ? Color.white : disabledColor;
            icon.color = _contentColor;
        }

        /// <summary>
        /// 호버 시작 — 지연 후 툴팁을 표시합니다.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltip == null || _command == null)
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

        // 호버 지연 후 이름·역할·비용을 포인터 위쪽에 펼친다
        private void ShowTooltip()
        {
            if (_tooltip == null || _command == null)
                return;

            _tooltip.SetContent(_tooltipName, _tooltipDescription, _cost, _contentColor);
            _tooltip.Show(this);

            // 커맨드 패널이 화면 하단이므로 pivot을 하단 중앙에 두어 위쪽으로만 펼친다.
            _tooltip.SetPivot(new Vector2(0.5f, 0f));
            _tooltip.SetPosition(new Vector2(_pointerScreenPos.x, TooltipBaseline()));
        }

        /// <summary>
        /// 툴팁이 시작될 Y입니다. 커서가 아닌 패널 상단 기준이라 아랫줄 호버가 윗줄을 가리지 않습니다.
        /// </summary>
        private float TooltipBaseline()
        {
            if (_tooltipAnchor == null)
                return _pointerScreenPos.y;

            _tooltipAnchor.GetWorldCorners(_anchorCorners);
            return _anchorCorners[1].y + TooltipMargin;
        }

        /// <summary>
        /// 칸이 클릭되었을 때 커맨드를 실행합니다.
        /// 월드 좌표가 필요한 커맨드는 직접 실행하지 않고 CommandExecutor에게 넘깁니다.
        /// </summary>
        private void HandleClick()
        {
            if (_command == null || _target == null)
                return;

            // 잠긴 칸은 실행 대신 이유를 알린다 — 한도·자원 부족을 말없이 삼키면 "눌러도 안 된다"로만 보인다
            if (_command.IsLocked(BuildContext()))
            {
                _command.NotifyRefusal(BuildContext());
                return;
            }

            if (_command.RequireClickToActivate)
            {
                Bus<CommandSelectEvent>.Raise(Owner.Player, new CommandSelectEvent(_command));
                return;
            }

            _command.Handle(BuildContext());
        }

        /// <summary>
        /// 현재 버튼의 명령과 대상에 맞는 CommandContext를 생성합니다.
        /// </summary>
        private CommandContext BuildContext()
            => _target is AbstractCommandable commandable
                ? new CommandContext(Owner.Player, commandable, new RaycastHit2D())
                : new CommandContext(Owner.Player, _target, new RaycastHit2D(), 0, MouseButton.Left, Vector3.zero);

        /// <summary>
        /// 아이콘 이미지를 설정합니다. sprite가 null이면 아이콘을 숨겨 빈 칸으로 둡니다.
        /// </summary>
        private void SetIcon(Sprite sprite)
        {
            if (sprite == null)
            {
                icon.enabled = false;
            }
            else
            {
                icon.sprite = sprite;
                icon.enabled = true;
            }
        }
    }
}
