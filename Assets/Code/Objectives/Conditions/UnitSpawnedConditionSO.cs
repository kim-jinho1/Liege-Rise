using Code.Core.Bus;
using Code.GameEvents.Unit;
using Code.Units.Base;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 플레이어 유닛이 지정 수만큼 생산되었을 때 완료되는 조건입니다.
    /// TargetUnit이 null이면 모든 플레이어 유닛을 카운트합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_UnitSpawned", menuName = "Objectives/Conditions/UnitSpawned")]
    public class UnitSpawnedConditionSO : ObjectiveConditionSO
    {
        [field: SerializeField] public AbstractUnitSO TargetUnit { get; private set; }
        [field: SerializeField] public int Count { get; private set; } = 1;

        public override ConditionTracker CreateTracker()
            => new UnitSpawnedTracker(TargetUnit, Count);
    }

    public class UnitSpawnedTracker : ConditionTracker
    {
        public override int Current => _current;

        public override int Target => _count;

        private readonly AbstractUnitSO _targetUnit;
        private readonly int _count;
        private int _current;

        public UnitSpawnedTracker(AbstractUnitSO targetUnit, int count)
        {
            _targetUnit = targetUnit;
            _count = count;
        }

        public override void Subscribe()
            => Bus<UnitSpawnEvent>.OnEvent[Owner.Player] += OnUnitSpawned;

        public override void Unsubscribe()
            => Bus<UnitSpawnEvent>.OnEvent[Owner.Player] -= OnUnitSpawned;

        private void OnUnitSpawned(UnitSpawnEvent e)
        {
            if (_targetUnit != null && e.Unit.UnitSO != _targetUnit)
                return;

            _current++;
            Report();

            if (_current >= _count)
                Complete();
        }
    }
}
