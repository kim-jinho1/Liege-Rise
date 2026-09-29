using System;
using Code.Animations;
using Code.Buildings.Logic.DefenseBuilding;
using Code.Commandable.Interfaces;
using Code.Navigation;
using Code.Units.Combat;
using Code.Units.Util;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Code.Units.BT.Actions
{
    public enum MountPhase
    {
        Moving,
        Mounted
    }

    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Mount Tower", story: "[Unit] mount [TargetGameObject] and attack from tower", category: "Action/Units", id: "d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9")]
    public partial class MountTowerAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractCombatUnit> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;
        [SerializeReference] public BlackboardVariable<AttackConfigSO> AttackConfig;
        [SerializeReference] public BlackboardVariable<ParameterSO> AttackParam;
        [SerializeReference] public BlackboardVariable<Transform> FirePositionTrm;
        [SerializeReference] public BlackboardVariable<float> MeleeOffset = new(0.8f);
        [SerializeReference] public BlackboardVariable<ParameterSO> MoveParam;

        private MountPhase _phase;
        private PathAgent _agent;
        private UnitAnimator _unitAnimator;
        private MannedTowerBuilding _tower;
        private int _slotIndex;
        private Transform _slotTransform;
        private IDamageable _currentTarget;
        private CombatSwingHelper _swing;

        protected override Status OnStart()
        {
            if (Unit.Value == null || TargetGameObject.Value == null ||
                !TargetGameObject.Value.TryGetComponent(out MannedTowerBuilding mannedTower) ||
                !mannedTower.TryReserveSlot(Unit.Value, out _slotIndex, out _slotTransform))
                return Status.Failure;

            _tower = mannedTower;
            _agent = Unit.Value.Agent;
            _unitAnimator = Unit.Value.UnitAnimator;
            _phase = MountPhase.Moving;

            EnsureSwingHelper();
            SetMoveParam(true);
            _agent.SetDestination(_slotTransform.position);
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (_tower == null || !_tower.gameObject.activeInHierarchy)
                return Status.Success;

            if (_phase == MountPhase.Moving)
            {
                if (MovementHelper.HasArrived(_agent))
                    TransitionToMounted();

                return Status.Running;
            }

            // 슬롯에 못박아 둔다 — 밀어내기나 다른 힘이 조금씩 밀어내면 타워에서 흘러내린다.
            if (_slotTransform != null)
                Unit.Value.transform.position = _slotTransform.position;

            if (_swing.IsSwinging || !_swing.IsReady(Unit.Value.AttackDelayMultiplier))
                return Status.Running;

            _currentTarget = GetClosestEnemy();
            if (_currentTarget == null)
                return Status.Running;

            // 제자리에서 쏘므로 이동이 좌우를 뒤집어 주지 않는다. 쏘기 전에 직접 대상을 본다
            _agent.FaceTowards(_currentTarget.Transform.position);
            _swing.TryStart(_currentTarget.Transform, _currentTarget);
            return Status.Running;
        }

        protected override void OnEnd()
        {
            SetMoveParam(false);
            _swing?.End();

            if (_agent != null)
                _agent.IsStopped = false;

            // 타워가 무너지면 사수도 같이 죽는다. ?.은 파괴된 유닛을 통과시킨다
            if (Unit.Value != null)
                Unit.Value.UnmountTower();

            // 타워에 오르기 전에 명령이 끊기면 Unmount가 일찍 빠져나가 예약이 남는다.
            _tower?.ReleaseSlot(_slotIndex, Unit.Value);
        }

        private void TransitionToMounted()
        {
            SetMoveParam(false);
            Unit.Value.transform.position = _slotTransform.position;
            Unit.Value.MountTower(_tower, _slotIndex);
            _agent.IsStopped = true;
            _phase = MountPhase.Mounted;
        }

        private void SetMoveParam(bool value)
            => MovementHelper.SetMoveParam(Unit.Value, MoveParam?.Value, value);

        /// <summary>
        /// 스윙 헬퍼를 초기화합니다.
        /// 이미 초기화되어 있으면 무시합니다.
        /// </summary>
        private void EnsureSwingHelper()
        {
            if (_swing != null)
                return;

            _swing = new CombatSwingHelper(Unit.Value, _unitAnimator, AttackConfig.Value, AttackParam.Value,
                null, 1, FirePositionTrm.Value, MeleeOffset.Value);
        }

        private IDamageable GetClosestEnemy()
        {
            IDamageable closest = null;
            float minSqrDist = float.MaxValue;
            Vector3 pos = Unit.Value.transform.position;

            // 안개로 거른 목록을 보면 시야를 못 가진 쪽은 코앞의 적도 못 친다. 전투 판정은 AllInRange다
            foreach (IDamageable damageable in Unit.Value.Sensor.AllInRange)
            {
                if (damageable?.Transform == null || !damageable.IsTargetable)
                    continue;

                float sqrDist = (damageable.Transform.position - pos).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closest = damageable;
                }
            }

            return closest;
        }
    }
}
