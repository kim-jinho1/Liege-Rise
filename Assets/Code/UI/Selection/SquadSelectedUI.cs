using System.Collections.Generic;
using Code.Commandable;
using Code.Military.Commanders;
using Code.Military.Data;
using Code.Military.Reinforcement;
using Code.Military.Squads;
using Code.Military.Squads.Formations;
using Code.UI.Components;
using Code.Units.Combat;
using TMPro;
using UnityEngine;

namespace Code.UI.Selection
{
    /// <summary>
    /// 분대 선택 시 인원·대형·보충 상태와 배속 카드를, 지휘관 선택 시 거느린 분대 카드를 표시하는 정보 패널입니다.
    /// 커맨드 버튼은 CommandPanel이 Slot 번호대로 전담하므로 여기서 다루지 않습니다.
    /// 이름·아이콘·체력은 공용 정보 패널(GeneralInfoUI)이 전담합니다.
    /// </summary>
    public class SquadSelectedUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject infoPanel;           // 인원·대형 표시 패널
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI reinforceText;  // 자리만 빌린다 — 보충 상태는 상태 줄에 합쳐 쓰고 이 칸에 배속 카드가 든다
        [SerializeField] private ProgressBar reinforceBar;

        [Header("Member Row")]
        [SerializeField] private RectTransform memberRow;        // 칸이 놓일 가로 컨테이너
        [SerializeField] private SquadMemberSlotUI slotPrefab;   // MemberFrame 프리팹

        [Header("Text")]
        // TMP의 SetText은 string.Format이 아니라 {0}·{1}만 아는 자체 파서다 — 서식 지정자를 붙이면 조용히 깨진다
        [SerializeField] private string countFormat = "인원 {0} / {1}";
        [SerializeField] private string formationFormat = "대형: {0}";
        [SerializeField] private string noFormationText = "대형 없음";
        [SerializeField] private string reformingSuffix = " · 재정렬 중";
        [SerializeField] private string reinforcingFormat = " · 보충 중 {0}%";
        [SerializeField] private string outOfRangeText = " · 보충 대기 · 거점 범위 밖";
        [SerializeField] private string commanderCountFormat = "거느린 분대 {0} / {1}";
        [SerializeField] private string hasLineText = "공세선 있음";
        [SerializeField] private string noLineText = "공세선 없음 · 우클릭 끌어 긋기";

        // 칸은 필요한 만큼 만들어 두고 재사용한다. 지휘관이 임명되면 한 칸 늘고 죽으면 다시 준다.
        private readonly List<SquadMemberSlotUI> _slots = new(8);

        private Squad _squad;
        private Commander _commander;

        // 매 프레임 문자열을 다시 만들지 않도록 마지막 표시값을 기억한다.
        private int _shownAliveCount = -1;
        private string _shownStatus;
        private int _shownSlotCount = -1;
        private bool _shownCommander;

        // 대형 줄은 대형이나 재정렬 상태가 바뀔 때만 달라진다. 매 프레임 string.Format을 다시 돌리면
        // 패널이 열려 있는 내내 프레임마다 문자열이 하나씩 버려진다
        private FormationSO _labelFormation;
        private bool _labelReforming;
        private string _cachedFormationLabel;

        private void Awake()
        {
            // 보충 상태는 상태 줄 끝에 붙는다. 이 줄·바는 자리만 차지하므로 접어 둔다
            if (reinforceText != null)
                reinforceText.gameObject.SetActive(false);

            if (reinforceBar != null)
                reinforceBar.gameObject.SetActive(false);
        }

        /// <summary>
        /// 지정 분대의 정보 패널을 켜고 표시를 갱신합니다.
        /// </summary>
        public void EnableFor(Squad squad)
        {
            _squad = squad;
            _commander = null;
            Open();
            if (memberRow != null)
                memberRow.gameObject.SetActive(true);
            RefreshSquad();
        }

        /// <summary>
        /// 지휘관의 정보 패널입니다. 거느린 분대가 카드로 뜨고 분대원 칸은 접습니다.
        /// </summary>
        public void EnableForCommander(Commander commander)
        {
            _commander = commander;
            _squad = null;
            Open();
            if (memberRow != null)
                memberRow.gameObject.SetActive(false);
            RefreshCommander();
        }

        private void Open()
        {
            infoPanel.SetActive(true);

            // 다른 대상으로 갈아탈 때 이전 표시값이 남아 갱신이 생략되지 않도록 초기화한다.
            _shownAliveCount = -1;
            _shownStatus = null;
            _shownSlotCount = -1;
        }

        /// <summary>
        /// 패널을 숨기고 참조를 해제합니다.
        /// </summary>
        public void Disable()
        {
            _squad = null;
            _commander = null;
            infoPanel.SetActive(false);
        }

        private void Update()
        {
            if (!infoPanel.activeSelf)
                return;

            if (_commander != null)
            {
                if (_commander.IsDead)
                    Disable();
                else
                    RefreshCommander();
                return;
            }

            // 표시 중 분대가 전멸하면 패널을 닫음
            if (_squad == null)
            {
                Disable();
                return;
            }

            RefreshSquad();
        }

        private void RefreshSquad()
        {
            RefreshMemberSlots();
            RefreshCount();
            SetStatus(FormationLabel() + ReinforceSuffix());
        }

        private void RefreshCommander()
        {
            int count = _commander.Roster.Count;
            if (count != _shownAliveCount)
            {
                _shownAliveCount = count;
                countText?.SetText(commanderCountFormat, count, _commander.Roster.Capacity);
            }

            SetStatus(_commander.HasLine ? hasLineText : noLineText);
        }

        private void RefreshCount()
        {
            int aliveCount = _squad.AliveCount;
            if (aliveCount == _shownAliveCount)
                return;

            _shownAliveCount = aliveCount;
            if (_squad.AbstractCommandableSO is SquadSO squadSO)
                countText?.SetText(countFormat, aliveCount, squadSO.MemberCount);
        }

        /// <summary>
        /// 상태 줄은 문자열이 달라졌을 때만 다시 씁니다.
        /// </summary>
        private void SetStatus(string label)
        {
            if (label == _shownStatus)
                return;

            _shownStatus = label;
            statusText?.SetText(label);
        }

        private string FormationLabel()
        {
            FormationSO formation = _squad.CurrentFormation;
            bool reforming = _squad.Formation is { IsReforming: true };

            if (_cachedFormationLabel != null
                && ReferenceEquals(_labelFormation, formation) && _labelReforming == reforming)
                return _cachedFormationLabel;

            string label = formation != null
                ? string.Format(formationFormat, formation.DisplayName)
                : noFormationText;

            // 재정렬 중에는 새 대형의 보정이 아직 없다 — 이름만 바뀐 것으로 오해하지 않게 알린다
            if (reforming)
                label += reformingSuffix;

            _labelFormation = formation;
            _labelReforming = reforming;
            _cachedFormationLabel = label;
            return label;
        }

        /// <summary>
        /// 보충 상태입니다. 진행도는 정수 퍼센트라 프레임마다 문자열이 바뀌지 않습니다.
        /// </summary>
        private string ReinforceSuffix()
        {
            SquadReinforcement reinforcement = _squad.Reinforcement;
            if (reinforcement is not { IsActive: true })
                return string.Empty;

            // 거점 판정은 레지스트리를 훑으므로 보충 중일 때만 묻는다
            if (reinforcement.CoveringPoint() == null)
                return outOfRangeText;

            return string.Format(reinforcingFormat, Mathf.RoundToInt(reinforcement.Progress * 100f));
        }

        /// <summary>
        /// 지휘관과 분대원 칸을 갱신합니다. 지휘관이 임명되면 맨 앞에 한 칸이 늘고 죽으면 사라집니다.
        /// </summary>
        private void RefreshMemberSlots()
        {
            if (memberRow == null || slotPrefab == null)
                return;

            Commander commander = _squad.Commander;
            bool hasCommander = commander != null && !commander.IsDead;

            IReadOnlyList<AbstractCombatUnit> members = _squad.Members;
            int required = (hasCommander ? 1 : 0) + members.Count;

            while (_slots.Count < required)
                _slots.Add(Instantiate(slotPrefab, memberRow));

            // 구성이 바뀐 프레임에만 아이콘을 다시 꽂는다. 체력은 매 프레임 갱신한다.
            bool layoutChanged = required != _shownSlotCount || hasCommander != _shownCommander;
            _shownSlotCount = required;
            _shownCommander = hasCommander;

            for (int i = 0; i < _slots.Count; i++)
            {
                SquadMemberSlotUI slot = _slots[i];
                bool inUse = i < required;

                if (slot.gameObject.activeSelf != inUse)
                    slot.gameObject.SetActive(inUse);

                if (!inUse)
                    continue;

                AbstractCommandable target = hasCommander && i == 0
                    ? commander
                    : members[hasCommander ? i - 1 : i];

                if (layoutChanged)
                    slot.Bind(IconOf(target), hasCommander && i == 0);

                slot.SetHealth(target);
            }
        }

        /// <summary>
        /// 표시할 아이콘을 고릅니다. 멤버 데이터에 아이콘이 없는 분대가 있어 분대 아이콘으로 대신합니다.
        /// </summary>
        private Sprite IconOf(AbstractCommandable target)
        {
            AbstractCommandableSO so = target != null ? target.AbstractCommandableSO : null;
            if (so != null && so.Icon != null)
                return so.Icon;

            return _squad.AbstractCommandableSO != null ? _squad.AbstractCommandableSO.Icon : null;
        }
    }
}
