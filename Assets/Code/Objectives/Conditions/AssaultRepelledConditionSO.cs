using Code.Core.Bus;
using Code.EnemyAI;
using Code.GameEvents.Enemy;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 몰려온 적 공세를 지정 횟수만큼 막아냈을 때 완료되는 조건입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_AssaultRepelled", menuName = "Objectives/Conditions/AssaultRepelled")]
    public class AssaultRepelledConditionSO : ObjectiveConditionSO
    {
        [field: SerializeField] public int Count { get; private set; } = 1;

        public override ConditionTracker CreateTracker()
            => new AssaultRepelledTracker(Count);
    }

    public class AssaultRepelledTracker : ConditionTracker
    {
        public override int Current => _repelled;

        public override int Target => _count;

        private readonly int _count;
        private int _repelled;
        private bool _underAttack;

        public AssaultRepelledTracker(int count)
        {
            _count = count;
        }

        public override void Subscribe()
        {
            Bus<EnemyDispatchEvent>.OnEvent[Owner.Enemy] += OnDispatched;
            Bus<AssaultsClearedEvent>.OnEvent[Owner.Enemy] += OnCleared;

            // 앞 스텝에서 이미 나와 있던 공세는 출발 신호를 놓친다. 지금 떠 있는 부대를 세어 이어받는다
            if (AssaultRegistry.Forces.Count > 0)
            {
                _underAttack = true;
                return;
            }

            // 막을 것이 이미 없다 — 앞 스텝에서 다 잡은 것이다. 여기서 기다리면 영영 안 끝난다
            _repelled = _count;
            Report();
            Complete();
        }

        public override void Unsubscribe()
        {
            Bus<EnemyDispatchEvent>.OnEvent[Owner.Enemy] -= OnDispatched;
            Bus<AssaultsClearedEvent>.OnEvent[Owner.Enemy] -= OnCleared;
        }

        // 한 웨이브가 여러 갈래로 나뉘면 출발 신호도 갈래 수만큼 온다. 그래도 "공격받는 중"은 하나다
        private void OnDispatched(EnemyDispatchEvent _)
            => _underAttack = true;

        private void OnCleared(AssaultsClearedEvent _)
        {
            // 공세가 오기도 전에 온 신호는 막아낸 것이 아니다
            if (!_underAttack)
                return;

            _underAttack = false;
            _repelled++;
            Report();

            if (_repelled >= _count)
                Complete();
        }
    }
}
