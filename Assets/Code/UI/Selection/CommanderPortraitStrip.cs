using System;
using System.Collections.Generic;
using Code.Commandable.Interfaces;
using Code.Core;
using Code.Core.Bus;
using Code.Military.Commanders;
using Code.Military.Data;
using Code.Military.Squads;
using Code.Players;
using UnityEngine;

namespace Code.UI.Selection
{
    /// <summary>
    /// 화면 오른쪽에 내 지휘관을 세워 둡니다. 지휘관은 분대 뒤에 서 있어 전투 중에 찾기 어렵습니다 —
    /// 여기서는 고르지 않아도 늘 보입니다.
    /// 좌클릭은 그 지휘관만 고르고, 우클릭은 지금 고른 분대를 그 밑으로 넣습니다.
    /// 자리와 생김새는 씬과 프리팹이 정하고, 이 클래스는 몇 장을 띄우고 무엇을 채울지만 압니다.
    /// </summary>
    public sealed class CommanderPortraitStrip : MonoBehaviour
    {
        [Header("배선")]
        [SerializeField] private RectTransform container;     // 초상이 들어갈 부모. 레이아웃 그룹을 여기 붙인다
        [SerializeField] private CommanderPortraitUI portraitPrefab;

        [Header("설정")]
        [SerializeField] private Owner owner = Owner.Player;
        [SerializeField, Min(1)] private int maxPortraits = 6;
        [SerializeField, Min(0.05f)] private float refreshInterval = 0.25f; // 명부는 이벤트가 없어 주기로 본다

        private readonly List<CommanderPortraitUI> _portraits = new(6);
        private readonly List<Commander> _commanders = new(6);
        private readonly Dictionary<CommanderPortraitUI, (Action pick, Action assign)> _bound = new(6);
        private readonly List<Squad> _squadBuffer = new(12);

        private SelectionController _selection;
        private float _nextRefresh;

        private void Awake()
        {
            if (container == null)
                container = transform as RectTransform;

            if (portraitPrefab == null)
            {
                // 배선이 빠지면 초상이 통째로 안 뜬다. 조용히 넘어가면 원인을 찾는 데 한참 걸린다
                Debug.LogWarning($"[{name}] 초상 프리팹이 배선되지 않아 지휘관 줄을 끕니다.", this);
                enabled = false;
            }
        }

        /// <summary>
        /// 깃발 번호로 초상 칸을 찾습니다. 튜토리얼의 강조와 안내 손이 특정 지휘관을 짚을 때 씁니다.
        /// </summary>
        public bool TryGetPortrait(int ordinal, out RectTransform rect)
        {
            rect = null;
            for (int i = 0; i < _commanders.Count && i < _portraits.Count; i++)
            {
                if (_commanders[i] == null || _commanders[i].Ordinal != ordinal)
                    continue;

                CommanderPortraitUI portrait = _portraits[i];
                if (portrait == null || !portrait.gameObject.activeInHierarchy)
                    return false;

                rect = portrait.transform as RectTransform;
                return rect != null;
            }

            return false;
        }

        private void Update()
        {
            if (Time.time < _nextRefresh)
                return;

            _nextRefresh = Time.time + refreshInterval;
            Refresh();
        }

        private void Refresh()
        {
            Collect();

            int shown = Mathf.Min(_commanders.Count, maxPortraits);
            for (int i = 0; i < shown; i++)
            {
                Commander commander = _commanders[i];
                CommanderPortraitUI portrait = PortraitAt(i);
                CommanderSO data = commander.CommanderSO;

                portrait.SetPortrait(data != null ? data.Icon : null);
                portrait.SetFlag(commander.Color, commander.Ordinal);
                portrait.SetCount(commander.Roster.Count, commander.Roster.Capacity);
                Bind(portrait, () => PickOnly(commander), () => AssignSelectedTo(commander));
            }

            Trim(shown);
        }

        /// <summary>좌클릭 — 다른 선택을 풀고 그 지휘관만 남깁니다.</summary>
        private void PickOnly(Commander commander)
        {
            if (commander == null || commander.IsDead)
                return;

            SelectionController selection = FindSelection();
            if (selection != null)
                selection.DeselectAllUnits();

            commander.Select();
        }

        /// <summary>
        /// 우클릭 — 지금 고른 분대를 전부 그 지휘관에게 넘깁니다.
        /// 고른 것이 없으면 아무 일도 하지 않습니다. 초상만 눌러 놓고 분대를 안 고른 경우입니다.
        /// </summary>
        private void AssignSelectedTo(Commander commander)
        {
            if (commander == null || commander.IsDead)
                return;

            SelectionController selection = FindSelection();
            if (selection == null || selection.SelectedUnits == null)
                return;

            // 배속이 명부를 건드리는 동안 선택 목록을 순회하면 안 된다 — 먼저 모아 둔다
            _squadBuffer.Clear();
            foreach (ISelectable selectable in selection.SelectedUnits)
                if (selectable is Squad squad && !squad.IsDead && squad.Owner == commander.Owner)
                    _squadBuffer.Add(squad);

            foreach (Squad squad in _squadBuffer)
                commander.Roster.Assign(squad);

            _squadBuffer.Clear();
            Refresh();
        }

        private void Collect()
        {
            _commanders.Clear();
            foreach (Commander commander in Commander.AliveCommanders)
                if (commander != null && !commander.IsDead && commander.Owner == owner)
                    _commanders.Add(commander);

            _commanders.Sort((a, b) => a.Ordinal.CompareTo(b.Ordinal));
        }

        private CommanderPortraitUI PortraitAt(int index)
        {
            while (_portraits.Count <= index)
                _portraits.Add(Instantiate(portraitPrefab, container));

            CommanderPortraitUI portrait = _portraits[index];
            if (!portrait.gameObject.activeSelf)
                portrait.gameObject.SetActive(true);

            return portrait;
        }

        private void Trim(int shown)
        {
            for (int i = shown; i < _portraits.Count; i++)
                if (_portraits[i].gameObject.activeSelf)
                    _portraits[i].gameObject.SetActive(false);
        }

        // 초상은 재사용되므로 이전 지휘관의 핸들러를 떼고 새로 건다
        private void Bind(CommanderPortraitUI portrait, Action pick, Action assign)
        {
            if (_bound.TryGetValue(portrait, out (Action pick, Action assign) old))
            {
                portrait.Picked -= old.pick;
                portrait.Assigned -= old.assign;
            }

            portrait.Picked += pick;
            portrait.Assigned += assign;
            _bound[portrait] = (pick, assign);
        }

        private SelectionController FindSelection()
        {
            if (_selection == null)
                _selection = FindAnyObjectByType<SelectionController>();

            return UnityRef.Alive(_selection);
        }
    }
}
