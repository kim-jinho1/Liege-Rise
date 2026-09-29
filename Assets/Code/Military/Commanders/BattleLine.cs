using UnityEngine;

namespace Code.Military.Commanders
{
    /// <summary>
    /// 지휘관에게 그은 공세선입니다. 두 점과 뒤집힘만 쥐고, 미는 방향은 선에 수직으로 계산합니다.
    /// </summary>
    public readonly struct BattleLine
    {
        public readonly Vector2 Start;
        public readonly Vector2 End;
        public readonly bool Flipped; // Tab — 수직 방향의 반대편으로 민다

        public BattleLine(Vector2 start, Vector2 end, bool flipped)
        {
            Start = start;
            End = end;
            Flipped = flipped;
        }

        public Vector2 Center => (Start + End) * 0.5f;

        public float Length => (End - Start).magnitude;

        public bool IsValid => (End - Start).sqrMagnitude > 0.01f;

        /// <summary>
        /// 선을 따라가는 단위 벡터입니다. 점이 겹치면 오른쪽으로 둡니다.
        /// </summary>
        public Vector2 Along
        {
            get
            {
                Vector2 delta = End - Start;
                return delta.sqrMagnitude < 0.0001f ? Vector2.right : delta.normalized;
            }
        }

        /// <summary>
        /// 미는 방향입니다. 선을 왼쪽으로 돌린 수직이 기본이고, 뒤집히면 반대편입니다.
        /// </summary>
        public Vector2 Normal
        {
            get
            {
                Vector2 along = Along;
                Vector2 normal = new(-along.y, along.x);
                return Flipped ? -normal : normal;
            }
        }

        public BattleLine Flip()
            => new(Start, End, !Flipped);

        public bool Same(in BattleLine other)
            => Flipped == other.Flipped
               && (Start - other.Start).sqrMagnitude < 0.0001f
               && (End - other.End).sqrMagnitude < 0.0001f;
    }
}
