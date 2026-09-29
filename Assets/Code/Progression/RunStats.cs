using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Building;
using Code.GameEvents.Economy;
using Code.GameEvents.Enemy;
using Code.GameEvents.Unit;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Progression
{
    /// <summary>
    /// 이번 판의 전적을 셉니다. 결과 화면이 읽어 갑니다.
    /// 씬 배선이 필요 없도록 스스로 붙습니다 — 매니저를 하나 더 두고 배선을 놓치면 조용히 0이 됩니다.
    /// </summary>
    public static class RunStats
    {
        public static int EnemyUnitsKilled { get; private set; }
        public static int EnemyBuildingsDestroyed { get; private set; }
        public static int SquadsLost { get; private set; }
        public static int SuppliesGathered { get; private set; }

        /// <summary>
        /// 이번 판이 시작될 때의 최고 기록(초)입니다. 기록이 없으면 0보다 작습니다.
        /// 판이 끝나면 StageProgressRecorder가 먼저 새 기록을 써 버리므로, 신기록 판정은 이 값으로 합니다.
        /// </summary>
        public static float BestBeforeRun { get; private set; } = -1f;

        /// <summary>
        /// 이번 판이 시작된 뒤 흐른 시간(초)입니다.
        /// Time.timeSinceLevelLoad를 쓰면 안 됩니다. 스테이지 씬은 _Core 위에
        /// additive로 얹히므로 그 시계는 판을 다시 시작해도 돌아가지 않습니다 — 한 세션의 두 번째
        /// 판부터 메뉴에 머문 시간과 앞선 판이 전부 얹혀 기록이 영원히 안 깨집니다.
        /// </summary>
        public static float ElapsedSeconds => Mathf.Max(0f, Time.time - _startedAt);

        private static float _startedAt;

        // SubsystemRegistration이 아니다 — BusRegistry가 그 시점에 모든 버스를 비우는데 둘의 순서가 정해져 있지 않아
        // 빌드에서 구독이 지워진 채 시작해 결과 창이 전부 0이었다. 씬이 올라온 뒤에 걸고, 스테이지마다 다시 건다
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            Reset();
            CaptureBestForLoadedStage();
            Subscribe();

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        /// <summary>
        /// 이미 올라와 있는 스테이지의 기록을 읽습니다.
        /// 에디터에서 스테이지 씬을 직접 Play하면 그 씬의 sceneLoaded는 구독하기 전에 이미 지나가,
        /// 이것이 없으면 이전 기록을 못 읽어 무엇을 해도 신기록이 됩니다.
        /// </summary>
        private static void CaptureBestForLoadedStage()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!SceneIds.IsStage(scene.buildIndex))
                    continue;

                BestBeforeRun = ProgressService.GetBestSeconds((SceneId)scene.buildIndex);
                return;
            }
        }

        /// <summary>
        /// 정적 수명이라 해제하지 않습니다. 두 번 붙지 않도록 먼저 떼고 붙습니다 — 버스가 비워진 뒤에도 안전합니다.
        /// </summary>
        private static void Subscribe()
        {
            Bus<UnitDeathEvent>.UnRegisterForAll(HandleUnitDeath);
            Bus<EnemyDeathEvent>.UnRegisterForAll(HandleEnemyDeath);
            Bus<BuildingDeathEvent>.UnRegisterForAll(HandleBuildingDeath);
            Bus<SquadDeathEvent>.UnRegisterForAll(HandleSquadDeath);
            Bus<SupplyEvent>.UnRegisterForAll(HandleSupply);

            Bus<UnitDeathEvent>.RegisterForAll(HandleUnitDeath);
            Bus<EnemyDeathEvent>.RegisterForAll(HandleEnemyDeath);
            Bus<BuildingDeathEvent>.RegisterForAll(HandleBuildingDeath);
            Bus<SquadDeathEvent>.RegisterForAll(HandleSquadDeath);
            Bus<SupplyEvent>.RegisterForAll(HandleSupply);
        }

        /// <summary>
        /// 전적을 0으로 되돌립니다. 새 판이 시작될 때 호출됩니다.
        /// </summary>
        public static void Reset()
        {
            EnemyUnitsKilled = 0;
            EnemyBuildingsDestroyed = 0;
            SquadsLost = 0;
            SuppliesGathered = 0;
            _startedAt = Time.time;
        }

        // 맵 씬은 additive로 얹히므로 그 씬이 올라오는 순간이 곧 새 판의 시작이다.
        // 인덱스를 직접 비교하지 않는다 — 스테이지 범위는 SceneId가 쥐고 있다
        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!SceneIds.IsStage(scene.buildIndex))
                return;

            Reset();

            // 이번 판을 시작하는 지금이 "이전 기록"을 읽을 수 있는 마지막 순간이다
            BestBeforeRun = ProgressService.GetBestSeconds((SceneId)scene.buildIndex);
            Subscribe(); // 씬 전환에서 버스가 비워졌을 수 있다
        }

        private static void HandleUnitDeath(UnitDeathEvent evt)
        {
            // UnitDeathEvent에는 Owner가 없어 유닛에서 직접 읽는다
            if (evt.Unit != null && evt.Unit.Owner == Owner.Enemy)
                EnemyUnitsKilled++;
        }

        /// <summary>
        /// 적 전투 유닛은 UnitDeathEvent 대신 이것을 발행합니다
        /// (AbstractCombatUnit.RaiseDeathEvent). 이걸 안 들으면 처치 수가 영영 0입니다.
        /// 한 유닛이 둘 다 쏘지는 않으므로 겹쳐 세지 않습니다.
        /// </summary>
        private static void HandleEnemyDeath(EnemyDeathEvent evt)
            => EnemyUnitsKilled++;

        private static void HandleBuildingDeath(BuildingDeathEvent evt)
        {
            if (evt.Owner == Owner.Enemy)
                EnemyBuildingsDestroyed++;
        }

        private static void HandleSquadDeath(SquadDeathEvent evt)
        {
            if (evt.Owner == Owner.Player)
                SquadsLost++;
        }

        /// <summary>
        /// 일꾼이 납품한 것만 셉니다. 시퀀스가 준 자원·농장 식량까지 세면 "모은 자원"이 아니라
        /// "받은 자원"이 되어, 한 판에 몇백씩 부풀어 오릅니다.
        /// </summary>
        private static void HandleSupply(SupplyEvent evt)
        {
            if (evt.Owner == Owner.Player && evt.Amount > 0 && evt.FromGathering)
                SuppliesGathered += evt.Amount;
        }
    }
}
