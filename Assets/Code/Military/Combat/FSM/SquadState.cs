using Code.Commandable.Interfaces;
using Code.Military.Squads;
using Code.Military.Stats;
using Code.Units.Util;
using UnityEngine;

namespace Code.Military.Combat.FSM
{
    /// <summary>
    /// 분대 상태의 공통 기반입니다. MonoBehaviour가 아니며, 생성 시 협력 컴포넌트를 필드로 잡습니다.
    /// 상태는 소유자(Squad)에 도달할 수 없고 필요한 값은 전부 기계가 프로퍼티로 공급합니다.
    /// </summary>
    public abstract class SquadState
    {
        protected const float StandoffFactor = 0.8f; // 사거리의 80% 지점까지만 붙는다

        protected readonly SquadStateMachine Machine;
        protected readonly SquadRoster Roster;
        protected readonly SquadFacing Facing;
        protected readonly SquadSensor Sensor;

        /// <summary>
        /// 분대 스탯 시트입니다. 상태 생성 시점에는 아직 비어 있어 매번 기계에서 읽습니다.
        /// </summary>
        protected StatSheet Stats => Machine.Stats;

        protected SquadState(SquadStateMachine machine)
        {
            Machine = machine;
            Roster = machine.Roster;
            Facing = machine.Facing;
            Sensor = machine.Sensor;
        }

        /// <summary>
        /// 이 상태의 종류입니다. 기계가 사전 등록 키로 사용합니다.
        /// </summary>
        public abstract SquadStateType Type { get; }

        /// <summary>
        /// 상태에 진입할 때 한 번 호출됩니다.
        /// </summary>
        public virtual void Enter() { }

        /// <summary>
        /// 소유자의 Update에서 매 프레임 호출됩니다.
        /// </summary>
        public virtual void Tick(float deltaTime) { }

        /// <summary>
        /// 상태를 벗어날 때 한 번 호출됩니다.
        /// </summary>
        public virtual void Exit() { }

        /// <summary>
        /// 분대원이 사망해 대형에 구멍이 생겼을 때 호출됩니다. 기본 동작은 없습니다.
        /// </summary>
        public virtual void OnMemberLost() { }

        /// <summary>
        /// 대형이 바뀌었을 때 호출됩니다. 기본은 선 자리에서 새 배치로 다시 세우는 것입니다.
        /// 멤버를 부리지 않는 상태(타워 탑승)는 빈 구현으로 덮습니다.
        /// </summary>
        public virtual void OnFormationChanged()
            => Roster.Arrange(Machine.Origin, Facing.DesiredAngle);

        /// <summary>
        /// 분대원이 피해를 입었을 때 호출됩니다. 방향은 피격자에서 공격자를 향합니다(없으면 zero).
        /// 기본은 반격입니다 — 맞고 있는데 하던 일을 계속하면 등을 내주며 걸어가는 것으로만 보입니다.
        /// 이미 목표를 물고 있는 상태(교전·타워 탑승)는 빈 구현으로 덮어 목표를 지킵니다.
        /// </summary>
        public virtual void OnDamaged(Vector3 attackerDirection)
            => Retaliate(attackerDirection);

        /// <summary>
        /// 맞은 쪽으로 돌아섭니다. 감지된 적이 있으면 사거리를 따지지 않고 반격에 들어갑니다.
        /// 사거리 판정으로만 반격하면 우리보다 긴 적(창고블린 1.5 · 횃불 워밴드 2.5 대 민병 1.2)에게는
        /// 조건이 영원히 성립하지 않아 맞기만 합니다. 추격이 무한정 늘어지지 않도록 상한은
        /// SquadEngagingState가 앵커 기준 리시로 겁니다.
        /// </summary>
        protected void Retaliate(Vector3 attackerDirection)
        {
            IDamageable target = Sensor.Target;

            if (!IsEngageable(target))
            {
                // 감지 못한 곳에서 날아온 피해는 쫓지 않는다. 방향만 돌려 측면·후면 피해를 줄인다
                Facing.SetDesired(attackerDirection);
                return;
            }

            TryEngageOrTurn(target);
        }

        /// <summary>
        /// 아직 때릴 수 있는 목표인지 판정합니다. 체력 0 검사가 핵심입니다.
        /// 유닛은 사망 연출이 끝날 때까지 씬에 남아 있어 Transform만 보면 시체를 목표로 잡습니다.
        /// 모든 상태가 이 하나의 판정을 공유해야 상태가 매 프레임 오가며 분대원의 스윙을 끊지 않습니다.
        /// </summary>
        protected static bool IsEngageable(IDamageable target)
            => target != null && target.Transform != null && target.CurrentHealth > 0 && target.IsTargetable;

        /// <summary>
        /// 목적지를 향해 정면을 돌리고 대형을 배치합니다.
        /// 정면은 DesiredAngle 기준입니다 — 도착 시 대형이 진행 방향을 봐야 하기 때문입니다(09단계).
        /// </summary>
        protected void FormUp(Vector3 destination)
        {
            Facing.SetDesired(destination - Machine.Origin);
            Roster.Arrange(destination, Facing.DesiredAngle);
        }

        /// <summary>
        /// 목표 콜라이더 표면에서 분대 쪽으로 사거리만큼 당긴 지점입니다.
        /// 목표가 건물이면 중심 좌표는 콜라이더 안쪽이라 경로 계산이 실패합니다.
        /// 접근과 교전이 같은 지점을 봐야 붙는 순간에 대형이 튀지 않습니다.
        /// </summary>
        protected Vector3 StandoffPoint(IDamageable target)
            => StandoffPoint(target, StandoffFactor);

        /// <summary>
        /// 물러설 거리를 지정한 정지 지점입니다. 교전은 접근보다 훨씬 바짝 붙습니다 —
        /// 접근용 0.8을 그대로 쓰면 분대가 팔 길이만큼 떨어져서 허공을 치는 것처럼 보입니다.
        /// </summary>
        protected Vector3 StandoffPoint(IDamageable target, float factor)
        {
            Vector3 origin = Machine.Origin;
            Vector3 surface = DistanceUtil.GetClosestTargetPosition(
                Machine.RootObject, target.Transform.gameObject);

            Vector3 back = origin - surface;
            if (back.sqrMagnitude < 0.0001f)
                return origin;

            return surface + back.normalized * (Machine.AttackRange * factor);
        }

        /// <summary>
        /// 목표를 교전으로 넘깁니다. 아직 멀면 정면만 돌리고 기다립니다.
        /// Idle과 Approaching이 공유합니다 — 판정이 두 벌이 되면 갈라집니다.
        /// 사거리 안에 든 적에게는 콘을 따지지 않습니다 — 상대도 움직이는 목표라, 콘이 맞을 때까지
        /// 교전을 막으면 둘이 서로를 돌기만 합니다.
        /// </summary>
        protected void TryEngageOrTurn(IDamageable target)
        {
            Vector3 toTarget = target.Transform.position - Machine.Origin;
            bool inRange = Roster.AnyInRange(target.Transform.gameObject, Machine.EngageRange);

            if (!inRange && Facing.AngleTo(toTarget) > Machine.ConeAngle * 0.5f)
            {
                Facing.SetDesired(toTarget);
                Machine.Label = "회전 중";
                return;
            }

            Machine.EngageWith(target);
        }
    }
}
