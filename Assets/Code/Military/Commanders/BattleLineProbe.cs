using Code.Core.Bus;
using Code.Military.Combat;
using Code.Military.Squads;
using Code.Units.Combat;
using UnityEngine;

namespace Code.Military.Commanders
{
    /// <summary>
    /// 공세선 근처의 적 분대를 찾고, 미는 방향이 그 적의 어느 구간을 치는지 알려 줍니다.
    /// 판정은 SquadArmor의 콘을 그대로 쓰므로 표시와 피해가 갈라지지 않습니다.
    /// </summary>
    public sealed class BattleLineProbe
    {
        private const int MaxHits = 48;

        private readonly Collider2D[] _hits = new Collider2D[MaxHits];
        private ContactFilter2D _filter;
        private bool _ready;

        /// <summary>
        /// 지점에서 가장 가까운 적 분대입니다. 없으면 null입니다.
        /// </summary>
        public Squad FindNearestEnemySquad(Vector2 center, float radius, Owner owner)
        {
            // 필드 초기화자에서 만들어지는 경우가 있어 마스크는 첫 호출에 잡는다 —
            // MonoBehaviour 생성자 안에서는 LayerMask.NameToLayer가 예외를 던진다
            if (!_ready)
            {
                _ready = true;
                _filter = new ContactFilter2D();
                _filter.SetLayerMask(LayerMask.GetMask("Unit", "Enemy"));
                _filter.useTriggers = false;
            }

            int count = Physics2D.OverlapCircle(center, radius, _filter, _hits);

            Squad best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (!_hits[i].TryGetComponent(out AbstractCombatUnit unit))
                    continue;

                if (unit.Owner == owner || unit.IsDead)
                    continue;

                Squad squad = unit.Squad as Squad;
                if (squad == null || squad.AliveCount <= 0)
                    continue;

                float sqr = ((Vector2)squad.transform.position - center).sqrMagnitude;
                if (sqr >= bestSqr)
                    continue;

                bestSqr = sqr;
                best = squad;
            }

            return best;
        }

        /// <summary>
        /// 미는 방향으로 그 적을 치면 맞는 구간입니다. 공격자 방향은 피격자→공격자라 미는 방향의 반대입니다.
        /// </summary>
        public static ArmorZone ZoneOf(Squad enemy, Vector2 pushDirection)
            => enemy.PeekZone(-pushDirection);
    }
}
