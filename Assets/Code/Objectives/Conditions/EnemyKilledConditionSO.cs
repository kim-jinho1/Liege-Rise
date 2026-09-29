using Code.Core.Bus;
using Code.GameEvents.Enemy;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 적 유닛이 지정 수만큼 처치되었을 때 완료되는 조건입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_EnemyKilled", menuName = "Objectives/Conditions/EnemyKilled")]
    public class EnemyKilledConditionSO : ObjectiveConditionSO
    {
        [field: SerializeField] public int Count { get; private set; } = 1;

        public override ConditionTracker CreateTracker()
            => new EnemyKilledTracker(Count);
    }

    public class EnemyKilledTracker : ConditionTracker
    {
        public override int Current => _current;

        public override int Target => _count;

        private readonly int _count;
        private int _current;

        public EnemyKilledTracker(int count)
        {
            _count = count;
        }

        public override void Subscribe()
            => Bus<EnemyDeathEvent>.RegisterForAll(OnEnemyKilled);

        public override void Unsubscribe()
            => Bus<EnemyDeathEvent>.UnRegisterForAll(OnEnemyKilled);

        private void OnEnemyKilled(EnemyDeathEvent _)
        {
            _current++;
            Report();

            if (_current >= _count)
                Complete();
        }
    }
}
