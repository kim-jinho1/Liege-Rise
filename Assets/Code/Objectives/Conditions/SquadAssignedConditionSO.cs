using Code.Core.Bus;
using Code.GameEvents.Unit;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 분대가 지휘관 밑에 지정한 수만큼 배속됐을 때 완료됩니다. 임명 때 딸려 들어가는 첫 분대도 셉니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_SquadAssigned", menuName = "Objectives/Conditions/SquadAssigned")]
    public class SquadAssignedConditionSO : ObjectiveConditionSO
    {
        [field: SerializeField, Min(1)] public int Count { get; private set; } = 1;

        public override ConditionTracker CreateTracker()
            => new SquadAssignedTracker(Count);
    }

    public class SquadAssignedTracker : ConditionTracker
    {
        public override int Current => _current;

        public override int Target => _count;

        private readonly int _count;
        private int _current;

        public SquadAssignedTracker(int count)
            => _count = count;

        public override void Subscribe()
            => Bus<CommanderOrderEvent>.OnEvent[Owner.Player] += OnOrder;

        public override void Unsubscribe()
            => Bus<CommanderOrderEvent>.OnEvent[Owner.Player] -= OnOrder;

        private void OnOrder(CommanderOrderEvent e)
        {
            if (e.Kind != CommanderOrderKind.Assigned)
                return;

            _current++;
            Report();

            if (_current >= _count)
                Complete();
        }
    }
}
