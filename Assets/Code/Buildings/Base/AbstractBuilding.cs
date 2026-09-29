using System;
using System.Collections.Generic;
using Code.Buildings.Data;
using Code.Buildings.Interfaces;
using Code.Buildings.Utils;
using Code.Commandable;
using Code.Core.Bus;
using Code.Effects;
using Code.GameEvents.Building;
using Code.GameEvents.Economy;
using Code.GameEvents.System;
using Code.GameEvents.Unit;
using Code.Navigation.Baking;
using ObjectPool.RunTime;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Code.Buildings.Base
{
    public abstract class AbstractBuilding : AbstractCommandable, IBuilding, IProductionDisplay
    {
        [Header("Settings & Assets")]
        [field: SerializeField] public BuildingSO BuildingSO { get; set; }
        [field: SerializeField] public PoolingItemSO DestroyEffect { get; private set; }

        [Header("References")]
        [SerializeField] private SpriteRenderer buildingRenderer;
        [field: SerializeField] public Transform SpawnPoint { get; private set; }

        /// <summary>
        /// 유닛이 이 건물에 접근할 때 사용하는 위치입니다. 프리팹의 ApproachPoint 자식을 자동 탐색합니다.
        /// </summary>
        public Transform ApproachPoint { get; private set; }

        public UnlockableSO SOBeingBuilt => _queue.SOBeingBuilt;
        public float CurrentQueueStartTime => _queue.CurrentQueueStartTime;

        public delegate void QueueUpdatedEvent(IReadOnlyList<UnlockableSO> unitsInQueue);
        public event QueueUpdatedEvent OnQueueUpdated;

        public int QueueSize => _queue.Count;
        public IReadOnlyList<UnlockableSO> Queue => _queue.Items;

        /// <summary>
        /// 생산 상태가 바뀔 때 발생합니다. UI 갱신용 공통 창구입니다.
        /// </summary>
        public event Action ProductionChanged;

        /// <summary>
        /// 현재 생산 중인 대상입니다. 기본 구현은 시간 기반 생산 큐를 따릅니다.
        /// </summary>
        public virtual UnlockableSO CurrentItem => SOBeingBuilt;

        /// <summary>
        /// 경과 시간을 BuildTime으로 나눈 진행도입니다.
        /// </summary>
        public virtual float Progress
        {
            get
            {
                UnlockableSO current = SOBeingBuilt;
                if (current == null || current.BuildTime <= 0f)
                    return 0f;

                return Mathf.Clamp01((Time.time - CurrentQueueStartTime) / current.BuildTime);
            }
        }

        /// <summary>
        /// 대기 중인 생산 목록입니다.
        /// </summary>
        // 대기 목록은 진행 중인 항목을 뺀다. 큐는 그것을 0번에 두므로 UI에 그대로 주면 하나가 둘로 보인다
        public virtual IReadOnlyList<UnlockableSO> QueuedItems => _waiting;

        public virtual void CancelCurrent()
        {
            if (SOBeingBuilt != null)
                CancelGenerate(0);
        }

        /// <summary>
        /// 대기 목록의 주문 하나를 취소합니다.
        /// </summary>
        public virtual void CancelQueued(int index)
            => CancelGenerate(index + (SOBeingBuilt != null ? 1 : 0)); // 대기 목록은 진행 중인 것을 뺀 인덱스다

        /// <summary>
        /// 생산 상태 변경을 알립니다. 파생 클래스가 자체 생산 흐름에서 호출합니다.
        /// </summary>
        protected void RaiseProductionChanged()
            => ProductionChanged?.Invoke();

        private const string ApproachPointName = "ApproachPoint";

        private UnitProductionQueue _queue;
        private WaitingQueueView _waiting;

        // 지금까지 이 건물이 뽑은 수. 스폰 자리를 흩는 순번으로만 쓴다
        private int _spawnIndex;

        protected override void Awake()
        {
            base.Awake();
            if (buildingRenderer == null)
                buildingRenderer = GetComponentInChildren<SpriteRenderer>();

            ApproachPoint = transform.Find(ApproachPointName) ?? transform;

            _queue = new UnitProductionQueue(SpawnBuiltUnit);
            _waiting = new WaitingQueueView(_queue);
            _queue.Updated += items =>
            {
                OnQueueUpdated?.Invoke(items);
                RaiseProductionChanged();
            };
        }

        /// <summary>
        /// 시야에 들어왔을 때 잔상 제거를 위한 VisibleStateChangeEvent를 발행합니다.
        /// </summary>
        protected override void OnGainVisibility()
        {
            base.OnGainVisibility();
            if (buildingRenderer != null)
                Bus<VisibleStateChangeEvent>.Raise(Owner,
                    new VisibleStateChangeEvent(Owner, transform, buildingRenderer, true));
        }

        /// <summary>
        /// 시야에서 벗어났을 때 잔상 생성을 위한 VisibleStateChangeEvent를 발행합니다.
        /// </summary>
        protected override void OnLoseVisibility()
        {
            base.OnLoseVisibility();
            if (buildingRenderer != null)
                Bus<VisibleStateChangeEvent>.Raise(Owner,
                    new VisibleStateChangeEvent(Owner, transform, buildingRenderer, false));
        }

        protected override void Start()
        {
            base.Start();
            SnapToNavGrid();
            Bus<BuildingSpawnEvent>.Raise(Owner, new BuildingSpawnEvent(Owner, this));

            if (SpawnPoint == null)
                SpawnPoint = transform;

            if (GetComponent<BuildingFootprintIndicator>() == null)
                gameObject.AddComponent<BuildingFootprintIndicator>();
        }

        /// <summary>
        /// 건물의 위치를 내비게이션 그리드에 맞춰 스냅합니다.
        /// 건물 크기와 그리드 크기를 고려하여 가장 가까운 유효 위치로 이동합니다.
        /// </summary>
        private void SnapToNavGrid()
        {
            if (BuildingSO == null || PathBaker.Instance == null)
                return;

            transform.position = PathBaker.Instance.SnapToGrid(transform.position, BuildingSO.Size);
        }

        /// <summary>
        /// 건설이 막 끝났을 때 내려앉는 연출을 재생합니다. 파괴 연출과 짝을 이룹니다.
        /// </summary>
        public async void PlayBuildComplete()
        {
            try
            {
                if (buildingRenderer != null)
                    await BuildCompleteEffect.SettleAsync(buildingRenderer, destroyCancellationToken);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        /// <summary>
        /// 건물 파괴 시 DestroyEffect가 지정되어 있으면 건물 위치에서 폭발 이펙트를 재생한 뒤 파괴를 진행합니다.
        /// </summary>
        public override async void Die()
        {
            // 흔드는 동안 살아 있으므로 사망 처리를 먼저 확정한다 — 콜라이더가 꺼져 더는 공격 대상이 아니다.
            if (!InitDeath())
                return;

            try
            {
                // 무너뜨리는 대상은 루트가 아니라 본체 스프라이트다. 루트를 움직이면 Y 정렬이 매 프레임 요동친다.
                if (buildingRenderer != null)
                    await DeathShakeEffect.CollapseAsync(buildingRenderer, destroyCancellationToken);

                if (DestroyEffect != null)
                    Bus<PoolEffectPlayEvent>.Raise(Owner,
                        new PoolEffectPlayEvent(DestroyEffect, transform.position, Quaternion.identity, 0f));

                // 건물이 사라지기 전에 잔해를 남긴다. 렌더러 정보를 가져와야 하므로 파괴보다 먼저다.
                if (BuildingSO != null)
                    BuildingRuins.Spawn(BuildingSO.DestroyedSprite, buildingRenderer,
                        transform.position, BuildingSO.RuinsLifetime, Owner);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
            finally
            {
                // 연출이 어떻게 끝나든 건물은 반드시 사라진다.
                if (this != null)
                    Destroy(gameObject);
            }
        }

        protected override void OnDestroy()
        {
            _queue?.Dispose();

            base.OnDestroy();
            Bus<BuildingDeathEvent>.Raise(Owner, new BuildingDeathEvent(Owner, this));
        }

        /// <summary>
        /// 완성된 UnlockableSO로부터 유닛을 스폰합니다. 서브클래스에서 재정의 가능합니다.
        /// </summary>
        protected virtual void SpawnBuiltUnit(UnlockableSO built)
        {
            if (built is not AbstractCommandableSO commandableSO || commandableSO.Prefab == null)
                return;

            // 한 점에 계속 뱉으면 생산한 유닛이 서로 포개져 몇 기인지 안 보인다
            GameObject newUnit = Instantiate(commandableSO.Prefab,
                SpawnScatter.Around(SpawnPoint.position, _spawnIndex++), SpawnPoint.rotation);
            if (newUnit.TryGetComponent(out AbstractCommandable commandable))
                commandable.Owner = Owner;
        }

        /// <summary>
        /// 유닛/업그레이드를 생산 큐에 추가하고 비용을 즉시 차감합니다.
        /// AssemblyBuilding 활성화 시 UserSupplies.ProductionCostMultiplier가 적용됩니다.
        /// </summary>
        public virtual void BuildUnlockable(UnlockableSO unlockable)
        {
            if (_queue.IsFull)
                return;

            Bus<ResourceChangedEvent>.Raise(Owner, new ResourceChangedEvent(Owner, unlockable.Cost)); // 자원 차감
            _queue.Enqueue(unlockable);
        }

        /// <summary>
        /// 생산 큐에서 지정 인덱스 항목을 취소하고 비용을 전액 환불합니다.
        /// </summary>
        public void CancelGenerate(int index)
        {
            if (index < 0 || index >= _queue.Count)
                return;

            UnlockableSO unlockableToCancel = _queue.Items[index];
            Bus<ResourceChangedEvent>.Raise(Owner,
                new ResourceChangedEvent(Owner, unlockableToCancel.Cost, 1, true)); // 자원 환불

            _queue.Cancel(index);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (BuildingSO == null)
                return;

            bool inPrefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null;
            if (!inPrefabStage && !Selection.Contains(gameObject))
                return;

            BuildingFootprint.DrawGizmo(transform.position, BuildingSO.Size, Color.cyan);
        }
#endif
    }
}
