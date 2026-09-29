using Code.Core.Bus;
using Code.GameEvents.Unit;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 플레이어 분대가 지정 수만큼 편성되었을 때 완료되는 조건입니다.
    /// 분대원은 각자 UnitSpawnEvent를 내므로 UnitSpawned로는 분대를 셀 수 없습니다 —
    /// 분대 하나가 인원수만큼 세어지고, 일꾼이 재생성될 때도 함께 걸립니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_SquadTrained", menuName = "Objectives/Conditions/SquadTrained")]
    public class SquadTrainedConditionSO : ObjectiveConditionSO
    {
        [field: SerializeField, Min(1)] public int Count { get; private set; } = 1;

        public override ConditionTracker CreateTracker()
            => new SquadTrainedTracker(Count);
    }

    public class SquadTrainedTracker : ConditionTracker
    {
        public override int Current => _current;

        public override int Target => _count;

        private readonly int _count;
        private int _current;

        public SquadTrainedTracker(int count)
        {
            _count = count;
        }

        public override void Subscribe()
            => Bus<SquadSpawnEvent>.OnEvent[Owner.Player] += OnSquadSpawned;

        public override void Unsubscribe()
            => Bus<SquadSpawnEvent>.OnEvent[Owner.Player] -= OnSquadSpawned;

        private void OnSquadSpawned(SquadSpawnEvent _)
        {
            _current++;
            Report();

            if (_current >= _count)
                Complete();
        }
    }
}
