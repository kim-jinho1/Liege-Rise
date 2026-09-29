using Code.Military.Combat.FSM;
using Code.Military.Data;
using Code.Military.Squads.Formations;
using Code.Military.Stats;
using Code.Units.Combat;
using UnityEngine;

namespace Code.Military.Squads
{
    /// <summary>
    /// 대형이 분대에 미치는 것 전부를 맡습니다 — 슬롯 재배치, 밀어내기 반경, 회전 속도,
    /// 그리고 재정렬을 마쳐야 걸리는 전투 보정입니다.
    /// </summary>
    public class SquadFormation
    {
        /// <summary>
        /// 새 자리로 걸어가는 중이라 아직 보정이 걸리지 않은 상태입니다.
        /// </summary>
        public bool IsReforming => _remaining > 0f;

        /// <summary>
        /// 분대원에게 먹일 현재 이동 속도 배율입니다.
        /// </summary>
        public float MoveSpeedMultiplier => _moveSpeedStat != null ? _moveSpeedStat.Value : 1f;

        private readonly SquadRoster _roster;
        private readonly SquadFacing _facing;
        private readonly SquadStateMachine _machine;
        private readonly StatSheet _stats;
        private readonly StatSO _moveSpeedStat;

        // 보정을 실제로 걸어 둔 대형입니다. 스탯 시트의 소스 키이기도 합니다.
        private FormationSO _applied;
        private float _remaining;

        public SquadFormation(SquadRoster roster, SquadFacing facing, SquadStateMachine machine,
            StatSheet stats, SquadSO squadSO)
        {
            _roster = roster;
            _facing = facing;
            _machine = machine;
            _stats = stats;

            _moveSpeedStat = squadSO == null ? null : stats.Get(squadSO.MoveSpeedMultiplierStat);

            // 대형뿐 아니라 지휘관 패시브가 속도를 건드려도 같은 경로로 흐른다
            if (_moveSpeedStat != null)
                _moveSpeedStat.OnValueChanged += HandleMoveSpeedChanged;
        }

        /// <summary>
        /// 스탯 구독을 끊습니다. 분대가 파괴될 때 호출합니다.
        /// </summary>
        public void Dispose()
        {
            if (_moveSpeedStat != null)
                _moveSpeedStat.OnValueChanged -= HandleMoveSpeedChanged;
        }

        /// <summary>
        /// 분대가 처음 서는 대형입니다. 재정렬 대가 없이 즉시 적용합니다.
        /// </summary>
        public void SelectInitial(int index)
        {
            _roster.SetFormation(index);
            Switch(true);
        }

        /// <summary>
        /// 대형을 바꿉니다. 이전 보정은 즉시 풀리고 새 보정은 재정렬을 마쳐야 붙습니다.
        /// </summary>
        public void Select(int index)
        {
            _roster.SetFormation(index);
            Switch(false);
        }

        /// <summary>
        /// 대형 에셋으로 바꿉니다. 보유하지 않은 대형이면 선택기가 무시하므로 그대로 재정렬만 돕니다.
        /// </summary>
        public void Select(FormationSO formation)
        {
            _roster.SetFormation(formation);
            Switch(false);
        }

        /// <summary>
        /// 재정렬 시간을 흘립니다. 분대가 매 프레임 호출합니다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_remaining <= 0f)
                return;

            _remaining -= deltaTime;
            if (_remaining <= 0f)
                ApplyEffects();
        }

        /// <summary>
        /// 새로 합류한 분대원에게 현재 대형의 밀어내기 반경과 이동 속도를 먹입니다.
        /// </summary>
        public void TuneMember(AbstractCombatUnit member)
        {
            if (member == null)
                return;

            FormationSO formation = _roster.CurrentFormation;
            member.SeparationRadius = formation != null ? formation.SeparationRadius : 0.4f;
            member.ApplySpeedMultiplier(MoveSpeedMultiplier);
        }

        private void Switch(bool immediate)
        {
            ReleaseEffects();

            FormationSO formation = _roster.CurrentFormation;
            _facing.RotationSpeed = formation != null ? formation.RotationSpeed : 90f;
            TuneMembers();

            if (immediate)
            {
                ApplyEffects();
                return;
            }

            // 제자리에서 새 자리로 걸어가게 한다 — 이게 없으면 버튼을 눌러도 화면이 그대로다
            _machine.NotifyFormationChanged();

            _remaining = formation != null ? Mathf.Max(0f, formation.SetupTime) : 0f;
            if (_remaining <= 0f)
                ApplyEffects();
        }

        private void TuneMembers()
        {
            foreach (AbstractCombatUnit member in _roster.Members)
                if (!member.IsDead)
                    TuneMember(member);
        }

        private void ApplyEffects()
        {
            FormationSO formation = _roster.CurrentFormation;

            // UnityEngine.Object에는 ?. 를 쓰지 않는다 — == 오버로드를 우회한다
            if (formation == null)
                return;

            _applied = formation;

            StatEffect[] effects = formation.Effects;
            if (effects == null)
                return;

            foreach (StatEffect effect in effects)
                effect?.ApplyTo(_stats, formation);
        }

        /// <summary>
        /// 이전 대형이 건 보정을 되돌립니다. 소스가 대형 에셋이라 지휘관 보정은 남습니다.
        /// </summary>
        private void ReleaseEffects()
        {
            _remaining = 0f;

            if (_applied == null)
                return;

            _stats.RemoveBySource(_applied);
            _applied = null;
        }

        // 분대원이 대형에서 받아 가는 값은 TuneMember 한 곳에만 적는다
        private void HandleMoveSpeedChanged(StatSO stat, float current, float previous)
            => TuneMembers();
    }
}
