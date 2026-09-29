using System.Collections.Generic;
using Code.Commandable.Interfaces;
using Code.Military.Squads;
using Code.Units.Combat;
using Code.Units.Util;
using UnityEngine;

namespace Code.Military.Combat.FSM
{
    /// <summary>
    /// 교전 상태입니다. 한 명이라도 사거리 안에 붙었으면 전원에게 같은 목표를 배정합니다.
    /// 자리는 목표를 둘러싼 원입니다 — 전원이 동시에 때리고 적의 측면·후면이 함께 열립니다.
    /// 이동 대형은 접근 상태가 그대로 쓰므로 대형 선택은 걸어갈 때의 모양으로 남습니다.
    /// </summary>
    public class SquadEngagingState : SquadState, ICanReceiveOrder
    {
        private const float LeaveFactor = 1.5f; // 진입 여유의 1.5배까지는 붙잡는다 (루트가 매 프레임 움직인다)
        private const float LeashRange = 6f;    // 자동 반격은 교전 시작 지점에서 이만큼까지만 쫓는다

        private const float MaintainInterval = 0.15f;
        private const float RepathMoveThresholdSqr = 0.5625f; // 0.75 유닛 — 목표가 이만큼 움직여야 다시 세운다
        private const float SeatToleranceSqr = 0.1225f;       // 0.35 유닛 — 자리에 섰다고 볼 거리
        private const float ReseatThresholdSqr = 2.25f;       // 1.5 유닛 — 싸우던 분대원을 자리로 되부를 거리

        // MoveTo는 블랙보드만 쓰고 BT가 다음 틱에 경로를 잡는다. 그 사이 HasArrived가 참이라 유예가 필요하다
        private const float SeatSettleTime = 0.35f;

        // 목표를 둘러싸는 원의 반지름 = 목표 덩치 + 사거리 × 이 값. 1에 가까울수록 헐겁게 두른다
        private const float SurroundFactor = 0.7f;

        // 자리를 벌리는 전체 부채꼴 각도. 360이면 반대편 자리를 받은 분대원이 적을 빙 돌아 걸어가고
        // 그동안 아무도 못 때린다 — 우리 쪽 반원 안에서만 벌린다
        private const float SeatArc = 150f;

        // 자리를 잡고 싸우는 중인 분대원. 같은 목표를 재배정하면 BT가 리셋돼 스윙이 끊긴다
        private readonly HashSet<AbstractCombatUnit> _seated = new();

        private Vector3 _anchor;    // 리시 기준점 (교전을 시작한 자리)
        private Vector3 _formPoint; // 마지막으로 대형을 세운 목표 위치
        private bool _hasFormed;
        private float _maintainTimer;

        // 교전을 시작한 뒤 흐른 시간. 재배치로는 되감지 않는다 —
        // 되감으면 움직이는 적을 쫓느라 재배치가 반복될 때 정착 판정이 영원히 서지 않는다
        private float _sinceEngage;
        private IDamageable _assigned;

        public SquadEngagingState(SquadStateMachine machine) : base(machine) { }

        public override SquadStateType Type => SquadStateType.Engaging;

        public override void Enter()
        {
            _anchor = Machine.Origin;
            _hasFormed = false;
            _maintainTimer = 0f;
            _sinceEngage = 0f;
            _assigned = null;
            _seated.Clear();

            // 새 목표를 전원에게 한 번 내린다.
            //
            // 자리에 앉은 분대원에게만 내리면, 이미 다른 적과 붙어 있던 분대원이 빠져나오지 못한다:
            // 공격 노드가 발을 묶어(IsStopped) 옛 경로가 남고, 그러면 HasArrived가 서지 않아 halted가
            // false다. 재배치는 첫 틱의 reform 때만 도므로 그 뒤로는 아무도 그 분대원을 다시 부르지
            // 않는다 — 화살표는 새 목표를 가리키는데 손은 옛 놈을 계속 때리는 상태가 된다.
            // 여기서 한 번 끊어 주면 Maintain이 이어서 자리로 보낸다
            if (IsEngageable(Machine.EngageTarget))
                Roster.AssignAttack(Machine.EngageTarget);
        }

        public override void Tick(float deltaTime)
        {
            IDamageable target = Machine.EngageTarget;

            if (!IsEngageable(target))
            {
                GoIdle();
                return;
            }

            Facing.SetDesired(target.Transform.position - Machine.Origin);

            _sinceEngage += deltaTime;
            _maintainTimer -= deltaTime;
            if (_maintainTimer <= 0f)
            {
                _maintainTimer = MaintainInterval;
                Maintain(target);
            }

            if (Roster.AnyInRange(target.Transform.gameObject, Machine.EngageRange * LeaveFactor))
                return;

            // 명령으로 지정한 목표면 대형째 다시 쫓는다.
            if (target == Machine.CommandedTarget)
            {
                Machine.ChangeState(SquadStateType.Approaching);
                return;
            }

            // 자동 반격은 멤버가 각자 붙는 중일 수 있으므로 바로 놓지 않는다.
            // 교전을 시작한 자리에서 리시 거리를 벗어났을 때만 제자리로 돌아간다.
            if ((Machine.Origin - _anchor).sqrMagnitude < LeashRange * LeashRange)
                return;

            GoIdle();
        }

        /// <summary>
        /// 목표 앞에 좁힌 대형을 세우고, 자리에 선 분대원부터 그 자리에서 싸우게 합니다.
        /// </summary>
        private void Maintain(IDamageable target)
        {
            if (target != _assigned)
            {
                _assigned = target;
                _hasFormed = false;
                _sinceEngage = 0f; // 새 목표에는 다시 자리를 잡을 기회를 준다
            }

            Vector3 targetCenter = target.Transform.position;

            // 판정 기준은 정지 지점이 아니라 목표 자신이다 — 정지 지점은 분대 무게중심을 따라
            // 매 프레임 흔들리고, 그걸 기준으로 삼으면 재배치가 반복돼 스윙이 끊긴다
            bool reform = !_hasFormed || (targetCenter - _formPoint).sqrMagnitude >= RepathMoveThresholdSqr;

            if (reform)
            {
                _formPoint = targetCenter;
                _hasFormed = true;
            }

            GameObject targetGo = target.Transform.gameObject;
            float angle = Facing.DesiredAngle;
            float ring = SurroundRadius(targetGo);
            int alive = Roster.AliveCount;

            int slot = 0;
            foreach (AbstractCombatUnit member in Roster.Members)
            {
                if (member.IsDead)
                    continue;

                Vector3 seat = SeatAround(slot, alive, targetCenter, ring, angle);
                slot++;
                Seat(member, seat, target, targetGo, reform);
            }
        }

        /// <summary>
        /// 목표 앞을 감싸는 자리입니다. 분대가 서 있는 쪽 부채꼴 안에서 좌우로 벌립니다.
        /// 앞에만 한 줄로 서면 다섯 중 둘만 닿고 나머지는 등 뒤에서 놉니다.
        /// 그렇다고 360°로 두르면 반대편 자리를 받은 분대원이 적을 빙 돌아 걸어가고, 그 사이 목표가
        /// 움직이면 영영 자리에 못 섭니다 — 그래서 우리 쪽 부채꼴로 제한합니다.
        /// </summary>
        private static Vector3 SeatAround(int slot, int aliveCount, Vector3 center, float radius, float facingAngle)
        {
            // 분대 정면의 반대편이 곧 "분대가 서 있는 쪽"이다. 그 부채꼴을 인원수로 나눠 채운다
            float step = aliveCount <= 1 ? 0f : SeatArc / (aliveCount - 1);
            float angle = facingAngle + 180f - SeatArc * 0.5f + slot * step;

            Vector3 offset = Quaternion.Euler(0f, 0f, angle) * Vector3.up * radius;
            return center + offset;
        }

        /// <summary>
        /// 둘러싸는 원의 반지름입니다. 목표의 덩치에 사거리를 조금 남겨 더합니다 —
        /// 건물은 중심이 콜라이더 안쪽이라 덩치를 빼면 벽 속에 자리를 잡습니다.
        /// </summary>
        private float SurroundRadius(GameObject targetGo)
        {
            float bulk = 0f;
            if (targetGo.TryGetComponent(out Collider2D collider))
                bulk = Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.y);

            return bulk + Machine.AttackRange * SurroundFactor;
        }

        /// <summary>
        /// 분대원 하나를 자리에 세우거나 그 자리에서 때리게 합니다.
        /// </summary>
        private void Seat(AbstractCombatUnit member, Vector3 seat, IDamageable target, GameObject targetGo, bool reform)
        {
            float toSeatSqr = (member.transform.position - seat).sqrMagnitude;
            bool inRange = SquadOrders.InRangeOf(member, targetGo, Machine.AttackRange);

            // 발이 멈췄다(도착했거나 길이 막혔거나 경로가 실패했다).
            // 스윙 중에도 참이지만 그때는 사거리 안이라 아래 판정이 걸러 준다
            bool halted = _sinceEngage >= SeatSettleTime && MovementHelper.HasArrived(member.Agent);

            if (_seated.Contains(member))
            {
                // 사거리를 잃은 것만으로 되돌리지 않는다 — 싸우는 중인 분대원은 자기 BT가 알아서 쫓아가고,
                // 여기서 MoveTo를 다시 내리면 그 추격과 스윙이 매번 취소된다.
                // 때리고 있는 중이면 자리에서 멀어졌든 말든 그대로 둔다 — 목표가 조금만 움직여도
                // 전원을 자리로 되부르면 상대가 움직이는 내내 스윙이 취소돼 아무도 못 때린다
                bool wandered = reform && !inRange && toSeatSqr > ReseatThresholdSqr;

                // 다만 목표를 놓치고 발까지 멈췄으면 아무도 다시 명령하지 않아 그대로 서서 논다
                bool abandoned = halted && !inRange;

                if (!wandered && !abandoned)
                    return;

                _seated.Remove(member);
                member.MoveTo(seat);
                return;
            }

            // 사거리에 들었으면 자리를 못 잡았어도 때린다. 자리 문턱(0.35)은 분리 반경이 계속 밀어내는
            // 난전에서 좀처럼 만족되지 않아, 이것을 기다리면 때릴 수 있는데도 계속 걷기만 한다
            if (inRange || toSeatSqr <= SeatToleranceSqr)
            {
                member.Attack(target);
                _seated.Add(member);
                return;
            }

            // 발이 멈췄는데 자리에도 못 갔고 사거리에도 못 들면 다시 보낸다.
            // 재배치 때만 보내면, 목표가 움직여 주기 전까지 서서 노는 분대원이 생긴다
            if (reform || halted)
                member.MoveTo(seat);
        }

        /// <summary>
        /// 교전을 접고 제자리로 돌아갑니다. Attack 분기를 걷어내지 않으면 멤버가 맵 끝까지 따라갑니다.
        /// </summary>
        private void GoIdle()
        {
            Roster.HaltAll();
            Machine.ChangeState(SquadStateType.Idle);
        }

        /// <summary>
        /// 이미 붙어 싸우는 중이라 피격으로 목표를 갈아타지 않습니다.
        /// 갈아타면 둘에게 번갈아 맞을 때마다 목표가 바뀌어 한 대도 못 때립니다.
        /// </summary>
        public override void OnDamaged(Vector3 attackerDirection) { }

        /// <summary>
        /// 빈자리를 메우도록 다음 틱에 대형을 다시 세웁니다.
        /// </summary>
        public override void OnMemberLost()
            => _hasFormed = false;

        /// <summary>
        /// 싸우는 중에 대형을 바꾸면 다음 틱에 새 배치로 다시 섭니다.
        /// </summary>
        public override void OnFormationChanged()
        {
            _hasFormed = false;
            _seated.Clear();
        }

        /// <summary>
        /// 여기서는 로스터를 건드리지 않습니다.
        /// 예전에는 무조건 HaltAll이라, 교전 ↔ 접근을 한 번 오갈 때마다 멤버의 스윙이 취소됐습니다.
        /// 이탈 판정 근처에서 상태가 흔들리면 공격 딜레이를 채우지 못해 한 대도 못 때립니다.
        /// 정지가 필요한 경로(Idle 복귀·정지 명령)는 각자 직접 멈춥니다.
        /// </summary>
        public override void Exit()
        {
            _assigned = null;
            _seated.Clear();
        }
    }
}
