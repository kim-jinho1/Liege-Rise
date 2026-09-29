using Code.Buildings.Base;
using Code.Buildings.Data;
using Code.Buildings.Interfaces;
using Code.Buildings.Utils;
using Code.Commandable;
using Code.Core.Bus;
using Code.GameEvents.Building;
using Code.GameEvents.Economy;
using Code.Navigation.Baking;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Code.Buildings.Construction
{
    /// <summary>
    /// 건설 중인 건물의 더미 오브젝트입니다.
    /// </summary>
    public class ConstructionDummy : AbstractCommandable, IBuilding, IBuildingConstructor
    {
        [field: SerializeField] public ConstructionProgress ProgressData { get; private set; }
            = new(BuildingState.Ghost, 0, 0);

        [SerializeField] private SpriteRenderer buildingRenderer;

        [Header("Build Pulse")]
        [SerializeField, Range(0f, 0.5f)] private float pulseStrength = 0.12f;
        [SerializeField, Min(0.02f)] private float pulseDuration = 0.12f;

        public BuildingSO BuildingSO { get; private set; }

        private SortingGroup _sortingGroup;
        private Transform _pulseTarget;
        private Vector3 _pulseBaseScale;
        private float _pulseTimer;
        private bool _cancelled;

        /// <summary>
        /// 현재 건설 속도 배율입니다(작업 폰 수). 일시정지 시 0, 무인 건설은 1.
        /// </summary>
        public float ConstructionSpeed { get; private set; } = 1f;

        /// <summary>
        /// 건설 더미는 항상 건설 중 상태입니다. 취소 명령의 대상이 됩니다.
        /// </summary>
        public bool IsBuilding => true;

        /// <summary>
        /// 건설 더미는 일꾼이 아니므로 건설에 배정될 수 없습니다.
        /// </summary>
        public bool IsAvailable => false;

        protected override void Awake()
        {
            base.Awake();
            if (buildingRenderer == null)
                buildingRenderer = GetComponentInChildren<SpriteRenderer>();

            _sortingGroup = GetComponent<SortingGroup>();
            BuildingSO = AbstractCommandableSO as BuildingSO;
            Debug.Assert(BuildingSO != null, $"BuildingSO is not assigned in {gameObject.name}");
        }

        protected override void Start()
        {
            base.Start();

            // base.Start가 만피로 채운 것을 되돌린다. 이벤트를 함께 쏘지 않으면
            // 이미 바인딩된 정보 패널이 만피 표시로 굳는다.
            int fullHealth = CurrentHealth;
            CurrentHealth = 0;
            RaiseHealthUpdated(fullHealth, 0);

            if (GetComponent<BuildingFootprintIndicator>() == null)
                gameObject.AddComponent<BuildingFootprintIndicator>();

            ResolvePulseTarget();
        }

        /// <summary>
        /// 스케일을 흔들 대상을 정합니다. 루트를 흔들면 발판 표시·자식 UI까지 같이 늘어나므로
        /// 스프라이트가 루트에 직접 붙어 있으면 연출을 포기합니다.
        /// </summary>
        private void ResolvePulseTarget()
        {
            if (buildingRenderer == null || buildingRenderer.transform == transform)
                return;

            _pulseTarget = buildingRenderer.transform;
            _pulseBaseScale = _pulseTarget.localScale;
        }

        /// <summary>
        /// 일꾼이 망치를 한 번 내려칠 때 호출합니다. 스프라이트가 세로로 잠깐 늘었다 돌아옵니다.
        /// 여러 일꾼이 붙어 있으면 각자의 망치질마다 다시 시작됩니다.
        /// </summary>
        public void PlayBuildPulse()
        {
            if (_pulseTarget != null)
                _pulseTimer = pulseDuration;
        }

        private void Update()
        {
            if (_pulseTimer <= 0f || _pulseTarget == null)
                return;

            _pulseTimer -= Time.deltaTime;
            if (_pulseTimer <= 0f)
            {
                _pulseTarget.localScale = _pulseBaseScale;
                return;
            }

            // 0에서 시작해 중간에 최대로 늘었다가 다시 0으로 돌아온다.
            float amount = Mathf.Sin(_pulseTimer / pulseDuration * Mathf.PI) * pulseStrength;
            _pulseTarget.localScale = new Vector3(
                _pulseBaseScale.x * (1f - amount * 0.5f),
                _pulseBaseScale.y * (1f + amount),
                _pulseBaseScale.z);
        }

        /// <summary>
        /// 건설 진행 상태·속도를 갱신하고 리빌 표시를 동기화합니다.
        /// </summary>
        public void SetProgress(BuildingState state, float progress, float speed)
        {
            ProgressData = new ConstructionProgress(state, ProgressData.StartTime, progress);
            ConstructionSpeed = speed;
        }

        /// <summary>
        /// 완성된 건물을 스폰하고 Dummy를 파괴합니다.
        /// </summary>
        public void CompleteAndSpawn()
        {
            GameObject building = Instantiate(BuildingSO.Prefab, transform.position, Quaternion.identity);
            AbstractBuilding abstractBuilding = building.GetComponent<AbstractBuilding>();
            abstractBuilding.Owner = Owner;
            Bus<BuildingSetupCompleteEvent>.Raise(abstractBuilding.Owner,
                new BuildingSetupCompleteEvent(abstractBuilding.Owner, abstractBuilding));

            if (IsSelected)
                abstractBuilding.Select();

            abstractBuilding.PlayBuildComplete();

            ConstructionComplete();
            Destroy(gameObject);
        }

        /// <summary>
        /// 건설 완료 시 선택 상태를 해제합니다.
        /// </summary>
        public void ConstructionComplete()
        {
            if (IsSelected)
                DeSelect();
        }

        /// <summary>
        /// 건설을 취소하고 비용의 80%를 환급한 뒤 Dummy를 파괴합니다.
        /// 배정된 일꾼은 OnDestroy에서 자동으로 정리됩니다.
        /// </summary>
        public void CancelConstruction()
        {
            // Destroy는 프레임 끝에야 반영되고, 일꾼마다 이 현장 참조를 따로 들고 있다.
            // 막지 않으면 취소 한 번에 환불이 인원수만큼 나간다.
            if (_cancelled)
                return;

            _cancelled = true;
            Bus<ResourceChangedEvent>.Raise(Owner,
                new ResourceChangedEvent(Owner, BuildingSO.Cost, 0.8f, true));

            Destroy(gameObject);
        }

        /// <summary>
        /// 유닛 배정 없이 건물을 배치할 때 호출합니다. 이후 StartPawnConstruction 또는
        /// StartWorkerlessConstruction으로 건설을 시작합니다.
        /// </summary>
        public void InitializeAsWaiting(Owner owner)
        {
            Owner = owner;
            ProgressData = new ConstructionProgress(BuildingState.Paused, 0, 0);
            PathBaker baker = PathBaker.Instance;
            if (baker != null)
                baker.RebakeAtConstruction(transform.position);
        }

        /// <summary>
        /// 폰 배정 기반 건설을 시작합니다. 유휴 폰이 자동 배정되며, 폰이 현장에 있는 동안만 진행됩니다.
        /// </summary>
        public PawnConstructionRunner StartPawnConstruction()
        {
            ProgressData = new ConstructionProgress(BuildingState.Paused, Time.time, 0f);
            PawnConstructionRunner runner = gameObject.AddComponent<PawnConstructionRunner>();
            runner.Begin(this);
            return runner;
        }

        /// <summary>
        /// 현장 본체의 정렬 순서입니다. 건설 중인 일꾼이 이보다 앞에 그려져야 가리지 않습니다.
        /// </summary>
        public int SortingOrder
            => _sortingGroup != null ? _sortingGroup.sortingOrder : 0;

        public const int WorkSlotCount = 4;

        /// <summary>
        /// 슬롯 인덱스에 해당하는 외곽 작업 위치(하/우/상/좌 가장자리 중앙, 외곽선 바깥)를 반환합니다.
        /// </summary>
        public Vector3 GetWorkPosition(int slotIndex)
        {
            float workMargin = BuildingSO.WorkMargin;
            Vector2 half = BuildingFootprint.HalfExtents(BuildingSO.Size);
            Vector3 center = transform.position;

            return (slotIndex % WorkSlotCount) switch
            {
                0 => center + new Vector3(0f, -(half.y + workMargin), 0f),
                1 => center + new Vector3(half.x + workMargin, 0f, 0f),
                2 => center + new Vector3(0f, half.y + workMargin, 0f),
                _ => center + new Vector3(-(half.x + workMargin), 0f, 0f)
            };
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (BuildingSO == null)
                return;

            bool inPrefabStage = PrefabStageUtility.GetCurrentPrefabStage() != null;
            if (!inPrefabStage && !Selection.Contains(gameObject))
                return;

            BuildingFootprint.DrawGizmo(transform.position, BuildingSO.Size, new Color(1f, 0.8f, 0f));
        }
#endif
    }
}
