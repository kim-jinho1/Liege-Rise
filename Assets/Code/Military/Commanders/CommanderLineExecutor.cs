using System.Collections.Generic;
using Code.Military.Combat.FSM;
using Code.Military.Squads;
using UnityEngine;

namespace Code.Military.Commanders
{
    /// <summary>
    /// 공세선을 분대 명령으로 바꾸는 두뇌입니다.
    /// 분대를 선 위에 나눠 세우고, 적이 보이는 동안 계속 밀며, 적이 없으면 선을 끝냅니다.
    /// 미는 일 자체는 LinePushTracker가 맡습니다.
    /// 교전은 분대 FSM이 알아서 하므로 싸우는 분대는 건드리지 않습니다 — 명령을 덮으면 스윙이 끊깁니다.
    /// </summary>
    public sealed class CommanderLineExecutor
    {
        private const float TickInterval = 0.4f;

        private const int Slots = CommanderRoster.DefaultCapacity + 2;

        public bool HasLine { get; private set; }
        public BattleLine Line => _line;

        /// <summary>선에서 앞으로 나간 거리입니다. 분대 무리가 실제로 간 만큼이고 뒤로는 줄지 않습니다.</summary>
        public float Advance { get; private set; }

        /// <summary>선을 받은 뒤 분대 위치를 한 번이라도 읽었는지입니다. 읽기 전엔 따라갈 자리가 없습니다.</summary>
        public bool HasProgress { get; private set; }

        private BattleLine _line;
        private float _timer;
        private bool _formed;
        private float _progress; // 분대 무리의 지금 위치. 선 앞이면 양수, 아직 못 닿았으면 음수
        private Vector2 _centroid; // 분대 무리의 실제 좌표. 지휘관은 이것을 따라간다

        private readonly LineSeatLayout _layout = new();
        private readonly BattleLineProbe _probe = new();
        private readonly LinePushTracker _push;

        // 우회 중인 분대. 적 뒤에 그은 선은 적 앞을 지나며 붙습니다 — 옆으로 돌아 뒤로 갑니다.
        // 우회점에 선 뒤 두 번째 구간(자리)을 여기서 내린다
        private readonly Squad[] _detourSquad = new Squad[Slots];
        private readonly Vector2[] _detourSeat = new Vector2[Slots];

        public CommanderLineExecutor()
        {
            _push = new LinePushTracker(_layout);
        }

        public void SetLine(in BattleLine line)
        {
            _line = line;
            HasLine = true;
            Advance = 0f;
            HasProgress = false;
            _formed = false;
            _timer = 0f;

            _push.Reset();
            for (int i = 0; i < Slots; i++)
                _detourSquad[i] = null;
        }

        public void Clear()
        {
            HasLine = false;
            _formed = false;
        }

        /// <summary>
        /// 지휘관이 설 자리입니다. 분대 무리의 지금 위치에서 뒤로 물러난 지점입니다 —
        /// 선 기준으로 잡으면 멀리 그은 선에 지휘관이 분대보다 먼저 달려가 혼자 죽습니다.
        /// 무게중심을 그대로 씁니다. 예전에는 _progress(수직 성분)만 남겨 선 중앙 축 위의 점을 다시
        /// 만들었는데, 그러면 분대가 옆으로 우회해도 지휘관만 중앙을 직선으로 걸어 적 앞에 혼자 나섭니다.
        /// </summary>
        public Vector2 FollowPoint(float rearOffset)
            => _centroid - _line.Normal * rearOffset;

        /// <summary>
        /// 한 틱 진행합니다. 선이 할 일을 다 했으면(전원이 서 있고 보이는 적이 없다) 선을 지우고 true입니다.
        /// </summary>
        public bool Tick(float deltaTime, IReadOnlyList<Squad> squads)
        {
            if (!HasLine || squads.Count == 0)
                return false;

            _timer -= deltaTime;
            if (_timer > 0f)
                return false;

            _timer = TickInterval;

            _progress = CentroidProgress(squads);
            HasProgress = true;

            if (!_formed)
            {
                // 방금 생성된 분대는 아직 분대원이 없다 — 지금 명령하면 아무도 안 걷고 곧장 Idle이 되어 선이 끝난다
                for (int i = 0; i < squads.Count; i++)
                    if (squads[i].AliveCount <= 0)
                        return false;

                _formed = true;
                FormUp(squads);
                return false;
            }

            bool detouring = ContinueDetours();
            bool anyEnemy = false;
            bool allIdle = !detouring;

            for (int i = 0; i < squads.Count; i++)
            {
                Squad squad = squads[i];

                if (squad.SeesEnemy)
                    anyEnemy = true;

                if (squad.State != SquadStateType.Idle)
                    allIdle = false;
            }

            Advance = Mathf.Max(Advance, _progress);

            if (!anyEnemy)
            {
                // 아직 걷는 중이면 기다리고, 다 섰으면 선은 끝난 것이다
                if (!allIdle)
                    return false;

                Clear();
                return true;
            }

            _push.Push(_line, squads);
            return false;
        }

        /// <summary>
        /// 분대 무리의 평균 위치를 선의 수직 방향으로 사영한 값입니다.
        /// </summary>
        private float CentroidProgress(IReadOnlyList<Squad> squads)
        {
            Vector2 centroid = Vector2.zero;
            for (int i = 0; i < squads.Count; i++)
                centroid += (Vector2)squads[i].transform.position;

            centroid /= squads.Count;
            _centroid = centroid;
            return Vector2.Dot(centroid - _line.Center, _line.Normal);
        }

        /// <summary>
        /// 분대를 선을 따라 나눠 세웁니다. 자리로 가는 길에 적이 있고 선이 그 적의 뒤·옆을 치면 옆으로 돌아갑니다.
        /// </summary>
        private void FormUp(IReadOnlyList<Squad> squads)
        {
            int count = _layout.SortAlong(_line, squads);
            Vector2 normal = _line.Normal;

            for (int i = 0; i < count; i++)
            {
                Squad squad = _layout[i];
                Vector2 seat = _layout.Seat(_line, i, count, 0f);

                if (DetourPlanner.TryPlan(squad.transform.position, seat, normal, squad.Owner, _probe,
                        out Vector2 detour))
                {
                    int slot = LinePushTracker.SlotOf(_detourSquad, squad);
                    _detourSquad[slot] = squad;
                    _detourSeat[slot] = seat;
                    squad.Advance(detour, normal);
                    continue;
                }

                squad.Advance(seat, normal);
            }
        }

        /// <summary>
        /// 우회점에 선 분대에 두 번째 구간(자리)을 내립니다. 하나라도 아직 우회 중이면 true.
        /// </summary>
        private bool ContinueDetours()
        {
            bool any = false;
            for (int i = 0; i < Slots; i++)
            {
                Squad squad = _detourSquad[i];
                if (squad == null)
                    continue;

                if (squad.IsDead)
                {
                    _detourSquad[i] = null;
                    continue;
                }

                any = true;
                if (squad.State != SquadStateType.Idle)
                    continue;

                _detourSquad[i] = null;
                squad.Advance(_detourSeat[i], _line.Normal);
            }

            return any;
        }
    }
}
