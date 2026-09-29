using System.Collections.Generic;
using Code.Buildings.Logic.DefenseBuilding;
using Code.Commandable;
using Code.Commandable.Interfaces;
using Code.Military.Data;
using Code.Military.Squads.Formations;
using Code.Units.Combat;
using UnityEngine;

namespace Code.Military.Squads
{
    public class SquadRoster : MonoBehaviour
    {
        /// <summary>
        /// 살아 있는 분대원 수입니다.
        /// </summary>
        public int AliveCount => _members.Count;

        /// <summary>
        /// 살아 있는 분대원 목록입니다. 사망자는 NotifyDied에서 빠지므로 항상 압축된 상태입니다.
        /// </summary>
        public IReadOnlyList<AbstractCombatUnit> Members => _members;

        /// <summary>
        /// 살아 있는 분대원들의 현재 체력 합입니다.
        /// </summary>
        public int TotalCurrentHealth
        {
            get
            {
                int sum = 0;
                foreach (AbstractCombatUnit member in _members)
                    if (member != null && !member.IsDead)
                        sum += member.CurrentHealth;

                return sum;
            }
        }

        /// <summary>
        /// 정원이 모두 살아 있을 때의 체력 합입니다. 분대 루트의 MaxHealth와 같은 정의를 씁니다.
        /// </summary>
        public int TotalMaxHealth
            => _squadSO == null || _squadSO.MemberSO == null ? 0 : _squadSO.MemberCount * _squadSO.MemberSO.Health;

        /// <summary>
        /// 분대원 체력 합이 바뀔 때 발생합니다. 루트는 피해를 받지 않으므로 표시는 이 경로로만 갱신됩니다.
        /// </summary>
        public event System.Action HealthChanged;

        /// <summary>
        /// 현재 자세에 해당하는 대형입니다. 인스펙터가 비어 있으면 null입니다.
        /// </summary>
        public FormationSO CurrentFormation => _formations.Current;

        public Vector3 MembersCenter
        {
            get
            {
                Vector3 sum = Vector3.zero;
                int count = 0;

                foreach (AbstractCombatUnit member in _members)
                {
                    if (member.IsDead)
                        continue;

                    sum += member.transform.position;
                    count++;
                }

                return count == 0 ? transform.position : sum / count;
            }
        }

        private readonly List<AbstractCombatUnit> _members = new();

        private Squad _squad;
        private SquadSO _squadSO;
        private readonly FormationSelector _formations = new();

        /// <summary>
        /// 분대와 분대원들을 설정합니다. 이 메서드는 Awake 이후에 호출되어야 합니다.
        /// </summary>
        public void SetUp(Squad squad, SquadSO squadSO)
        {
            _squad = squad;
            _squadSO = squadSO;
            _formations.SetUp(squadSO != null ? squadSO.Formations : null);
        }

        /// <summary>
        /// 분대원들을 생성합니다. 생성된 분대원들은 이 컴포넌트의 자식으로 설정됩니다.
        /// </summary>
        public void SpawnMembers()
        {
            for (int i = 0; i < _squadSO.MemberCount; i++)
                SpawnMember(i);
        }

        /// <summary>
        /// 분대원 1기를 보충합니다. 정원이 차 있으면 아무것도 하지 않고 false를 돌려줍니다.
        /// </summary>
        public bool ReinforceOne()
        {
            // 시신이 빠져 있으므로 명부 크기가 곧 다음 빈자리다
            if (_squadSO == null || _members.Count >= _squadSO.MemberCount)
                return false;

            return SpawnMember(_members.Count);
        }

        /// <summary>
        /// 분대원 1기를 대형의 지정한 자리에 세우고 명부에 올립니다.
        /// </summary>
        private bool SpawnMember(int slotIndex)
        {
            FormationSO formation = CurrentFormation;

            Vector3 slot = formation == null
                ? Vector3.zero
                : formation.GetOffset(slotIndex, _squadSO.MemberCount);

            GameObject go = Instantiate(_squadSO.MemberSO.Prefab,
                transform.position + slot, Quaternion.identity);

            if (!go.TryGetComponent(out AbstractCombatUnit unit))
                return false;

            unit.Owner = _squad.Owner;
            unit.AbstractCommandableSO = _squadSO.MemberSO; // Start 전이라 Health·AttackConfig가 이 값으로 잡힌다.
            unit.IsSpawnerProduced = _squad.IsSpawnerProduced;
            _squad.Formation?.TuneMember(unit); // 밀어내기 반경·이동 속도는 대형이 정한다
            unit.Squad = _squad;
            _members.Add(unit);

            // 분대원의 Start가 아직이라 초기 체력 통지도 이 구독으로 들어온다.
            unit.OnHealthUpdated += HandleMemberHealthUpdated;

            // 보충으로 합류한 분대원도 선택 중이면 외곽선을 물려받아야 한다.
            if (_squad != null && _squad.IsSelected)
                unit.SetSelectionVisual(true);

            HealthChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 분대원들을 현재 대형에 따라 루트 위치를 기준으로 배치합니다. 죽은 분대원은 무시됩니다.
        /// </summary>
        public void Arrange(Vector3 origin, float facingAngle)
            => Arrange(origin, facingAngle, CurrentFormation == null ? 0f : CurrentFormation.Spacing);

        /// <summary>
        /// 간격을 지정해 배치합니다. 교전 중에는 사거리에 맞춰 좁힌 간격이 들어옵니다.
        /// </summary>
        public void Arrange(Vector3 origin, float facingAngle, float spacing)
        {
            FormationSO formation = CurrentFormation;

            int slot = 0;
            foreach (AbstractCombatUnit member in _members)
            {
                if (member.IsDead)
                    continue;

                member.MoveTo(formation == null
                    ? origin
                    : formation.SlotAt(slot, AliveCount, origin, facingAngle, spacing));

                slot++;
            }
        }

        /// <summary>
        /// 분대원들을 멈춥니다. 죽은 분대원은 무시됩니다.
        /// </summary>
        public void HaltAll() => SquadOrders.HaltAll(_members);

        /// <summary>
        /// 분대원들에게 지정한 대상에 대한 공격을 할당합니다. 죽은 분대원은 무시됩니다.
        /// </summary>
        public void AssignAttack(IDamageable target) => SquadOrders.AssignAttack(_members, target);

        /// <summary>
        /// 분대원 전원에게 탑승 명령을 내립니다.
        /// </summary>
        public void AssignMount(MannedTowerBuilding tower) => SquadOrders.AssignMount(_members, tower);

        /// <summary>
        /// 분대원들이 모두 지정한 위치에 도착했는지 확인합니다.
        /// </summary>
        public bool AllArrived() => SquadOrders.AllArrived(_members);

        /// <summary>
        /// 분대원 중 한 명이라도 지정한 범위 안에 있는지 확인합니다.
        /// </summary>
        public bool AnyInRange(GameObject targetGo, float range) => SquadOrders.AnyInRange(_members, targetGo, range);

        /// <summary>
        /// 분대의 대형을 지정합니다. 지정한 인덱스가 유효하지 않으면 무시됩니다.
        /// </summary>
        public void SetFormation(int index)
            => _formations.Select(index);

        /// <summary>
        /// 대형 에셋으로 자세를 지정합니다. 이 분대가 쓸 수 없는 대형이면 무시됩니다.
        /// </summary>
        public void SetFormation(FormationSO formation)
            => _formations.Select(formation);

        /// <summary>
        /// 이 분대가 지정한 대형을 사용할 수 있는지 확인합니다.
        /// </summary>
        public bool CanUse(FormationSO formation)
            => _formations.CanUse(formation);

        /// <summary>
        /// 분대원 한 명의 사망을 알립니다.
        /// </summary>
        // 남겨 두면 곧 파괴될 참조를 계속 순회한다. 파괴 전에 불리므로 여기서 뺀다
        public void NotifyDied(AbstractCombatUnit member)
        {
            if (member != null)
                member.OnHealthUpdated -= HandleMemberHealthUpdated;

            _members.Remove(member);
            HealthChanged?.Invoke();
        }

        private void HandleMemberHealthUpdated(AbstractCommandable member, int lastHealth, int newHealth)
            => HealthChanged?.Invoke();

        /// <summary>
        /// 분대원들의 선택 시각화를 설정합니다. 선택 시각화는 분대원 개별적으로 적용됩니다.
        /// </summary>
        public void SetSelectionVisual(bool selected)
        {
            foreach (AbstractCombatUnit member in _members)
                if (!member.IsDead)
                    member.SetSelectionVisual(selected);
        }

        /// <summary>
        /// 분대원들의 호버 외곽선을 설정합니다.
        /// </summary>
        public void SetHoveredVisual(bool hovered)
        {
            foreach (AbstractCombatUnit member in _members)
                if (!member.IsDead)
                    member.SetHoveredVisual(hovered);
        }

        /// <summary>
        /// 분대원들의 시퀀스 안내 외곽선을 설정합니다.
        /// </summary>
        public void SetGuideHighlight(bool on)
        {
            foreach (AbstractCombatUnit member in _members)
                if (!member.IsDead)
                    member.SetGuideHighlight(on);
        }
    }
}
