using Code.Core.Bus;
using Code.GameEvents.Unit;
using Code.Military.Commanders;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 아군 지휘관이 지정한 수만큼 임명됐을 때 완료됩니다. 지휘관은 유닛이라 스폰 이벤트로 셉니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_CommanderAppointed", menuName = "Objectives/Conditions/CommanderAppointed")]
    public class CommanderAppointedConditionSO : ObjectiveConditionSO
    {
        [field: SerializeField, Min(1)] public int Count { get; private set; } = 1;

        public override ConditionTracker CreateTracker()
            => new CommanderAppointedTracker(Count);
    }

    public class CommanderAppointedTracker : ConditionTracker
    {
        public override int Current => _current;

        public override int Target => _count;

        private readonly int _count;
        private int _current;

        public CommanderAppointedTracker(int count)
            => _count = count;

        public override void Subscribe()
            => Bus<UnitSpawnEvent>.OnEvent[Owner.Player] += OnSpawned;

        public override void Unsubscribe()
            => Bus<UnitSpawnEvent>.OnEvent[Owner.Player] -= OnSpawned;

        private void OnSpawned(UnitSpawnEvent e)
        {
            if (e.Unit is not Commander)
                return;

            _current++;
            Report();

            if (_current >= _count)
                Complete();
        }
    }
}
