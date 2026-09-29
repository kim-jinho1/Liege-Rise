using System;
using Code.Animations;
using Code.Buildings.Base;
using Code.Buildings.Logic.Shared;
using Code.Navigation;
using Code.Rendering;
using Code.Units.Base;
using Code.Units.Pawns;
using Code.Units.Util;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Code.Units.BT.Actions
{
    public enum StationPhase
    {
        Moving,
        Stationed
    }

    /// <summary>
    /// [Unit]이 [TargetGameObject]로 이동하여 건물 안에서 작업을 수행하도록 합니다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Station At Building", story: "[Unit] enters [TargetGameObject] and works inside", category: "Action/Units", id: "9de5eb4aaf0549d88f19b319162ca473")]
    public partial class StationAtBuildingAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractUnit> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;
        [SerializeReference] public BlackboardVariable<ParameterSO> MoveParam;

        // 양이 그룹 안에서 50~150을 쓰고 울타리는 그보다 위 정렬 레이어다. 그 사이에 세운다
        private const int StationSortingOrder = 300;

        private StationPhase _phase;
        private PathAgent _agent;
        private Pawn _pawn;
        private PawnStationHost _host;
        private StationedPawnDisplay _display;
        private AbstractBuilding _building;
        private MoveStallWatchdog _watchdog;
        private SortingOverride _sorting;

        protected override Status OnStart()
        {
            if (Unit.Value is not Pawn pawn || TargetGameObject.Value == null)
                return Status.Failure;

            _host = TargetGameObject.Value.GetComponentInParent<PawnStationHost>();
            _display = TargetGameObject.Value.GetComponentInParent<StationedPawnDisplay>();
            _building = TargetGameObject.Value.GetComponentInParent<AbstractBuilding>();
            if (_host == null || _building == null || !_host.HasSpace)
                return Status.Failure;

            _pawn = pawn;
            _agent = pawn.Agent;
            _phase = StationPhase.Moving;

            SetMoveParam(true);
            _agent.SetDestination(_building.ApproachPoint.position);
            _watchdog.Reset();
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (_host == null || !_host.gameObject.activeInHierarchy)
                return Status.Success; // 건물 파괴 → OnEnd에서 복원

            if (_phase != StationPhase.Moving)
                return Status.Running;

            if (!MovementHelper.HasArrived(_agent))
                return _watchdog.IsStuck(_agent) ? Status.Failure : Status.Running; // 경로 계산이 실패하면 남은 거리가 영원히 줄지 않는다. 포기해야 다른 일을 잡는다.

            if (!_host.TryStation(_pawn))
                return Status.Failure; // 이동 중 만석이 된 경우

            TransitionToStationed();

            return Status.Running;
        }

        protected override void OnEnd()
        {
            SetMoveParam(false);
            LeaveBuildingSorting();

            if (_phase == StationPhase.Stationed && _pawn != null)
            {
                // 작업 모션을 먼저 끈다. 켜 둔 채 내보내면 걸어가면서 칼질을 한다
                _pawn.SetStationPose(false, false, 0f);
                _pawn.SetHiddenInBuilding(false);
                if (_building != null)
                    _pawn.transform.position = _building.ApproachPoint.position;

                _pawn.StationedBuilding = null;
                if (_host != null)
                    _host.Unstation(_pawn);
            }

            if (_agent != null)
                _agent.IsStopped = false;

            _phase = StationPhase.Moving;
        }

        private void TransitionToStationed()
        {
            SetMoveParam(false);
            _agent.IsStopped = true;

            // 건물에 표시 컴포넌트가 있으면 숨기지 않고 그 자리에 세운다. 없으면 지금까지처럼 안으로 들어간다
            bool visible = _display != null;
            Vector3 position = _building.transform.position;
            float toolIndex = 0f;

            if (visible)
                _display.GetPose(_pawn, _host, out position, out toolIndex);

            _pawn.StationedBuilding = _building;

            if (visible)
            {
                _pawn.transform.position = position;
                _pawn.SetHiddenInBuilding(false);
            }
            else
            {
                // 문 앞에 선 자리에서 줄어든 뒤에 안으로 넣는다.
                // 먼저 옮기면 건물 한가운데서 쪼그라들어 벽 속으로 빨려드는 것처럼 보인다
                Vector3 inside = position;
                _pawn.SetHiddenInBuilding(true, () => _pawn.transform.position = inside);
            }

            _pawn.SetStationPose(false, false, toolIndex);

            // 건물 안을 돌아다니므로 Y 정렬에 맡기면 울타리 앞뒤를 넘나든다.
            // 건물의 정렬 그룹 안으로 넣고 순서를 고정해야 양보다 앞, 울타리보다 뒤가 된다
            if (visible)
                JoinBuildingSorting();

            _phase = StationPhase.Stationed;
        }

        /// <summary>
        /// 일꾼을 건물의 정렬 그룹 자식으로 옮기고 그룹 안 순서를 고정합니다.
        /// </summary>
        private void JoinBuildingSorting()
        {
            Transform parent = _display != null ? _display.Parent : null;
            if (parent != null)
                _pawn.transform.SetParent(parent, true);

            _sorting.Apply(_pawn.gameObject, StationSortingOrder);
        }

        private void LeaveBuildingSorting()
        {
            _sorting.Restore();

            // 징집·사망으로 파괴되는 중이면 되돌릴 곳이 없다. IsDead는 Destroy 직전에 켜지므로
            // BT가 OnDestroy에서 OnEnd를 부르는 경로를 이걸로 거른다 — 안 거르면 SetParent가 경고를 낸다.
            // 판을 나갈 때는 죽은 게 아니라 씬째 파괴되므로 scene.isLoaded로 한 번 더 거른다
            if (_pawn != null && !_pawn.IsDead && _pawn.gameObject.scene.isLoaded
                && !Core.ApplicationLifecycle.IsQuitting && _pawn.transform.parent != null)
                _pawn.transform.SetParent(null, true);
        }

        private void SetMoveParam(bool value)
            => MovementHelper.SetMoveParam(Unit.Value, MoveParam?.Value, value);
    }
}
