using System;
using System.Threading;
using Code.Core.Bus;
using Code.EnemyAI;
using Code.GameEvents.Enemy;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 출격한 적 공세가 자기 목표 기지 앞까지 닿았거나, 길에서 아군과 붙었거나, 닿기 전에 전멸했으면 완료되는 조건입니다.
    /// 출발 신호로 넘기면 아직 오지도 않았는데 "왔다"고 말하게 되고, 닿기만 기다리면 앞에서 다 죽었을 때 영영 안 끝납니다.
    /// 교전도 "왔다"로 치는 이유는 길에서 요격당한 갈래가 싸움이 끝날 때까지 제자리에 서서 반경에 영영 못 들어오기 때문입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_AssaultNear", menuName = "Objectives/Conditions/AssaultApproached")]
    public class AssaultApproachedConditionSO : ObjectiveConditionSO
    {
        // 부대 중심이 목표 기지에서 이 거리 안에 들면 닿은 것이다
        [field: SerializeField, Min(1f)] public float Radius { get; private set; } = 24f;

        public override ConditionTracker CreateTracker()
            => new AssaultApproachedTracker(Radius);
    }

    public class AssaultApproachedTracker : ConditionTracker
    {
        private const float PollInterval = 0.5f;

        private readonly float _radius;
        private CancellationTokenSource _polling;
        private bool _dispatched;

        public AssaultApproachedTracker(float radius)
        {
            _radius = radius;
        }

        // 닿았다는 이벤트가 없어 부대 위치를 주기적으로 본다
        public override void Subscribe()
        {
            Unsubscribe();
            Bus<EnemyDispatchEvent>.OnEvent[Owner.Enemy] += OnDispatched;
            Bus<AssaultsClearedEvent>.OnEvent[Owner.Enemy] += OnCleared;

            // 앞 스텝에서 이미 나와 있던 공세는 출발 신호를 놓친다. 지금 떠 있는 부대를 이어받는다
            _dispatched = AssaultRegistry.Forces.Count > 0;

            // 앞 스텝에서 나왔다가 이미 다 죽은 공세 — 여기서 기다리면 영영 안 끝난다
            if (!_dispatched && AssaultRegistry.ClearedCount > 0)
            {
                Complete();
                return;
            }

            _polling = new CancellationTokenSource();
            PollAsync(_polling.Token);
        }

        public override void Unsubscribe()
        {
            Bus<EnemyDispatchEvent>.OnEvent[Owner.Enemy] -= OnDispatched;
            Bus<AssaultsClearedEvent>.OnEvent[Owner.Enemy] -= OnCleared;
            _polling?.Cancel();
            _polling?.Dispose();
            _polling = null;
        }

        private void OnDispatched(EnemyDispatchEvent _)
            => _dispatched = true;

        // 공세가 오기도 전에 온 신호는 막아낸 것이 아니다
        private void OnCleared(AssaultsClearedEvent _)
        {
            if (_dispatched)
                Complete();
        }

        private async void PollAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (AnyForceArrived())
                    {
                        Complete();
                        return;
                    }

                    await Awaitable.WaitForSecondsAsync(PollInterval, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private bool AnyForceArrived()
        {
            float radiusSqr = _radius * _radius;

            foreach (AssaultForce force in AssaultRegistry.Forces)
            {
                if (force == null || force.Target == null)
                    continue;

                if (force.Brain != null && force.Brain.IsEngaged)
                    return true;

                Vector2 toTarget = (Vector2)force.Target.transform.position - (Vector2)force.Center;
                if (toTarget.sqrMagnitude <= radiusSqr)
                    return true;
            }

            return false;
        }
    }
}
