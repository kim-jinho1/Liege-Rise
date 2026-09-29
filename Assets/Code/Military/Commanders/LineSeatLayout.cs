using System.Collections.Generic;
using Code.Military.Squads;
using UnityEngine;

namespace Code.Military.Commanders
{
    /// <summary>
    /// 선 위 자리 계산입니다. 분대를 선 방향 순서로 정렬해 겹치지 않는 간격으로 나눠 세웁니다.
    /// </summary>
    public sealed class LineSeatLayout
    {
        private const float MinSlotSpacing = 1.5f;   // 분대끼리 겹치지 않는 최소 간격
        private const float MaxSlotSpacing = 6f;     // 선이 아무리 길어도 이보다 벌어지지 않는다

        // 선 위 순서를 정하는 임시 표. 명부 상한이 작아 고정 크기로 두고 할당을 피한다
        private readonly Squad[] _ordered = new Squad[CommanderRoster.DefaultCapacity + 2];
        private readonly float[] _projection = new float[CommanderRoster.DefaultCapacity + 2];

        /// <summary>마지막 정렬 결과입니다. 인덱스는 SortAlong이 돌려준 수 안에서만 유효합니다.</summary>
        public Squad this[int index] => _ordered[index];

        /// <summary>
        /// 선 위 i번째 자리에서 앞으로 ahead만큼 나간 지점입니다. 절벽·물 위면 가장 가까운 통행 칸으로 옮깁니다.
        /// </summary>
        public Vector2 Seat(in BattleLine line, int index, int count, float ahead)
        {
            Vector2 along = line.Along;
            float spacing = count == 1
                ? 0f
                : Mathf.Clamp(line.Length / (count - 1), MinSlotSpacing, MaxSlotSpacing);

            float total = spacing * (count - 1);
            Vector2 first = line.Center - along * (total * 0.5f);

            return LineTerrain.Snap(first + along * (spacing * index) + line.Normal * ahead);
        }

        /// <summary>
        /// 선 방향으로 사영해 삽입 정렬합니다. 여섯을 넘지 않아 이보다 나은 정렬이 필요 없습니다.
        /// </summary>
        public int SortAlong(in BattleLine line, IReadOnlyList<Squad> squads)
        {
            int count = Mathf.Min(squads.Count, _ordered.Length);
            Vector2 along = line.Along;

            for (int i = 0; i < count; i++)
            {
                Squad squad = squads[i];
                float projection = Vector2.Dot(squad.transform.position, along);

                int j = i - 1;
                while (j >= 0 && _projection[j] > projection)
                {
                    _projection[j + 1] = _projection[j];
                    _ordered[j + 1] = _ordered[j];
                    j--;
                }

                _projection[j + 1] = projection;
                _ordered[j + 1] = squad;
            }

            return count;
        }
    }
}
