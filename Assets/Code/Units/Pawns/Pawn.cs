using Code.Animations;
using Code.Buildings.Base;
using Code.Buildings.Construction;
using Code.Buildings.Interfaces;
using Code.Commands.Base;
using Code.Core;
using Code.Effects;
using Code.Environments;
using Code.Units.Base;
using Code.Units.BT;
using Unity.Behavior;
using UnityEngine;

namespace Code.Units.Pawns
{
    /// <summary>
    /// 일꾼 유닛의 핵심 로직을 담당합니다.
    /// 채집(Gather), 건설(Construction) 업무를 수행합니다.
    /// </summary>
    public class Pawn : AbstractUnit, IBuildingConstructor
    {
        [Header("Pawn Setting")]
        [SerializeField] private BaseCommandSO[] cancelConstructionCommandSet;
        [SerializeField] private CommandableFinderSO headQuarterFinder;
        [SerializeField] private ParameterSO interactParam; // 상주 건물에서 작업하는 모션
        [SerializeField] private ParameterSO pawnIndexParam; // 손에 드는 도구를 고르는 블렌드 값
        [SerializeField] private ParameterSO stationMoveParam; // 상주 중 걸을 때 쓰는 이동 파라미터
        [SerializeField] private ParameterSO transportParam; // 짐을 진 채 상주하면 운반 블렌드(3 = Iron)가 뜬다. 상주 시 끈다

        public bool IsBuilding
            => GetVariable(BTVariables.Command, out BlackboardVariable<UnitCommands> commandVariable) &&
               commandVariable.Value == UnitCommands.ConstructBuilding;

        /// <summary>
        /// 현재 채집 또는 자원 귀환 중인지 여부입니다.
        /// </summary>
        public bool IsGathering
            => GetVariable(BTVariables.Command, out BlackboardVariable<UnitCommands> cmd) &&
               cmd.Value is UnitCommands.Gather or UnitCommands.ReturnSupplies;

        /// <summary>
        /// 유저가 수동으로 지정한 납품 대상 건물입니다. null이면 자동(가장 가까운 본성).
        /// </summary>
        public GameObject ManualHeadQuarter { get; private set; }

        /// <summary>
        /// 현재 채집 목표 자원 오브젝트입니다. BT 액션에서 직접 참조합니다.
        /// </summary>
        public ResourceObject TargetResource { get; set; }

        /// <summary>
        /// 현재 상주 중인 건물입니다. 상주 중이 아니면 null. BT 액션이 설정합니다.
        /// </summary>
        public AbstractBuilding StationedBuilding { get; set; }

        /// <summary>
        /// 자동 배정(건설 등) 가능 여부입니다. Stop 상태인 Pawn만 대상입니다.
        /// </summary>
        public bool IsAvailable
            => GetVariable(BTVariables.Command, out BlackboardVariable<UnitCommands> cmd) &&
               cmd.Value == UnitCommands.Stop;

        public bool HasSupplies { get; private set; }
        public int Capacity { get; private set; }

        private const int DefaultCarryCapacity = 5;

        private Renderer[] _allRenderers;
        private Collider2D[] _colliders2D;
        private Collider[] _colliders3D;

        protected override void Awake()
        {
            base.Awake();
            Capacity = (AbstractCommandableSO as PawnSO)?.CarryCapacity ?? DefaultCarryCapacity;
            _allRenderers = GetComponentsInChildren<Renderer>(true);
            _colliders2D = GetComponentsInChildren<Collider2D>(true);
            _colliders3D = GetComponentsInChildren<Collider>(true);
        }

        protected override void Start()
        {
            base.Start();
            if (GetVariable(BTVariables.SupplyAmountHeld, out BlackboardVariable<int> supplyAmountHeld))
                supplyAmountHeld.OnValueChanged += () => SetActiveBackpack(supplyAmountHeld.Value > 0);
        }

        private void SetActiveBackpack(bool isActive)
            => HasSupplies = isActive;

        /// <summary>
        /// 대상 GameObject에서 건물의 ApproachPoint를 해석합니다. 건물이 아니면 원본을 반환합니다.
        /// </summary>
        private static GameObject ResolveApproach(GameObject target)
        {
            if (target == null)
                return null;

            AbstractBuilding building = target.GetComponentInParent<AbstractBuilding>();
            return building != null ? building.ApproachPoint.gameObject : target;
        }

        /// <summary>
        /// 지정한 ResourceObject를 채집 목표로 설정하고 Gather 커맨드를 시작합니다.
        /// 이미 짐을 지고 있으면 먼저 납품하고 그 다음에 새 자원으로 갑니다.
        /// </summary>
        public void Gather(ResourceObject resource)
        {
            ManualHeadQuarter = null;
            TargetResource = resource;

            // 바꾸는 것은 "캐러 갈 자원"뿐이다. "지금 든 자원"(SupplySO)을 덮어쓰면 운반 중인 짐이 바뀐다
            SetVariableValue(BTVariables.ResourceFilter, resource.ResourceSO);
            SetVariableValue(BTVariables.HeadQuarter, ResolveApproach(headQuarterFinder.Value?.gameObject));

            // 납품이 끝나면 BT가 Command를 Gather로 되돌리고 TargetResource를 그대로 이어받는다
            SetVariableValue(BTVariables.Command,
                HasSupplies ? UnitCommands.ReturnSupplies : UnitCommands.Gather);
        }

        /// <summary>
        /// 납품 대상 건물을 수동 지정합니다. Gather 루프 중이면 즉시 반영됩니다.
        /// </summary>
        public void SetDeliveryTarget(GameObject target)
        {
            GameObject approach = ResolveApproach(target);
            ManualHeadQuarter = approach;
            SetVariableValue(BTVariables.HeadQuarter, approach);
        }

        public void ReturnSupplies(GameObject headQuarter)
        {
            GameObject approach = ResolveApproach(headQuarter);
            ManualHeadQuarter = approach;
            SetVariableValue(BTVariables.HeadQuarter, approach);
            SetVariableValue(BTVariables.Command, UnitCommands.ReturnSupplies);
        }

        /// <summary>
        /// 지정한 ConstructionDummy를 건설 대상으로 설정하고 ConstructBuilding 커맨드를 시작합니다.
        /// </summary>
        public void AssignToConstruction(ConstructionDummy dummy)
        {
            ManualHeadQuarter = null;
            TargetResource = null;

            SetVariableValue(BTVariables.ConstructionDummySO, dummy);
            SetVariableValue(BTVariables.TargetGameObject, dummy.gameObject);
            SetVariableValue(BTVariables.Command, UnitCommands.ConstructBuilding);
        }

        /// <summary>
        /// 폰 배치형 건물로 이동해 상주(숨김 점유)를 시작합니다.
        /// </summary>
        public void StationAt(AbstractBuilding building)
        {
            ManualHeadQuarter = null;
            TargetResource = null;
            SetVariableValue(BTVariables.TargetGameObject, building.gameObject);
            SetVariableValue(BTVariables.Command, UnitCommands.StationedAtBuilding);
        }

        /// <summary>
        /// 상주 중 시각·충돌 요소를 끄거나 복원합니다(논리 점유 — GameObject는 활성 유지).
        /// </summary>
        public void SetHiddenInBuilding(bool hidden, System.Action onHidden = null)
        {
            // 줄어드는 모습을 보여 준 뒤에 끈다. 먼저 끄면 그냥 사라진 것으로 보인다.
            // 자리를 옮기는 일도 그때 해야 문 앞에서 줄어든 뒤에 안으로 들어간다
            if (hidden)
            {
                SpawnScaleEffect.PlayOut(gameObject, () =>
                {
                    ApplyHiddenInBuilding(true);
                    onHidden?.Invoke();
                });
                return;
            }

            ApplyHiddenInBuilding(false);
            SpawnScaleEffect.PlayIn(gameObject);
        }

        private void ApplyHiddenInBuilding(bool hidden)
        {
            foreach (Renderer render in _allRenderers)
                if (render != null)
                    render.enabled = !hidden;

            foreach (Collider2D collider in _colliders2D)
                if (collider != null)
                    collider.enabled = !hidden;

            foreach (Collider collider in _colliders3D)
                if (collider != null)
                    collider.enabled = !hidden;
        }

        /// <summary>
        /// 상주 중인 폰의 자세를 정합니다. 손에 드는 도구는 PawnIndex가 고릅니다.
        /// </summary>
        public void SetStationPose(bool moving, bool working, float toolIndex)
        {
            if (UnitAnimator == null)
                return;

            if (transportParam != null)
                UnitAnimator.SetParameter(transportParam, 0f);

            if (pawnIndexParam != null)
                UnitAnimator.SetParameter(pawnIndexParam, toolIndex);

            if (interactParam != null)
                UnitAnimator.SetParameter(interactParam, working);

            if (stationMoveParam != null)
                UnitAnimator.SetParameter(stationMoveParam, moving);
        }

        /// <summary>
        /// 수동으로 지정한 채집/운반 경로를 제거하고 현재 작업을 중지합니다.
        /// </summary>
        public void ClearRoute()
        {
            ManualHeadQuarter = null;
            TargetResource = null;
            SetVariableValue<GameObject>(BTVariables.TargetGameObject, null);
            Stop();
        }

        /// <summary>
        /// 건설을 취소하고 Dummy를 파괴하며 자원을 80% 환불합니다.
        /// </summary>
        public void CancelConstruction()
        {
            if (GetVariable(BTVariables.ConstructionDummySO,
                    out BlackboardVariable<ConstructionDummy> constructionDummy))
            {
                if (constructionDummy.Value != null)
                {
                    constructionDummy.Value.CancelConstruction();
                    constructionDummy.Value = null;
                }
            }

            SetCommandOverrides(null);
            Stop();
        }

        public override void DeSelect()
        {
            base.DeSelect();
            if (IsBuilding)
                SetCommandOverrides(cancelConstructionCommandSet);
        }
    }
}
