using Code.Buildings.Interfaces;
using Code.Buildings.Logic.Shared;
using Code.Core.Bus;
using Code.Economics;
using Code.GameEvents.Economy;
using Code.Military.Squads;
using Code.Players;

namespace Code.Military.Reinforcement
{
    /// <summary>
    /// 분대의 결원을 거점 범위 안에서 한 명씩 채웁니다.
    /// 플레이어가 켜야 시작되고, 정원이 차거나 비용을 못 내면 스스로 꺼집니다.
    /// </summary>
    public class SquadReinforcement
    {
        /// <summary>
        /// 보충이 진행 중인지 여부입니다.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 다음 한 명까지의 진행도(0~1)입니다.
        /// </summary>
        public float Progress
            => _interval > 0f ? _elapsed / _interval : 0f;

        private readonly Squad _squad;
        private readonly SquadRoster _roster;
        private readonly ResourceCostSO _memberCost;

        private float _elapsed;
        private float _interval;

        public SquadReinforcement(Squad squad, SquadRoster roster, ResourceCostSO memberCost)
        {
            _squad = squad;
            _roster = roster;
            _memberCost = memberCost;
        }

        /// <summary>
        /// 보충을 시작합니다.
        /// </summary>
        public void Begin()
        {
            if (IsActive || _squad.MissingCount <= 0)
                return;

            IsActive = true;
            _elapsed = 0f;
        }

        /// <summary>
        /// 보충을 멈추고 쌓인 진행 시간을 버립니다.
        /// </summary>
        public void Stop()
        {
            IsActive = false;
            _elapsed = 0f;
            _interval = 0f;
        }

        /// <summary>
        /// 이 분대를 지금 보충해 줄 수 있는 거점입니다. 범위 밖이면 null입니다.
        /// </summary>
        public IReinforcePoint CoveringPoint()
            => ReinforcePointRegistry.FindCovering(_squad.Owner, _squad.transform.position);

        /// <summary>
        /// 보충 시간을 흘립니다. 분대가 매 프레임 호출합니다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsActive)
                return;

            if (_squad.MissingCount <= 0)
            {
                Stop();
                return;
            }

            IReinforcePoint point = CoveringPoint();

            // 진행 시간을 유지한 채 멈춘다 — 거점으로 돌아오면 이어서 채워진다
            if (point == null)
                return;

            _interval = point.ReinforceInterval;

            _elapsed += deltaTime;
            if (_elapsed < _interval)
                return;

            _elapsed = 0f;

            if (!TryPay())
            {
                Stop();
                return;
            }

            _roster.ReinforceOne();
        }

        /// <summary>
        /// 자원을 지불합니다. 폰은 더 이상 소모하지 않습니다(2026-09-07) — 분대 유지가 일꾼을 갉아먹었습니다.
        /// </summary>
        private bool TryPay()
        {
            UserSupplies supplies = UserSupplies.Instance;
            if (_memberCost != null && (supplies == null || !supplies.HasEnoughSupplies(_squad.Owner, _memberCost)))
                return false;

            if (_memberCost != null)
                Bus<ResourceChangedEvent>.Raise(_squad.Owner, new ResourceChangedEvent(_squad.Owner, _memberCost));

            return true;
        }
    }
}
