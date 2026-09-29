using System.Collections.Generic;
using Code.Military.Squads;
using UnityEngine;

namespace Code.Military.Commanders
{
    /// <summary>
    /// 지휘관 밑 분대들의 실제 자리로 전선을 만듭니다 — 선을 따라 정렬한 분대 중심을 잇고 양 끝을 조금 늘립니다.
    /// 그은 선은 명령이고, 화면에 보이는 전선은 분대가 지금 서 있는 곳입니다. 그래서 걸을 때 따라 휩니다.
    /// </summary>
    public sealed class BattleFront
    {
        public const int MaxPoints = CommanderRoster.DefaultCapacity + 2;

        private const float EndExtension = 1f;       // 끝 분대 바깥으로 이만큼 더 뻗는다
        private const float SingleHalfLength = 1.5f; // 분대가 하나면 이 폭의 짧은 막대
        private const float MoveThresholdSqr = 0.0225f; // 0.15 유닛 — 어느 점이든 이만큼 움직여야 다시 찍는다

        /// <summary>마지막으로 만든 전선의 무게중심입니다. 화살표가 여기서 출발합니다.</summary>
        public Vector2 Centroid { get; private set; }

        private readonly Vector2[] _sorted = new Vector2[MaxPoints];
        private readonly float[] _projection = new float[MaxPoints];
        private readonly Vector2[] _last = new Vector2[MaxPoints];
        private int _lastCount;

        /// <summary>
        /// 전선 점을 채우고 개수를 돌려줍니다. 살아 있는 분대가 없으면 0입니다.
        /// </summary>
        public int Build(IReadOnlyList<Squad> squads, Vector2 along, Vector2[] points)
        {
            int n = 0;
            Vector2 sum = Vector2.zero;

            for (int i = 0; i < squads.Count && n < MaxPoints - 2; i++)
            {
                Squad squad = squads[i];
                if (squad == null || squad.IsDead || squad.AliveCount <= 0)
                    continue;

                Vector2 position = squad.transform.position;
                float projection = Vector2.Dot(position, along);
                sum += position;

                int j = n - 1;
                while (j >= 0 && _projection[j] > projection)
                {
                    _projection[j + 1] = _projection[j];
                    _sorted[j + 1] = _sorted[j];
                    j--;
                }

                _projection[j + 1] = projection;
                _sorted[j + 1] = position;
                n++;
            }

            if (n == 0)
                return 0;

            Centroid = sum / n;

            if (n == 1)
            {
                points[0] = _sorted[0] - along * SingleHalfLength;
                points[1] = _sorted[0] + along * SingleHalfLength;
                return 2;
            }

            points[0] = _sorted[0] - along * EndExtension;
            for (int i = 0; i < n; i++)
                points[i + 1] = _sorted[i];
            points[n + 1] = _sorted[n - 1] + along * EndExtension;
            return n + 2;
        }

        /// <summary>
        /// 지난번과 눈에 띄게 달라졌는지입니다. 달라졌으면 이번 것을 기억합니다.
        /// </summary>
        public bool Changed(Vector2[] points, int count)
        {
            bool changed = count != _lastCount;
            for (int i = 0; i < count && !changed; i++)
                changed = (points[i] - _last[i]).sqrMagnitude > MoveThresholdSqr;

            if (!changed)
                return false;

            _lastCount = count;
            for (int i = 0; i < count; i++)
                _last[i] = points[i];

            return true;
        }
    }
}
