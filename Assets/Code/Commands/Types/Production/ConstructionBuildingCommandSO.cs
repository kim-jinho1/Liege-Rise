using Code.Buildings.Construction;
using Code.Buildings.Data;
using Code.Buildings.Interfaces;
using Code.Commands.Base;
using Code.Commands.Interfaces;
using Code.Core.Bus;
using Code.GameEvents.Economy;
using Code.GameEvents.System;
using Code.Navigation.Baking;
using Code.Players;
using Code.UI;
using Code.Units.Pawns;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Code.Commands.Types.Production
{
    [CreateAssetMenu(fileName = "Construction Building", menuName = "Units/Commands/Construction Building", order = 0)]
    public class ConstructionBuildingCommandSO : BaseCommandSO, IConstructionCommand
    {
        [field: SerializeField] public BuildingSO BuildingSO { get; private set; }
        [field: SerializeField] public GameObject GhostPrefab { get; private set; }
        [SerializeField] private LayerMask overlapLayerMask;

        // 자리가 없다는 말은 건설에만 있으므로 여기가 제자리다.
        // 자원 부족은 여러 커맨드가 같은 말을 써서 TextFormat이 쥔다
        [Header("Notices")]
        [SerializeField] private string blockedNotice = "여기에는 지을 수 없습니다";

        public Vector2Int BuildingSize => BuildingSO != null ? BuildingSO.Size : Vector2Int.one;

        private static readonly Collider2D[] HitBuffer = new Collider2D[1];
        private ContactFilter2D _filter;

        private void OnEnable()
        {
            _filter = new ContactFilter2D();
            _filter.SetLayerMask(overlapLayerMask);
            _filter.useTriggers = false;
        }

        public override bool CanHandle(CommandContext context)
        {
            // Pawn이 선택된 경우에만 건설 메뉴 표시 (건설 중 여부 무관)
            if (context.Commandable is not IBuildingConstructor)
                return false;

            // 일시정지 재개는 러너의 유휴 폰 자동 배정이 처리하므로 우클릭 재개 경로 없음
            if (context.MouseButton == MouseButton.Right)
                return false;

            return UserSupplies.Instance != null
                   && UserSupplies.Instance.HasEnoughSupplies(context.Owner, BuildingSO.Cost)
                   && AllRestrictionPass(context.Hit.point, BuildingSize);
        }

        public override void Handle(CommandContext context)
        {
            // 신규 건설: 자원 확인 → Dummy 스폰 → 자원 즉시 차감 → 폰 배정 건설 시작
            if (UserSupplies.Instance == null
                || !UserSupplies.Instance.HasEnoughSupplies(context.Owner, BuildingSO.Cost)
                || !AllRestrictionPass(context.Hit.point, BuildingSize))
                return;

            GameObject instance = Instantiate(BuildingSO.ConstructionDummyPrefab, context.Hit.point, Quaternion.identity);
            if (!instance.TryGetComponent(out ConstructionDummy dummy))
                return;

            dummy.InitializeAsWaiting(context.Owner);
            Bus<ResourceChangedEvent>.Raise(context.Owner, new ResourceChangedEvent(context.Owner, BuildingSO.Cost));

            PawnConstructionRunner runner = dummy.StartPawnConstruction();

            // 커맨드 발동 시 선택된(또는 가장 가까운) 폰을 첫 일꾼으로 우선 배정
            if (context.Commandable is Pawn pawn && !pawn.IsBuilding)
                runner.AssignPawn(pawn);
        }

        /// <summary>
        /// 배치가 거절된 이유를 가려 알립니다. 돈이 없는 것과 자리가 없는 것은 다른 문제입니다.
        /// </summary>
        public override void NotifyRefusal(CommandContext context)
        {
            bool canAfford = UserSupplies.Instance != null
                             && UserSupplies.Instance.HasEnoughSupplies(context.Owner, BuildingSO.Cost);

            string message = canAfford ? blockedNotice : TextFormat.NotEnoughSupplies;
            if (!string.IsNullOrEmpty(message))
                Bus<NoticeEvent>.Raise(context.Owner, new NoticeEvent(message, context.Hit.point));
        }

        /// <summary>
        /// 배치 영역에 다른 콜라이더가 없고 건설 불가 지형(다리 등)이 아니며
        /// 모든 추가 제한 조건을 통과하면 true를 반환합니다.
        /// </summary>
        public virtual bool AllRestrictionPass(Vector3 worldCenter, Vector2Int size)
        {
            Vector2 boxSize = GetBoxSize(size);
            if (Physics2D.OverlapBox(worldCenter, boxSize, 0f, _filter, HitBuffer) != 0)
                return false;

            PathBaker baker = PathBaker.Instance;
            return baker == null || baker.CanBuildFootprint(worldCenter, size);
        }

        private static Vector2 GetBoxSize(Vector2Int size)
        {
            PathBaker baker = PathBaker.Instance;
            if (baker == null || baker.BaseTilemap == null)
                return new Vector2(size.x, size.y);

            Vector2 cellSize = baker.BaseTilemap.cellSize;
            return new Vector2(size.x * cellSize.x, size.y * cellSize.y);
        }

        public override bool IsLocked(CommandContext context)
        {
            return UserSupplies.Instance != null
                   && !UserSupplies.Instance.HasEnoughSupplies(context.Owner, BuildingSO.Cost);
        }
    }
}
