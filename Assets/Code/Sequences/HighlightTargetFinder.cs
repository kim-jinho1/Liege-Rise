using System.Collections.Generic;
using Code.Buildings.Base;
using Code.Buildings.Data;
using Code.Buildings.Logic.CoreBuilding;
using Code.Commandable;
using Code.Core;
using Code.Core.Bus;
using Code.Environments;
using Code.Military.Commanders;
using Code.Military.Squads;
using Code.Players.Cursors;
using Code.Sequences.Guide;
using Code.Units.Pawns;
using UnityEngine;

namespace Code.Sequences
{
    /// <summary>
    /// 힌트 종류마다 씬에서 대상을 직접 찾습니다. 프리팹에 태그를 달지 않으므로 어느 종류든 같은 방식으로 켜집니다.
    /// 스텝이 바뀌거나 1초마다 한 번 도는 정도라 FindObjectsByType 비용은 무시할 만합니다.
    /// 강조할 오브젝트 없이 좌표만 내는 종류는 GuidePointFinder가 풉니다.
    /// </summary>
    public static class HighlightTargetFinder
    {
        private const float EnemyClusterRadius = 12f; // 가장 가까운 적과 이 안에 있는 것까지 한 무리로 본다
        private const int TreeCount = 3;

        private static readonly List<IGuideHighlightable> Scratch = new(16);

        /// <summary>
        /// 종류에 맞는 살아 있는 대상을 전부 모읍니다.
        /// </summary>
        public static void Collect(GameObjectHighlightType type, List<IGuideHighlightable> into)
        {
            into.Clear();

            switch (type)
            {
                case GameObjectHighlightType.Pawn:
                    CollectPawns(into);
                    break;

                case GameObjectHighlightType.Commander:
                case GameObjectHighlightType.Commander1:
                case GameObjectHighlightType.Commander2:
                    int ordinal = type switch
                    {
                        GameObjectHighlightType.Commander1 => 1,
                        GameObjectHighlightType.Commander2 => 2,
                        _ => 0
                    };
                    foreach (Commander commander in Object.FindObjectsByType<Commander>())
                        if (IsAlive(commander, Owner.Player) && (ordinal == 0 || commander.Ordinal == ordinal))
                            into.Add(commander);
                    break;

                case GameObjectHighlightType.Squad:
                    CollectSquads(into);
                    break;

                case GameObjectHighlightType.FreeArmy:
                    CollectFreeArmy(into);
                    break;

                case GameObjectHighlightType.MainCastle:
                    foreach (HeadquartersBuilding castle in Object.FindObjectsByType<HeadquartersBuilding>())
                        if (IsAlive(castle, Owner.Player))
                            into.Add(castle);
                    break;

                case GameObjectHighlightType.Barracks:
                case GameObjectHighlightType.Farm:
                case GameObjectHighlightType.Cottage:
                    foreach (AbstractBuilding building in Object.FindObjectsByType<AbstractBuilding>())
                        if (IsAlive(building, Owner.Player) && MatchesBuilding(type, building))
                            into.Add(building);
                    break;

                case GameObjectHighlightType.Tree:
                    CollectTrees(into);
                    break;

                case GameObjectHighlightType.Enemy:
                    CollectEnemyCluster(into);
                    break;

                case GameObjectHighlightType.EnemyBase:
                    foreach (HeadquartersMarker marker in Object.FindObjectsByType<HeadquartersMarker>())
                        if (IsAlive(marker.Building, Owner.Enemy))
                            into.Add(marker.Building);
                    break;
            }
        }

        /// <summary>
        /// 종류 중 reference에 가장 가까운 대상의 좌표입니다. AmbushSpot은 대상 없이 좌표만 냅니다.
        /// </summary>
        public static bool TryGetNearest(GameObjectHighlightType type, Vector3 reference, out Vector3 position)
        {
            if (GuidePointFinder.IsAuthoredPoint(type))
                return GuidePointFinder.TryGetAuthoredPoint(type, out position);

            Collect(type, Scratch);
            IGuideHighlightable nearest = Nearest(Scratch, reference);
            Scratch.Clear();

            // nearest는 인터페이스라 != null이 파괴된 오브젝트를 걸러 주지 못한다
            if (!UnityRef.IsAlive(nearest))
            {
                position = default;
                return false;
            }

            position = nearest.GuidePosition;
            return true;
        }

        /// <summary>
        /// 목록에서 reference에 가장 가까운 것입니다. 비어 있으면 null.
        /// </summary>
        public static IGuideHighlightable Nearest(List<IGuideHighlightable> list, Vector3 reference)
        {
            IGuideHighlightable best = null;
            float bestSqr = float.MaxValue;
            foreach (IGuideHighlightable candidate in list)
            {
                float sqr = (candidate.GuidePosition - reference).sqrMagnitude;
                if (sqr >= bestSqr)
                    continue;

                bestSqr = sqr;
                best = candidate;
            }

            return best;
        }

        /// <summary>
        /// 그 종류 중 하나라도 지금 선택돼 있는지입니다. 이미 골라 둔 것을 다시 고르라고 하지 않기 위해 봅니다.
        /// </summary>
        public static bool IsAnySelected(GameObjectHighlightType type)
        {
            Collect(type, Scratch);
            bool selected = false;
            foreach (IGuideHighlightable target in Scratch)
            {
                if (target is not AbstractCommandable commandable || !commandable.IsSelected)
                    continue;

                selected = true;
                break;
            }

            Scratch.Clear();
            return selected;
        }

        /// <summary>
        /// 플레이어 일꾼입니다. 건물 안에 들어간 일꾼은 보이지도 않으니 빼고, 노는 일꾼이 있으면 그것만 —
        /// 농장에서 일하는 일꾼에게 집을 지으라고 하지 않기 위해서입니다.
        /// </summary>
        private static void CollectPawns(List<IGuideHighlightable> into)
        {
            bool anyIdle = false;
            foreach (Pawn pawn in Object.FindObjectsByType<Pawn>())
            {
                if (!IsAlive(pawn, Owner.Player) || pawn.StationedBuilding != null)
                    continue;

                into.Add(pawn);
                anyIdle |= pawn.IsAvailable;
            }

            if (!anyIdle)
                return;

            for (int i = into.Count - 1; i >= 0; i--)
                if (!((Pawn)into[i]).IsAvailable)
                    into.RemoveAt(i);
        }

        /// <summary>
        /// 플레이어 분대입니다. 지휘관 없는 분대가 하나라도 있으면 그것만 — 배속·임명 스텝이 새 분대를 짚기 위해서입니다.
        /// </summary>
        private static void CollectSquads(List<IGuideHighlightable> into)
        {
            // 배속된 분대로 대신 짚지 않는다 — 이미 붙은 분대를 또 붙이라고 하게 된다. 없으면 빈 채로 둔다
            foreach (Squad squad in Object.FindObjectsByType<Squad>())
                if (IsAlive(squad, Owner.Player) && squad.AliveCount > 0 && !squad.HasCommander)
                    into.Add(squad);
        }

        /// <summary>
        /// 분대 없는 지휘관과 지휘관 없는 분대입니다. "같이 담고 긋기"가 한 상자에 담을 것들입니다.
        /// </summary>
        private static void CollectFreeArmy(List<IGuideHighlightable> into)
        {
            foreach (Commander commander in Object.FindObjectsByType<Commander>())
                if (IsAlive(commander, Owner.Player) && commander.Roster.Count == 0)
                    into.Add(commander);

            if (into.Count == 0)
                return; // 지휘관이 없으면 담을 부대가 아니다

            CollectSquads(into);
        }

        /// <summary>
        /// 성에서 가까운 나무 몇 그루입니다. 숲 전체를 켜면 어디를 누르라는지 도리어 안 보입니다.
        /// </summary>
        private static void CollectTrees(List<IGuideHighlightable> into)
        {
            foreach (ResourceObject resource in Object.FindObjectsByType<ResourceObject>())
                if (!resource.IsDepleted && resource.ResourceSO != null
                    && resource.ResourceSO.HoverCursor == CursorKind.GatherWood)
                    into.Add(resource);

            if (!GuidePointFinder.TryGetPlayerCastle(out Vector3 castle))
                castle = Vector3.zero;

            into.Sort((a, b) => (a.GuidePosition - castle).sqrMagnitude.CompareTo((b.GuidePosition - castle).sqrMagnitude));
            if (into.Count > TreeCount)
                into.RemoveRange(TreeCount, into.Count - TreeCount);
        }

        /// <summary>
        /// 성에서 가장 가까운 적과 그 주변 무리입니다. 16스텝은 길목 망루가, 20스텝은 내려온 고블린 열이 잡힙니다.
        /// </summary>
        private static void CollectEnemyCluster(List<IGuideHighlightable> into)
        {
            foreach (AbstractBuilding building in Object.FindObjectsByType<AbstractBuilding>())
                if (IsAlive(building, Owner.Enemy))
                    into.Add(building);

            foreach (Squad squad in Object.FindObjectsByType<Squad>())
                if (IsAlive(squad, Owner.Enemy) && squad.AliveCount > 0)
                    into.Add(squad);

            if (!GuidePointFinder.TryGetPlayerCastle(out Vector3 castle))
                castle = Vector3.zero;

            IGuideHighlightable nearest = Nearest(into, castle);
            if (nearest == null)
                return;

            Vector3 center = nearest.GuidePosition;
            float radiusSqr = EnemyClusterRadius * EnemyClusterRadius;
            for (int i = into.Count - 1; i >= 0; i--)
                if ((into[i].GuidePosition - center).sqrMagnitude > radiusSqr)
                    into.RemoveAt(i);
        }

        private static bool MatchesBuilding(GameObjectHighlightType type, AbstractBuilding building)
            => type switch
            {
                GameObjectHighlightType.Barracks => building.BuildingSO is SquadRecruitmentBuildingSO,
                GameObjectHighlightType.Farm => building.BuildingSO is FarmBuildingSO,
                GameObjectHighlightType.Cottage => building.BuildingSO is PopulationBuildingSO,
                _ => false
            };

        private static bool IsAlive(AbstractCommandable commandable, Owner owner)
            => commandable != null && !commandable.IsDead && commandable.Owner == owner;
    }
}
