using System;
using System.Collections.Generic;
using Code.Core;
using Code.Core.Bus;
using Code.Economics;
using Code.GameEvents.Economy;
using UnityEngine;

namespace Code.Players
{
    /// <summary>
    /// 플레이어 자원 관리 싱글톤입니다.
    /// trackedResources에 등록된 ResourceSO를 Owner별로 추적합니다.
    /// SupplyEvent를 수신해 자원량을 갱신하며, 최솟값 0 / 최댓값 SO.MaxAmount를 보장합니다.
    /// </summary>
    public class UserSupplies : MonoSingleton<UserSupplies>
    {
        [SerializeField] private ResourceSO[] trackedResources; // 추적할 자원 종류 목록
        [SerializeField] private ResourceAmount[] initialAmounts; // 게임 시작 시 초기 보유량

        /// <summary>
        /// 이 판에서 실제로 세는 자원 목록입니다. 여기 없는 ResourceSO는 아무리 지급해도 무시됩니다.
        /// </summary>
        public IReadOnlyList<ResourceSO> TrackedResources => trackedResources;

        public delegate void SupplyChanged(Owner owner, int newAmount, ResourceSO resourceType);
        public delegate void SupplyUpdateValue(Owner owner, int amount, ResourceSO resourceType);

        /// <summary>
        /// 자원량이 변경될 때마다 발행됩니다.
        /// </summary>
        public event SupplyChanged OnSupplyChanged;

        /// <summary>
        /// 얼마만큼 자원이 변경되었는지 발행됩니다. (예: +100, -50)
        /// </summary>
        public event SupplyUpdateValue OnSupplyUpdateValue;

        /// <summary>
        /// ResourceSO → Owner → 현재 보유량
        /// </summary>
        private Dictionary<ResourceSO, Dictionary<Owner, int>> _resources;
        private BusSubscriptionScope _subscriptionScope;

        protected override void Awake()
        {
            base.Awake();
            InitializeResources();
            _subscriptionScope = new BusSubscriptionScope();
            _subscriptionScope.SubscribeAll<SupplyEvent>(HandleSupplyEvent);
            _subscriptionScope.SubscribeAll<ResourceChangedEvent>(HandleResourceChanged);
        }

        protected override void OnDestroy()
        {
            _subscriptionScope?.Dispose();
            base.OnDestroy();
        }

        private void InitializeResources()
        {
            _resources = new Dictionary<ResourceSO, Dictionary<Owner, int>>();
            foreach (ResourceSO resource in trackedResources)
            {
                if (resource == null)
                    continue;

                Dictionary<Owner, int> ownerDict = new Dictionary<Owner, int>();
                foreach (Owner owner in Enum.GetValues(typeof(Owner)))
                    ownerDict[owner] = 0;

                _resources[resource] = ownerDict;
            }

            foreach (ResourceAmount entry in initialAmounts)
            {
                if (entry.Resource == null || !_resources.TryGetValue(entry.Resource, out var ownerDict))
                    continue;

                ownerDict[Owner.Player] = Mathf.Clamp(entry.Amount, 0, int.MaxValue);
            }
        }

        private void HandleSupplyEvent(SupplyEvent evt)
        {
            Supply(evt.Owner, evt.Amount, evt.Supply);
        }

        /// <summary>
        /// 자원 처리 로직입니다. 지정된 Owner의 자원량을 갱신하고 OnSupplyChanged 이벤트를 발행합니다.
        /// </summary>
        private void Supply(Owner owner, int amount, ResourceSO resource)
        {
            if (resource == null || !_resources.TryGetValue(resource, out var ownerDict))
                return;

            int previous = ownerDict[owner];
            int clamped = Mathf.Clamp(previous + amount, 0, int.MaxValue);
            ownerDict[owner] = clamped;

            OnSupplyChanged?.Invoke(owner, clamped, resource);

            // 0에서 더 깎이면 요청량과 실제 변화량이 다르다. 표시에는 실제로 바뀐 값을 넘긴다.
            OnSupplyUpdateValue?.Invoke(owner, clamped - previous, resource);
        }

        private void HandleResourceChanged(ResourceChangedEvent evt)
        {
            if (evt.Cost == null)
                return;

            // 환불 여부와 배율을 적용한 비용으로 자원을 갱신합니다.
            foreach (ResourceAmount entry in evt.Cost.Costs)
            {
                if (entry.Amount == 0)
                    continue;

                if (evt.IsRefund)
                    Supply(evt.Owner, Mathf.RoundToInt(entry.Amount * evt.Multiplier), entry.Resource);
                else
                    Supply(evt.Owner, -Mathf.RoundToInt(entry.Amount * evt.Multiplier), entry.Resource);
            }
        }

        /// <summary>
        /// 지정 Owner가 특정 ResourceCostSO에 정의된 모든 자원을 충분히 보유하고 있는지 확인합니다.
        /// </summary>
        public bool HasEnoughSupplies(Owner owner, ResourceCostSO cost)
        {
            if (cost == null)
                return true;

            foreach (ResourceAmount entry in cost.Costs)
                if (GetCurrentAmount(owner, entry.Resource) < entry.Amount)
                    return false;

            return true;
        }

        /// <summary>
        /// 지정 Owner가 보유한 특정 자원의 현재량을 반환합니다.
        /// 사전에 없는 자원 종류는 0입니다.
        /// </summary>
        public int GetCurrentAmount(Owner owner, ResourceSO resource)
        {
            if (resource == null || !_resources.TryGetValue(resource, out var ownerDict))
                return 0;

            return ownerDict.GetValueOrDefault(owner, 0);
        }

#if UNITY_EDITOR
        [Header("디버그")]
        [SerializeField] private ResourceSO debugResource;  // 지급할 자원. 비워두면 추적 중인 모든 자원을 지급한다
        [SerializeField] private int debugAmount = 100;     // 지급할 수량

        /// <summary>
        /// 플레이 중 플레이어에게 자원을 지급합니다. debugResource가 비어 있으면 전체 지급합니다.
        /// </summary>
        [Sirenix.OdinInspector.Button("자원 지급(디버그)")]
        private void DebugGrantSupplies()
        {
            if (!Application.isPlaying)
                return;

            if (debugResource != null)
            {
                Supply(Owner.Player, debugAmount, debugResource);
                return;
            }

            foreach (ResourceSO resource in trackedResources)
                if (resource != null)
                    Supply(Owner.Player, debugAmount, resource);
        }
#endif
    }
}

