using System.Collections.Generic;
using Code.Buildings.Base;
using Code.Buildings.Data;
using Code.Commandable;
using Code.Core.Bus;
using Code.Economics;
using Code.GameEvents.Economy;
using Code.Military.Data;
using Code.Players;
using UnityEngine;

namespace Code.Buildings.Logic.ProductionBuilding
{
    /// <summary>
    /// 분대와 지휘관을 시간과 자원으로 뽑는 병영입니다. 주문 즉시 자원을 내고 BuildTime이 지나면 문 앞에 배치합니다.
    /// 예전에는 폰이 걸어 들어와 분대원이 됐는데, 그 방식은 폰을 너무 빨리 소모해 접었습니다(2026-09-07).
    /// 상한은 병영이 아니라 본부·지휘관이 정합니다(UserPopulation).
    /// </summary>
    public class SquadRecruitmentBuilding : AbstractBuilding
    {
        public UnlockableSO ActiveOrder { get; private set; }

        public override float Progress
            => ActiveOrder != null && ActiveOrder.BuildTime > 0f
                ? Mathf.Clamp01((Time.time - _startTime) / ActiveOrder.BuildTime)
                : 0f;

        public override UnlockableSO CurrentItem => ActiveOrder;

        public override IReadOnlyList<UnlockableSO> QueuedItems => _queuedOrders;

        public int QueuedOrderCount
            => _queuedOrders.Count;

        private SquadRecruitmentBuildingSO SquadBuildingSO
            => BuildingSO as SquadRecruitmentBuildingSO;

        // IReadOnlyList로 노출해 UI가 대기 목록을 그릴 수 있어야 하므로 List를 씁니다(최대 4개).
        private readonly List<UnlockableSO> _queuedOrders = new();
        private SquadDeployer _deployer;
        private float _startTime;

        private const int MaxQueuedOrders = 4;

        protected override void Awake()
        {
            base.Awake();

            // 문 앞(ApproachPoint)에서 내보낸다. SpawnPoint는 건물 위일 수 있어 그 자리에서는 길이 잡히지 않는다
            _deployer = new SquadDeployer(ApproachPoint != null ? ApproachPoint : SpawnPoint);
        }

        public override void CancelQueued(int index)
        {
            if (index < 0 || index >= _queuedOrders.Count)
                return;

            Refund(_queuedOrders[index]);
            _queuedOrders.RemoveAt(index);
            RaiseProductionChanged();
        }

        /// <summary>지금 만드는 것을 취소하고 환불합니다. 대기 중인 다음 주문이 바로 이어집니다.</summary>
        public override void CancelCurrent()
        {
            if (ActiveOrder == null)
                return;

            Refund(ActiveOrder);
            ActiveOrder = null;
            StartNextOrder();
            RaiseProductionChanged();
        }

        /// <summary>
        /// 주문 비용입니다. 분대는 병영 데이터의 생산 항목이 값을 정하고, 지휘관은 자기 데이터의 값을 씁니다.
        /// </summary>
        public ResourceCostSO GetCost(UnlockableSO item)
        {
            if (item is SquadSO squad)
                return SquadBuildingSO != null ? SquadBuildingSO.FindOption(squad)?.Cost : null;

            return item != null ? item.Cost : null;
        }

        public bool CanOrder(UnlockableSO item)
            => RefusalFor(item) == SquadOrderRefusal.None;

        public SquadOrderRefusal RefusalFor(UnlockableSO item)
        {
            if (item == null || !IsKnown(item))
                return SquadOrderRefusal.UnknownSquad;

            if (_queuedOrders.Count >= MaxQueuedOrders)
                return SquadOrderRefusal.QueueFull;

            // 지휘관은 상한을 올리는 쪽이라 상한에 걸리지 않는다
            if (item is SquadSO && !HasSquadSlot())
                return SquadOrderRefusal.PopulationCap;

            if (UserSupplies.Instance == null || !UserSupplies.Instance.HasEnoughSupplies(Owner, GetCost(item)))
                return SquadOrderRefusal.NotEnoughSupplies;

            return SquadOrderRefusal.None;
        }

        /// <summary>
        /// 주문합니다. 자원은 지금 내고, 취소하면 그대로 돌려받습니다.
        /// </summary>
        public bool TryOrder(UnlockableSO item)
        {
            if (!CanOrder(item))
                return false;

            ResourceCostSO cost = GetCost(item);
            if (cost != null)
                Bus<ResourceChangedEvent>.Raise(Owner, new ResourceChangedEvent(Owner, cost));

            _queuedOrders.Add(item);
            if (ActiveOrder == null)
                StartNextOrder();

            RaiseProductionChanged();
            return true;
        }

        private bool IsKnown(UnlockableSO item)
            => item switch
            {
                SquadSO squad => SquadBuildingSO != null && SquadBuildingSO.FindOption(squad) != null,
                CommanderSO commander => commander.Prefab != null,
                _ => false
            };

        private void StartNextOrder()
        {
            if (_queuedOrders.Count == 0)
                return;

            ActiveOrder = _queuedOrders[0];
            _queuedOrders.RemoveAt(0);
            _startTime = Time.time;
        }

        private void Update()
        {
            if (ActiveOrder == null || Time.time - _startTime < ActiveOrder.BuildTime)
                return;

            // 지휘관도 분대와 같은 문으로 걸어 나온다. 갑자기 나타나면 어디서 왔는지 모른다
            if (ActiveOrder is AbstractCommandableSO built)
                _ = _deployer.DeployAsync(built, Owner, SquadBuildingSO.RallyOffset, destroyCancellationToken);

            ActiveOrder = null;
            StartNextOrder();
            RaiseProductionChanged();
        }

        private void Refund(UnlockableSO item)
        {
            ResourceCostSO cost = GetCost(item);
            if (cost != null)
                Bus<ResourceChangedEvent>.Raise(Owner, new ResourceChangedEvent(Owner, cost, 1, true));
        }

        private bool HasSquadSlot()
        {
            if (UserPopulation.Instance == null)
                return true;

            int reserved = 0;
            foreach (UnlockableSO order in _queuedOrders)
                if (order is SquadSO)
                    reserved++;

            if (ActiveOrder is SquadSO)
                reserved++;

            return UserPopulation.Instance.GetSquadCount(Owner) + reserved
                   < UserPopulation.Instance.GetSquadCapacity(Owner);
        }
    }
}
