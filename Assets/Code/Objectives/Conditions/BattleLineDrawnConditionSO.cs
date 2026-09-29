using System.Collections.Generic;
using Code.Buildings.Logic.CoreBuilding;
using Code.Core.Bus;
using Code.GameEvents.Unit;
using Code.Military.Commanders;
using Code.Sequences;
using Code.Stages;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    public enum BattleLinePlacement
    {
        Anywhere,          // 그었기만 하면 된다
        NearHeadquarters,  // 아군 본부 곁 — "성 앞으로 물러서라"
        BesideEnemyPath    // 아군 본부 ↔ 적 본거지 축에서 옆으로 벗어난 곳 — "길 옆 숲에 숨어라"
    }

    /// <summary>
    /// 지휘관이 공세선을 그었을 때 완료됩니다. 자리 조건은 씬에 마커를 두지 않고 본부 위치로 계산합니다 —
    /// 관람객이 대충 그어도 통과하도록 반경을 넉넉히 둡니다. 같은 지휘관이 여러 번 그어도 한 번으로 셉니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_BattleLineDrawn", menuName = "Objectives/Conditions/BattleLineDrawn")]
    public class BattleLineDrawnConditionSO : ObjectiveConditionSO
    {
        [field: SerializeField, Min(1)] public int Count { get; private set; } = 1; // 서로 다른 지휘관 수
        [field: SerializeField] public BattleLinePlacement Placement { get; private set; }
        [field: SerializeField] public float NearRadius { get; private set; } = 8f;   // NearHeadquarters
        [field: SerializeField] public float PathLateral { get; private set; } = 4f;  // BesideEnemyPath: 축에서 이만큼 옆

        public override ConditionTracker CreateTracker()
            => new BattleLineDrawnTracker(Count, Placement, NearRadius, PathLateral);
    }

    public class BattleLineDrawnTracker : ConditionTracker
    {
        public override int Current => _done.Count;

        public override int Target => _count;

        private readonly int _count;
        private readonly BattleLinePlacement _placement;
        private readonly float _nearRadius;
        private readonly float _pathLateral;
        private readonly HashSet<Commander> _done = new();

        public BattleLineDrawnTracker(int count, BattleLinePlacement placement, float nearRadius, float pathLateral)
        {
            _count = count;
            _placement = placement;
            _nearRadius = nearRadius;
            _pathLateral = pathLateral;
        }

        public override void Subscribe()
            => Bus<CommanderOrderEvent>.OnEvent[Owner.Player] += OnOrder;

        public override void Unsubscribe()
            => Bus<CommanderOrderEvent>.OnEvent[Owner.Player] -= OnOrder;

        private void OnOrder(CommanderOrderEvent e)
        {
            if (e.Kind != CommanderOrderKind.LineDrawn || e.Commander == null)
                return;

            if (!Accepts(e.LineCenter))
                return;

            if (!_done.Add(e.Commander))
                return;

            Report();
            if (_done.Count >= _count)
                Complete();
        }

        private bool Accepts(Vector2 center)
        {
            if (_placement == BattleLinePlacement.Anywhere)
                return true;

            // 스테이지에 자리를 찍어 뒀으면 그것이 기준이다. 계산으로 잡은 본부 앞은 폴백일 뿐이다 —
            // 안내 손은 찍어 둔 자리를 짚는데 조건만 성 앞을 요구하면 시킨 대로 해도 안 넘어간다
            if (TryGetAuthoredSpot(out Vector2 authored))
                return (center - authored).sqrMagnitude <= _nearRadius * _nearRadius;

            if (!TryFindHeadquarters(center, out Vector2 ours, out Vector2 theirs))
                return true; // 본부를 못 찾으면 자리 조건 없이 통과시킨다 — 튜토리얼이 막히는 쪽이 더 나쁘다

            if (_placement == BattleLinePlacement.NearHeadquarters)
                return (center - ours).sqrMagnitude <= _nearRadius * _nearRadius;

            Vector2 axis = theirs - ours;
            float length = axis.magnitude;
            if (length < 0.001f)
                return true;

            Vector2 rel = center - ours;
            float along = Vector2.Dot(rel, axis) / (length * length);
            float lateral = Mathf.Abs(rel.x * axis.y - rel.y * axis.x) / length;

            // 성과 소굴 사이 구간(양 끝 15%는 뺀다)에서 축을 옆으로 벗어나 있으면 매복이다
            return along > 0.15f && along < 0.85f && lateral >= _pathLateral;
        }

        /// <summary>
        /// 이 자리 조건에 대응하는, 스테이지에 찍어 둔 좌표입니다. 안 찍었으면 false입니다.
        /// </summary>
        private bool TryGetAuthoredSpot(out Vector2 spot)
        {
            spot = default;

            StageSO stage = StageDatabaseSO.Current;
            if (stage == null)
                return false;

            GameObjectHighlightType key = _placement == BattleLinePlacement.NearHeadquarters
                ? GameObjectHighlightType.DefendSpot
                : GameObjectHighlightType.AmbushSpot;

            return stage.TryGetGuidePoint(key, out spot);
        }

        /// <summary>
        /// 선에서 가장 가까운 아군 본부와 적 본거지입니다. 드문 이벤트라 그때 찾습니다.
        /// </summary>
        private static bool TryFindHeadquarters(Vector2 center, out Vector2 ours, out Vector2 theirs)
        {
            ours = theirs = default;
            float oursSqr = float.MaxValue, theirsSqr = float.MaxValue;

            foreach (HeadquartersBuilding hq in Object.FindObjectsByType<HeadquartersBuilding>())
            {
                if (hq == null || hq.IsDead)
                    continue;

                float sqr = ((Vector2)hq.transform.position - center).sqrMagnitude;
                if (hq.Owner == Owner.Player && sqr < oursSqr)
                {
                    oursSqr = sqr;
                    ours = hq.transform.position;
                }
                else if (hq.Owner == Owner.Enemy && sqr < theirsSqr)
                {
                    theirsSqr = sqr;
                    theirs = hq.transform.position;
                }
            }

            return oursSqr < float.MaxValue && theirsSqr < float.MaxValue;
        }
    }
}
