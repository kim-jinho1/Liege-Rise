using UnityEngine;

namespace Code.Military.Combat.FSM
{
    /// <summary>
    /// 탑승형 타워에 올라가 있는 상태입니다.
    /// 로스터에 아무 명령도 내리지 않는 것이 이 상태의 존재 이유입니다 — 멤버는 각자의
    /// MountTower BT 분기가 직접 몰고 있어서, 분대가 끼어들면 그 분기가 끊깁니다.
    /// ICanReceiveOrder를 선언하지 않으므로 이동·공격 명령은 진입 자체를 하지 못하고,
    /// 해제는 Squad.DismountTower가 부르는 SetMounted(false)뿐입니다.
    /// </summary>
    public class SquadMountedState : SquadState
    {
        public SquadMountedState(SquadStateMachine machine) : base(machine) { }

        public override SquadStateType Type => SquadStateType.Mounted;

        /// <summary>
        /// 타워 위에서는 대형 회전이 의미가 없으므로 정면을 고정합니다.
        /// </summary>
        public override void Enter()
            => Facing.SetRotationLocked(true);

        /// <summary>
        /// 타워 위에서는 재배치하지 않습니다. 멤버는 각자의 MountTower 분기가 몰고 있습니다.
        /// </summary>
        public override void OnFormationChanged() { }

        /// <summary>
        /// 타워 위에서는 반격하러 내려가지 않습니다. 기본 구현이 교전으로 넘기면 탑승이 풀립니다.
        /// </summary>
        public override void OnDamaged(Vector3 attackerDirection) { }

        /// <summary>
        /// 회전 잠금을 풀어 줍니다. 하차 자체는 Squad가 로스터를 멈춰 처리합니다.
        /// </summary>
        public override void Exit()
            => Facing.SetRotationLocked(false);
    }
}
