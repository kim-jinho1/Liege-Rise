using System;
using System.Collections.Generic;
using Code.Bootstraps;
using Code.Commandable;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Unit;
using Code.Military.Data;
using Code.Stages;
using UnityEngine;

namespace Code.EnemyAI
{
    /// <summary>
    /// 적 공세의 시계입니다. 시간표(EnemyAIConfigSO)에 적힌 시각마다 웨이브를 집행하고,
    /// 스포너가 뽑은 병력은 가장 가까운 진지의 수비대로 배정합니다.
    /// 병력이 쌓여서 나가는 경로는 없습니다 — 언제 얼마나 나갈지는 표만 정합니다.
    /// </summary>
    public class EnemyAIManager : MonoSingleton<EnemyAIManager>, IGameStartListener
    {
        // GameModules/Enemy/EnemyAIConfig.asset. 비어 있으면 공세가 한 번도 나오지 않는다
        [SerializeField] private EnemyAIConfigSO config;

        /// <summary>
        /// Bootstrap 초기화 우선순위입니다. 다른 매니저 초기화 이후 적 AI를 시작합니다.
        /// </summary>
        public int InitOrder => 100;

        /// <summary>
        /// 진지 하나가 곁에 둘 수비 병력의 상한입니다. 진지와 스포너가 읽습니다.
        /// </summary>
        public int DefenseCapacity => Active != null ? Active.DefenseCapacity : 7;
        public float FlankRange => Active != null ? Active.FlankRange : 18f;
        public float ReserveRange => Active != null ? Active.ReserveRange : 30f;

        /// <summary>
        /// 이번 판이 쓰는 시간표입니다. 스테이지가 자기 표를 들고 있으면 그것, 아니면 _Core의 기본 표입니다.
        /// </summary>
        private EnemyAIConfigSO Active
        {
            get
            {
                StageSO stage = StageDatabaseSO.Current;
                return stage != null && stage.AssaultConfig != null ? stage.AssaultConfig : config;
            }
        }

        private readonly List<EnemyGarrison> _garrisons = new(16);
        private readonly AssaultScheduler _scheduler = new();

        private BusSubscriptionScope _subscriptionScope;

        // 시간표가 이미 돌고 있는지. 시퀀스가 개시 효과를 두 번 태워도 시계가 겹치지 않게 막는다
        private bool _started;

        protected override void Awake()
        {
            base.Awake();

            WarnIfUnconfigured();
            AssaultRegistry.ResetRun();

            _subscriptionScope = new BusSubscriptionScope();
            _subscriptionScope.Subscribe<UnitSpawnEvent>(Owner.Enemy, HandleUnitSpawned);
            _subscriptionScope.Subscribe<SquadSpawnEvent>(Owner.Enemy, HandleSquadSpawned);
        }

        protected override void OnDestroy()
        {
            _subscriptionScope?.Dispose();
            base.OnDestroy();
        }

        /// <summary>
        /// 게임 시작 시 호출되어 공세 시간표를 돌리기 시작합니다.
        /// 스테이지가 미루기로 했으면 시퀀스가 BeginAssaults를 부를 때까지 기다립니다.
        /// </summary>
        public void OnGameStart()
        {
            RefreshGarrisons();

            if (StageDatabaseSO.Current != null && StageDatabaseSO.Current.DeferAssaults)
                return;

            BeginAssaults();
        }

        /// <summary>
        /// 공세 시간표를 지금부터 돌립니다.
        /// firstWaveDelay가 음수면 표의 시각을 그대로 쓰고(이 호출이 0초),
        /// 값이 있으면 첫 웨이브가 그만큼 뒤에 오도록 표 전체를 당깁니다.
        /// 두 번 불려도 안전합니다 — 스텝을 되돌아오거나 효과가 중복 배선될 수 있습니다.
        /// </summary>
        public void BeginAssaults(float firstWaveDelay = -1f)
        {
            if (_started)
                return;

            _started = true;
            RunScheduleAsync(firstWaveDelay);
        }

        /// <summary>
        /// 시간표의 원점입니다. 첫 웨이브가 정확히 지연만큼 뒤에 오도록 표를 통째로 밀거나 당깁니다 —
        /// 당기기만 하면 첫 시각이 0인 표는 지연을 무시하고 그 자리에서 바로 나옵니다.
        /// </summary>
        private float ResolveStartTime(float firstWaveDelay)
        {
            if (firstWaveDelay < 0f || !Active.TryGetWave(0, out _, out float firstAt))
                return Time.time;

            return Time.time - (firstAt - firstWaveDelay);
        }

        /// <summary>
        /// 시간표가 배선돼 있는지 확인합니다. 없으면 공세가 한 번도 발생하지 않습니다.
        /// </summary>
        private void WarnIfUnconfigured()
        {
            if (config == null)
                Debug.LogWarning($"[{name}] EnemyAIConfig가 배선되지 않아 공세가 발생하지 않습니다. " +
                                 "_Core의 EnemyAIManager에 GameModules/Enemy/EnemyAIConfig.asset을 넣으세요.", this);
        }

        /// <summary>
        /// 현재 씬에 존재하는 적 진지를 다시 모읍니다. 파괴된 진지를 걷어내려 웨이브마다 호출합니다.
        /// </summary>
        private void RefreshGarrisons()
        {
            _garrisons.Clear();
            foreach (EnemyGarrison garrison in FindObjectsByType<EnemyGarrison>())
                if (garrison.IsActiveGarrison)
                    _garrisons.Add(garrison);

            if (_garrisons.Count == 0)
                Debug.LogWarning($"[{name}] EnemyGarrison이 하나도 없습니다. 공세가 출발할 자리가 없습니다.", this);
        }

        private void HandleUnitSpawned(UnitSpawnEvent evt)
            => Assign(evt.Unit);

        private void HandleSquadSpawned(SquadSpawnEvent evt)
            => Assign(evt.Squad as AbstractCommandable);

        /// <summary>
        /// 스포너가 뽑은 병력을 진지 수비대로 배정합니다.
        /// 반경 안의 진지를 먼저 보고, 어느 반경에도 들지 않으면 전역 최근접 진지가 거둡니다 —
        /// 거두는 쪽이 없으면 그 병력은 아무 명부에도 오르지 않고 영원히 서 있게 됩니다.
        /// </summary>
        private void Assign(AbstractCommandable unit)
        {
            if (unit == null || !unit.IsSpawnerProduced)
                return;

            EnemyGarrison receiver = FindReceiver(unit.transform.position);
            if (receiver != null)
                receiver.Register(unit);
        }

        /// <summary>
        /// 이 자리에서 나온 병력을 거둘 진지가 아직 여유가 있는지입니다.
        /// 스포너가 생산 직전에 물어봐, 가득 찬 진지에 병력이 더 쌓이지 않게 합니다.
        /// </summary>
        public bool HasRoomFor(Vector3 position)
        {
            EnemyGarrison receiver = FindReceiver(position);
            return receiver == null || receiver.HasRoom;
        }

        /// <summary>
        /// 이 좌표의 병력을 거둘 진지입니다. 반경 안의 진지를 먼저 보고,
        /// 어느 반경에도 들지 않으면 전역 최근접 진지가 거둡니다 —
        /// 거두는 쪽이 없으면 그 병력은 아무 명부에도 오르지 않고 영원히 서 있게 됩니다.
        /// </summary>
        private EnemyGarrison FindReceiver(Vector3 position)
        {
            EnemyGarrison covering = null;
            EnemyGarrison nearest = null;
            float bestCovering = float.MaxValue;
            float bestOverall = float.MaxValue;

            foreach (EnemyGarrison garrison in _garrisons)
            {
                if (garrison == null)
                    continue;

                float sqr = garrison.SqrDistanceTo(position);
                if (sqr < bestCovering)
                {
                    bestCovering = sqr;
                    covering = garrison;
                }

                float unbounded = garrison.SqrDistanceToUnbounded(position);
                if (unbounded >= bestOverall)
                    continue;

                bestOverall = unbounded;
                nearest = garrison;
            }

            return covering ?? nearest;
        }

        /// <summary>
        /// 시간표를 순서대로 집행합니다. 표를 다 쓰면 마지막 줄이 RepeatInterval 간격으로 되풀이됩니다.
        /// </summary>
        private async void RunScheduleAsync(float firstWaveDelay)
        {
            EnemyAIConfigSO table = Active;
            if (table == null)
                return;

            float startTime = ResolveStartTime(firstWaveDelay);

            try
            {
                for (int index = 0; ; index++)
                {
                    // 표를 다 썼고 되풀이하지 않는 표면 여기서 조용히 끝난다
                    if (!table.TryGetWave(index, out AssaultWave wave, out float atSeconds))
                    {
                        if (index == 0)
                            Debug.LogWarning($"[{name}] 공세 시간표가 비어 있습니다.", this);
                        return;
                    }

                    float wait = startTime + atSeconds - Time.time;

                    // 시각이 이미 지났어도 한 프레임은 반드시 넘긴다 — 표에 같은 시각이 여러 줄 있으면
                    // 그냥 continue할 경우 프레임 안에서 무한히 돌아 에디터가 멈춘다
                    if (wait > 0f)
                        await Awaitable.WaitForSecondsAsync(wait, destroyCancellationToken);
                    else
                        await Awaitable.NextFrameAsync(destroyCancellationToken);

                    LaunchWave(wave, index);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// 웨이브 하나를 집행하고 결과를 남깁니다.
        /// 요구한 규모보다 적게 나갔다면 출발할 진지가 모자랐다는 뜻입니다.
        /// </summary>
        private void LaunchWave(AssaultWave wave, int index)
        {
            RefreshGarrisons();

            EnemyAIConfigSO table = Active;
            SquadSO[] roster = table.RosterFor(wave);
            if (roster == null || roster.Length == 0)
            {
                Debug.LogWarning($"[{name}] 웨이브 #{index + 1}에 쓸 적 분대 목록이 비어 있어 건너뜁니다.", this);
                return;
            }

            int dispatched = _scheduler.Execute(wave, roster, _garrisons, table.SpawnScatterRadius, table.Commander);
            Debug.Log($"[{name}] 웨이브 #{index + 1} 출발 — {dispatched}부대, 시각 {Time.time:F0}s, 표 {table.name}", this);

            if (dispatched < wave.squadCount)
                Debug.LogWarning($"[{name}] 웨이브 #{index + 1}: {wave.squadCount}부대 중 {dispatched}부대만 출발했습니다.",
                    this);
        }
    }
}
