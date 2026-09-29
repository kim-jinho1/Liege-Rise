using System;
using System.Threading;
using Code.Buildings.Base;
using Code.Buildings.Data;
using Code.Commandable;
using Code.EnemyAI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Code.Buildings.Logic.EnemyBuilding
{
    /// <summary>
    /// 자기 진지를 지킬 수비 병력을 채우는 건물입니다.
    /// 공세 병력은 여기서 나오지 않습니다 — 그건 EnemyAIManager의 시간표가 그때그때 생성합니다.
    /// 그래서 진지 명부가 상한까지 차면 이 건물은 생산을 쉽니다.
    /// </summary>
    public class SpawnerBuilding : AbstractBuilding
    {
        [SerializeField] private SpawnUnitInfo[] spawnInfos;

        private float _productionInterval = 24f;
        private CancellationTokenSource _productionCts;

        protected override void Awake()
        {
            base.Awake();
            Debug.Assert(spawnInfos is { Length: > 0 },
                $"SpawnerBuilding {gameObject.name} requires spawn data.");

            SpawnerBuildingSO spawnerData = BuildingSO as SpawnerBuildingSO;
            if (spawnerData != null)
                _productionInterval = spawnerData.SpawnInterval;
        }

        protected override void Start()
        {
            base.Start();
            StartProductionLoop();
        }

        protected override void OnDestroy()
        {
            _productionCts?.Cancel();
            _productionCts?.Dispose();
            base.OnDestroy();
        }

        private void StartProductionLoop()
        {
            _productionCts?.Cancel();
            _productionCts?.Dispose();
            _productionCts = new CancellationTokenSource();
            RunProductionLoopAsync(_productionCts.Token);
        }

        private async void RunProductionLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Awaitable.WaitForSecondsAsync(_productionInterval, token);
                    SpawnUnit();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 확률 가중치로 유닛을 선택하여 스폰합니다.
        /// 거둘 진지가 가득 차 있으면 이번 주기를 건너뜁니다 — 만들어 놓고 버리지 않기 위해서입니다.
        /// </summary>
        private void SpawnUnit()
        {
            if (spawnInfos == null || spawnInfos.Length == 0 || SpawnPoint == null)
                return;

            if (EnemyAIManager.Instance != null && !EnemyAIManager.Instance.HasRoomFor(SpawnPoint.position))
                return;

            AbstractCommandableSO pick = PickTarget(spawnInfos);
            if (pick == null || pick.Prefab == null)
                return;

            GameObject spawned = Instantiate(pick.Prefab, SpawnPoint.position, SpawnPoint.rotation);
            if (!spawned.TryGetComponent(out AbstractCommandable commandable))
                return;

            commandable.Owner = Owner;
            commandable.AbstractCommandableSO = pick; // 분대는 Start에서 이 값으로 편성을 읽는다
            commandable.IsSpawnerProduced = true;
        }

        /// <summary>
        /// SpawnUnitInfo 배열에서 Probability 가중치 기반으로 유닛 SO를 선택합니다.
        /// </summary>
        private static AbstractCommandableSO  PickTarget(SpawnUnitInfo[] infos)
        {
            if (infos == null || infos.Length == 0)
                return null;

            float total = 0f;
            foreach (SpawnUnitInfo info in infos)
                total += info.Probability;

            float roll = Random.Range(0f, total);
            float cumulative = 0f;
            foreach (SpawnUnitInfo info in infos)
            {
                cumulative += info.Probability;
                if (roll <= cumulative)
                    return info.UnitData;
            }

            return infos[^1].UnitData;
        }
    }
}
