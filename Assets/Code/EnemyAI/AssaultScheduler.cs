using System.Collections.Generic;
using Code.Commandable;
using Code.Core.Bus;
using Code.GameEvents.Enemy;
using Code.Military.Commanders;
using Code.Military.Data;
using UnityEngine;

namespace Code.EnemyAI
{
    /// <summary>
    /// 시간표가 부른 웨이브 하나를 실제 병력으로 옮깁니다.
    /// 진지에 모여 있던 병력을 데려가는 것이 아니라 그 자리에서 새로 생성합니다 —
    /// 그래야 표에 적은 숫자가 생산 속도·맵 편차와 무관하게 그대로 나옵니다.
    /// </summary>
    public class AssaultScheduler
    {
        private readonly List<EnemyGarrison> _origins = new(8);
        private readonly List<AbstractCommandable> _column = new(8);
        private CommanderSO _commanderData; // 갈래마다 하나씩 세울 적 지휘관. 없으면 갈래는 예전처럼 직진한다

        /// <summary>
        /// 웨이브를 집행하고 실제로 내보낸 분대 수를 반환합니다.
        /// </summary>
        public int Execute(AssaultWave wave, SquadSO[] roster, IReadOnlyList<EnemyGarrison> garrisons,
            float scatterRadius, CommanderSO commander = null)
        {
            if (roster == null || roster.Length == 0)
                return 0;

            _commanderData = commander;

            CollectOrigins(garrisons);
            int columns = wave.ResolveColumns(_origins.Count);
            if (columns == 0)
                return 0;

            Shuffle(_origins);

            int dispatched = 0;
            for (int column = 0; column < columns; column++)
            {
                int share = ShareFor(wave.squadCount, columns, column);
                if (share <= 0)
                    continue;

                dispatched += LaunchColumn(_origins[column], share, roster, scatterRadius);
            }

            return dispatched;
        }

        /// <summary>
        /// 이번 웨이브가 출발할 수 있는 진지를 모읍니다.
        /// 전초 진지가 하나라도 살아 있으면 그쪽만 쓰고, 전부 무너졌을 때만 본거지에서 나옵니다.
        /// </summary>
        private void CollectOrigins(IReadOnlyList<EnemyGarrison> garrisons)
        {
            _origins.Clear();
            if (garrisons == null)
                return;

            foreach (EnemyGarrison garrison in garrisons)
                if (garrison != null && garrison.IsActiveGarrison && !garrison.IsLastResortOrigin)
                    _origins.Add(garrison);

            if (_origins.Count > 0)
                return;

            foreach (EnemyGarrison garrison in garrisons)
                if (garrison != null && garrison.IsActiveGarrison)
                    _origins.Add(garrison);
        }

        /// <summary>
        /// 진지 하나에서 분대를 생성해 한 갈래로 내보냅니다.
        /// </summary>
        private int LaunchColumn(EnemyGarrison origin, int squadCount, SquadSO[] roster, float scatterRadius)
        {
            _column.Clear();
            Vector3 center = origin.SpawnPosition;
            int strength = 0;

            // 갈래는 한 병종으로 채운다. 섞어 뽑으면 "돼지기수 갈래는 우회한다"가 다수결 운에 걸린다 —
            // 한 갈래가 한 병종이면 성향이 명확하고, 화면에서도 기병 갈래·보병 갈래로 읽힌다
            SquadSO pick = roster[Random.Range(0, roster.Length)];
            if (pick == null || pick.Prefab == null)
                return 0;

            for (int i = 0; i < squadCount; i++)
            {

                AbstractCommandable spawned = SpawnSquad(pick, ScatterAround(center, scatterRadius));
                if (spawned == null)
                    continue;

                _column.Add(spawned);
                strength += Mathf.Max(1, pick.MemberCount); // 아직 Start 전이라 AliveCount를 못 읽는다
            }

            if (_column.Count == 0)
                return 0;

            AssaultForce.Create(_column, SpawnCommander(center));
            Bus<EnemyDispatchEvent>.Raise(Owner.Enemy, new EnemyDispatchEvent(center, strength));
            return _column.Count;
        }

        /// <summary>
        /// 갈래의 지휘관을 출발지에 세웁니다. 데이터가 비었으면 null이고 갈래는 지휘관 없이 갑니다.
        /// </summary>
        private Commander SpawnCommander(Vector3 position)
        {
            if (_commanderData == null || _commanderData.Prefab == null)
                return null;

            GameObject spawned = Object.Instantiate(_commanderData.Prefab, position, Quaternion.identity);
            if (!spawned.TryGetComponent(out Commander commander))
            {
                Object.Destroy(spawned);
                return null;
            }

            commander.Owner = Owner.Enemy;
            commander.AbstractCommandableSO = _commanderData;
            commander.IsSpawnerProduced = false;
            return commander;
        }

        /// <summary>
        /// 분대 하나를 적 소속으로 생성합니다.
        /// </summary>
        private static AbstractCommandable SpawnSquad(SquadSO data, Vector3 position)
        {
            GameObject spawned = Object.Instantiate(data.Prefab, position, Quaternion.identity);
            if (!spawned.TryGetComponent(out AbstractCommandable commandable))
            {
                Object.Destroy(spawned);
                return null;
            }

            commandable.Owner = Owner.Enemy;
            commandable.AbstractCommandableSO = data; // 분대는 Start에서 이 값으로 편성을 읽는다

            // 스포너 산출물로 표시하면 EnemyAIManager가 곧바로 진지 수비대로 되돌린다
            commandable.IsSpawnerProduced = false;
            return commandable;
        }

        /// <summary>
        /// 같은 좌표에 겹쳐 나지 않도록 출발지 주변으로 흩습니다.
        /// </summary>
        private static Vector3 ScatterAround(Vector3 center, float radius)
        {
            if (radius <= 0f)
                return center;

            Vector2 offset = Random.insideUnitCircle * radius;
            return center + new Vector3(offset.x, offset.y, 0f);
        }

        /// <summary>
        /// 총 분대 수를 갈래 수로 나눈 몫입니다. 나머지는 앞 갈래부터 하나씩 더 가져갑니다.
        /// </summary>
        private static int ShareFor(int total, int columns, int index)
            => total / columns + (index < total % columns ? 1 : 0);

        private static void Shuffle(List<EnemyGarrison> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int swap = Random.Range(0, i + 1);
                (items[i], items[swap]) = (items[swap], items[i]);
            }
        }
    }
}
