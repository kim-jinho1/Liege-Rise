using System.Collections.Generic;
using Code.Buildings.Logic.CoreBuilding;
using Code.Commandable;
using Code.Core.Bus;
using Code.Military.Combat;
using Code.Military.Combat.FSM;
using Code.Military.Commanders;
using Code.Military.Data;
using Code.Military.Squads;
using Code.Stages;
using UnityEngine;

namespace Code.EnemyAI
{
    /// <summary>
    /// 적 갈래의 두뇌입니다. 지휘관에게 선을 그어 주는 것이 전부이고, 선을 분대 명령으로 바꾸는 일은
    /// 플레이어와 같은 CommanderLineExecutor가 합니다. 판단은 셋뿐입니다 —
    /// 옆구리를 맞았으면 예비 갈래를 부르거나 돌아서고, 우회 성향이면 근처 플레이어 분대의 옆구리로 가고,
    /// 아니면 본부를 향해 한 걸음씩 가로선을 긋습니다.
    /// </summary>
    public sealed class EnemyLineBrain
    {
        private const float TickInterval = 1f;
        private const float MarchStep = 6f;         // 길을 못 찾았을 때만 직선으로 이만큼 앞에 긋는다
        private const float Standoff = 3f;          // 본부 앞 선을 길 끝에서 이만큼 물려 긋는다
        private const float AssaultRange = 10f;     // 본부가 이 안이면 선 없이 곧장 친다
        private const float LineHalfLength = 4f;    // 세 분대가 어깨를 맞대는 폭의 절반
        private const float FlankSideDistance = 5f; // 피해자 옆구리에서 이만큼 떨어져 선다
        private const float FlankedWindow = 2f;     // 이 안에 맞은 측·후면 피해만 "지금" 맞은 것이다
        private const float ReactionCooldown = 4f;
        private const float InterceptReach = 6f;
        private const float WaypointReach = 7f;   // 길목을 이만큼 안으로 지나면 다음 길목으로 넘어간다

        public Commander Commander { get; }
        public bool IsAlive => Commander != null && !Commander.IsDead;

        private readonly List<Squad> _squads = new(8);
        private readonly BattleLineProbe _probe = new();
        private readonly SquadDoctrine _doctrine;

        private float _timer;
        private int _waypoint;
        private float _lastReaction = -100f;
        private float _interceptUntil = -1f;
        private Squad _flankVictim;

        public EnemyLineBrain(Commander commander, List<AbstractCommandable> units)
        {
            Commander = commander;

            int flankVotes = 0;
            foreach (AbstractCommandable unit in units)
            {
                if (unit is not Squad squad || !commander.Roster.Assign(squad))
                    continue;

                _squads.Add(squad);
                if (squad.AbstractCommandableSO is SquadSO { Doctrine: SquadDoctrine.Flank })
                    flankVotes++;
            }

            // 갈래의 성향은 다수결이다. 표가 섞여 뽑히므로 분대 하나로 정하면 매번 다르다
            _doctrine = flankVotes * 2 > _squads.Count ? SquadDoctrine.Flank : SquadDoctrine.Frontal;
        }

        /// <summary>
        /// 적을 보고 있거나 싸우는 분대가 하나라도 있는지입니다. 예비 갈래를 고를 때 봅니다.
        /// </summary>
        public bool IsEngaged
        {
            get
            {
                foreach (Squad squad in _squads)
                {
                    if (squad != null && !squad.IsDead
                                      && (squad.SeesEnemy || squad.State == SquadStateType.Engaging))
                        return true;
                }

                return false;
            }
        }

        public void Tick(float deltaTime, Vector3 center, HeadquartersBuilding target)
        {
            _timer -= deltaTime;
            if (_timer > 0f)
                return;

            _timer = TickInterval;

            for (int i = _squads.Count - 1; i >= 0; i--)
                if (_squads[i] == null || _squads[i].IsDead)
                    _squads.RemoveAt(i);

            if (_squads.Count == 0)
                return;

            if (TryReactToFlank(center))
                return;

            if (Time.time < _interceptUntil)
                return; // 요격 중에는 스스로 선을 갈지 않는다

            if (_doctrine == SquadDoctrine.Flank && TryFlank(center))
                return;

            March(center, target);
        }

        /// <summary>
        /// 다른 갈래의 부름을 받아 그 지점으로 선을 긋습니다. 한동안 자기 판단을 멈춥니다.
        /// </summary>
        public void Intercept(Vector3 from, Vector2 point)
        {
            _interceptUntil = Time.time + ReactionCooldown * 2f;
            SetLineAt(from, point - (Vector2)from);
        }

        /// <summary>
        /// 최근에 측·후면을 맞았으면 반응합니다. 놀고 있는 갈래가 있으면 그쪽이 요격하고, 없으면 돌아섭니다 —
        /// 돌아서는 순간 원래 붙들던 상대에게 옆구리가 열립니다. 그게 플레이어가 노리는 것입니다.
        /// </summary>
        private bool TryReactToFlank(Vector3 center)
        {
            // 이미 붙어 싸우는 중이면 선을 다시 긋지 않는다.
            // 선은 "무리의 평균 위치"에 그어지므로, 앞서 나간 분대가 그 선에 맞추려고 뒤로 걸어온다 —
            // 공세가 오다 말고 물러나는 것처럼 보인다. 맞은 분대는 어차피 각자 반격한다
            if (IsEngaged)
                return false;

            if (Time.time < _lastReaction + ReactionCooldown)
                return false;

            float since = Time.time - FlankedWindow;
            Vector2 direction = default;
            bool hit = false;

            for (int i = 0; i < _squads.Count && !hit; i++)
            {
                SquadArmor armor = _squads[i].Armor;
                if (armor == null || armor.LastHitZone == ArmorZone.Front || armor.LastHitTime < since)
                    continue;

                direction = armor.LastHitDirection;
                hit = true;
            }

            if (!hit || direction.sqrMagnitude < 0.0001f)
                return false;

            _lastReaction = Time.time;
            Vector2 attacker = (Vector2)center + direction.normalized * InterceptReach;

            if (!CallReserve(center, attacker))
                SetLineAt(center, direction);

            return true;
        }

        private bool CallReserve(Vector3 center, Vector2 attacker)
        {
            float range = EnemyAIManager.Instance != null ? EnemyAIManager.Instance.ReserveRange : 30f;
            float rangeSqr = range * range;

            IReadOnlyList<AssaultForce> forces = AssaultRegistry.Forces;
            foreach (AssaultForce force in forces)
            {
                EnemyLineBrain other = force != null ? force.Brain : null;
                if (other == null || other == this || !other.IsAlive || other.IsEngaged)
                    continue;

                if (((Vector2)force.Center - (Vector2)center).sqrMagnitude > rangeSqr)
                    continue;

                other.Intercept(force.Center, attacker);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 근처 플레이어 분대의 옆구리에 선을 긋습니다. 어느 쪽 옆구리인지는 우리에게 가까운 쪽입니다.
        /// 같은 피해자에게 이미 가는 중이면 선을 다시 긋지 않습니다 — 매번 새로 그으면 대형이 계속 다시 섭니다.
        /// </summary>
        private bool TryFlank(Vector3 center)
        {
            float range = EnemyAIManager.Instance != null ? EnemyAIManager.Instance.FlankRange : 18f;
            Squad victim = _probe.FindNearestEnemySquad(center, range, Owner.Enemy);

            if (victim == null)
            {
                _flankVictim = null;
                return false;
            }

            if (victim == _flankVictim && Commander.HasLine)
                return true;

            _flankVictim = victim;

            Vector2 facing = victim.FacingDirection;
            Vector2 perpendicular = new(-facing.y, facing.x);
            Vector2 victimPosition = victim.transform.position;
            Vector2 sideA = victimPosition + perpendicular * FlankSideDistance;
            Vector2 sideB = victimPosition - perpendicular * FlankSideDistance;
            Vector2 side = (sideA - (Vector2)center).sqrMagnitude <= (sideB - (Vector2)center).sqrMagnitude
                ? sideA
                : sideB;

            side = LineTerrain.Snap(side); // 옆구리가 물가·절벽이면 가장 가까운 통행 칸에서 친다
            SetLineAt(side, victimPosition - side);
            return true;
        }

        /// <summary>
        /// 본부 바로 앞에 가로선을 한 번에 긋습니다. 걷거나 싸우는 중이면 그대로 둡니다 —
        /// 한 걸음씩 긋고 전원이 서기를 기다리면 갈래가 6칸마다 멈춰 서서 오다 말다 합니다.
        /// 본부가 코앞인데 센서가 못 봤으면 선 대신 직접 공격을 시킵니다.
        /// </summary>
        private void March(Vector3 center, HeadquartersBuilding target)
        {
            if (target == null)
                return;

            if (!AllIdle() || (Commander.HasLine && IsEngaged))
                return;

            Vector2 toTarget = (Vector2)target.transform.position - (Vector2)center;
            if (toTarget.sqrMagnitude <= AssaultRange * AssaultRange)
            {
                Commander.ClearLine();
                foreach (Squad squad in _squads)
                    squad.Attack(target);
                return;
            }

            // 스테이지가 길목을 찍어 뒀으면 그리로 먼저 간다. 안 찍었으면 본부로 곧장 간다
            Vector2 goal = NextWaypoint(center, target.transform.position);
            Vector2 toGoal = goal - (Vector2)center;

            // 직선으로 밀면 절벽·물에 막힌다. 실제 길을 따라 목표 앞에 긋고, 길이 없을 때만 직선이다
            if (LineTerrain.TryStepBeforeEnd(center, goal, Standoff, out Vector2 point, out Vector2 direction))
                SetLineAt(point, direction);
            else if (toGoal.sqrMagnitude > 0.01f)
                SetLineAt((Vector2)center + toGoal.normalized * MarchStep, toGoal);
        }

        /// <summary>
        /// 아직 지나지 않은 길목입니다. 다 지났으면 본부입니다.
        /// 길목을 안 찍으면 공세가 늘 같은 최단 경로로 옵니다. 길이 하나뿐이면 매복할 자리도 하나뿐이라
        /// 플레이어가 고를 것이 없습니다 — 길목은 그 선택을 만들라고 있습니다.
        /// </summary>
        private Vector2 NextWaypoint(Vector3 center, Vector2 fallback)
        {
            StageSO stage = StageDatabaseSO.Current;
            IReadOnlyList<Vector2> route = stage != null ? stage.WaveRoute : null;
            if (route == null)
                return fallback;

            while (_waypoint < route.Count)
            {
                if ((route[_waypoint] - (Vector2)center).sqrMagnitude > WaypointReach * WaypointReach)
                    return route[_waypoint];

                _waypoint++;
            }

            return fallback;
        }

        private bool AllIdle()
        {
            foreach (Squad squad in _squads)
                if (squad.State != SquadStateType.Idle)
                    return false;

            return true;
        }

        /// <summary>
        /// 중심을 지나고 미는 방향에 수직인 선입니다. 점 순서를 맞춰 선의 수직이 곧 미는 방향이 되게 합니다.
        /// </summary>
        private void SetLineAt(Vector2 center, Vector2 pushDirection)
        {
            if (pushDirection.sqrMagnitude < 0.0001f)
                return;

            Vector2 normal = pushDirection.normalized;
            Vector2 along = new(normal.y, -normal.x);
            Commander.SetLine(new BattleLine(center - along * LineHalfLength, center + along * LineHalfLength, false));
        }
    }
}
