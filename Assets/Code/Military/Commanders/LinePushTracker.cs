using System.Collections.Generic;
using Code.Commandable.Interfaces;
using Code.Military.Combat.FSM;
using Code.Military.Squads;
using UnityEngine;

namespace Code.Military.Commanders
{
    /// <summary>
    /// 공세선 위의 분대를 앞으로 미는 일과, 이미 민 분대를 기억하는 일을 맡습니다.
    /// </summary>
    public sealed class LinePushTracker
    {
        internal const int Slots = CommanderRoster.DefaultCapacity + 2;

        private const float PushDistance = 12f;      // 놀고 있는 분대를 앞으로 보내는 거리. 붙으면 그 전에 멈춘다
        private const float TargetStandoff = 1f;

        // 선이 미는 방향에서 이 각도 안에 있는 적만 쫓아간다.
        // 넓히면 옆의 적이 부대를 끌고 가고, 좁히면 살짝 비낀 성을 지나쳐 걷는다
        private const float TargetArc = 70f;

        // 밀기 한 번이 분대원 수만큼의 A*다. 같은 분대를 이 간격 안에 다시 밀지 않고,
        // 밀었는데 이만큼도 못 나아간 분대(자리가 막힘)는 선이 바뀔 때까지 놓아둔다
        private const float PushCooldown = 2f;
        private const float PushProgressMin = 0.5f;

        private readonly LineSeatLayout _layout;

        // 분대별 밀기 기록. 명부 순서로 나란히 둔다 — 명부는 뒤에서만 빠지므로 인덱스가 어긋나면 그냥 다시 민다
        private readonly Squad[] _pushed = new Squad[Slots];
        private readonly float[] _pushedAt = new float[Slots];
        private readonly float[] _pushedProgress = new float[Slots];
        private readonly bool[] _pushBlocked = new bool[Slots];

        public LinePushTracker(LineSeatLayout layout)
        {
            _layout = layout;
        }

        /// <summary>선이 새로 그어졌을 때 기록을 비웁니다.</summary>
        public void Reset()
        {
            for (int i = 0; i < Slots; i++)
            {
                _pushed[i] = null;
                _pushBlocked[i] = false;
            }
        }

        /// <summary>
        /// 놀고 있는 분대만 각자 지금 진행 위치에서 앞으로 길게 보냅니다. 싸우는 분대는 그대로 둡니다.
        /// 걷는 중에 붙으면 전진 상태가 교전으로 넘어가며 스스로 멈춥니다.
        /// </summary>
        public void Push(in BattleLine line, IReadOnlyList<Squad> squads)
        {
            int count = _layout.SortAlong(line, squads);
            Vector2 normal = line.Normal;

            for (int i = 0; i < count; i++)
            {
                Squad squad = _layout[i];
                if (squad.State != SquadStateType.Idle)
                    continue;

                float progress = Vector2.Dot((Vector2)squad.transform.position - line.Center, normal);
                int slot = SlotOf(_pushed, squad);

                if (_pushed[slot] == squad)
                {
                    // 밀었는데 제자리였던 분대(자리가 막혔거나 그 자리에서 싸웠다)는 두 배로 뜸하게 다시 민다
                    float cooldown = _pushBlocked[slot] ? PushCooldown * 2f : PushCooldown;
                    if (Time.time < _pushedAt[slot] + cooldown)
                        continue;

                    _pushBlocked[slot] = progress - _pushedProgress[slot] < PushProgressMin;
                }

                _pushed[slot] = squad;
                _pushedAt[slot] = Time.time;
                _pushedProgress[slot] = progress;

                // 보고 있는 적이 있으면 수직이 아니라 그쪽으로 간다. 수직만 고집하면 사거리 밖의 성 옆을
                // 그냥 지나쳐 걷고, 시야에서 빠지면 선이 끝나 되돌아오기를 되풀이한다.
                //
                // 다만 미는 쪽에 있는 적만 쫓는다. 센서는 방향을 안 보고 가장 가까운 적을 주므로,
                // 그대로 받으면 옆이나 뒤의 적이 부대를 끌고 간다 — 플레이어가 그은 화살표가 아니라
                // 우연히 가까운 적이 명령이 되는 것이다
                if (TrySensedTarget(squad, out Vector2 targetPosition))
                {
                    Vector2 toTarget = targetPosition - (Vector2)squad.transform.position;
                    if (toTarget.sqrMagnitude > 0.01f && Vector2.Angle(normal, toTarget) <= TargetArc)
                    {
                        Vector2 dir = toTarget.normalized;
                        squad.Advance(LineTerrain.Snap(targetPosition - dir * TargetStandoff), dir);
                        continue;
                    }
                }

                squad.Advance(_layout.Seat(line, i, count, progress + PushDistance), normal);
            }
        }

        private static bool TrySensedTarget(Squad squad, out Vector2 position)
        {
            IDamageable target = squad.Sensor.Target;
            if (target == null || target.Transform == null || target.CurrentHealth <= 0)
            {
                position = default;
                return false;
            }

            position = target.Transform.position;
            return true;
        }

        /// <summary>
        /// 분대의 기록 칸입니다. 없으면 빈 칸을 줍니다.
        /// </summary>
        internal static int SlotOf(Squad[] keys, Squad squad)
        {
            int empty = 0;
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] == squad)
                    return i;

                if (keys[i] == null)
                    empty = i;
            }

            return empty;
        }
    }
}
