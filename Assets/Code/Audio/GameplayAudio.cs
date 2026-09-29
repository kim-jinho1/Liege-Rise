using Ami.BroAudio;
using Code.Bootstraps;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Building;
using Code.GameEvents.Enemy;
using Code.GameEvents.System;
using Code.GameEvents.Unit;
using UnityEngine;

namespace Code.Audio
{
    /// <summary>
    /// 게임플레이 씬의 배경음과 핵심 효과음을 Bus 이벤트에 연결해 재생하는 싱글톤입니다.
    /// SoundID는 BroAudio 라이브러리에서 인스펙터로 지정합니다(클립 미지정 시 자동 무시).
    /// 곡을 언제 갈아 끼울지는 BgmDirector가 정합니다.
    /// </summary>
    public class GameplayAudio : MonoSingleton<GameplayAudio>, IGameStartListener
    {
        [Header("BGM")]
        // 분위기마다 목록이 따로다. 비어 있는 칸은 조용히 건너뛰고 곡이 있는 분위기가 대신 돈다
        [SerializeField] private SoundID[] calmTracks = System.Array.Empty<SoundID>();    // 평화 — 판 초반
        [SerializeField] private SoundID[] tensionTracks = System.Array.Empty<SoundID>(); // 중간 — 압박이 시작된 뒤
        [SerializeField] private SoundID[] crisisTracks = System.Array.Empty<SoundID>();  // 위험 — 공세가 오는 동안

        [SerializeField] private bool playBgmOnStart = true;
        [SerializeField] private float bgmFadeIn = 1f;
        [SerializeField] private float bgmFadeOut = 2f;

        // 곡과 곡 사이 침묵 길이(초). 한 판이 20~40분이라 곡을 붙여 두면 귀가 먼저 지친다
        [SerializeField] private Vector2 bgmGapRange = new(40f, 60f);

        // 이만큼 지나면 평화에서 중간으로 넘어간다. 첫 공세보다 조금 앞서게 잡는다
        [SerializeField, Min(0f)] private float tensionAfterSeconds = 240f;

        // 공세가 순식간에 갈려도 위험 곡을 최소한 이만큼은 유지한다
        [SerializeField, Min(0f)] private float crisisMinSeconds = 30f;

        // 같은 효과음이 한 프레임에 겹치지 않게 두는 최소 간격(초)
        [SerializeField, Min(0f)] private float sfxInterval = 0.12f;

        // 판이 끝날 때 BGM을 걷는 시간. 승패 소리를 덮지 않게 평소 페이드보다 짧다
        [SerializeField, Min(0f)] private float bgmEndFadeOut = 0.6f;

        private readonly SoundThrottle _throttle = new();

        [Header("SFX")]
        [SerializeField] private SoundID unitSpawn; // 아군 유닛 생산
        [SerializeField] private SoundID buildingComplete; // 건물 완공
        [SerializeField] private SoundID buildingDestroyed; // 건물 파괴(아군/적 공용)
        [SerializeField] private SoundID enemyDeath; // 적 유닛 사망
        [SerializeField] private SoundID enemyDispatch; // 적 주둔지 출격
        [SerializeField] private SoundID victory; // 승리
        [SerializeField] private SoundID defeat; // 패배

        private BusSubscriptionScope _scope;
        private BgmDirector _bgm;

        private void OnEnable()
        {
            _scope = new BusSubscriptionScope();
            _scope.Subscribe<UnitSpawnEvent>(Owner.Player, OnUnitSpawn);
            _scope.Subscribe<BuildingSetupCompleteEvent>(Owner.Player, OnBuildingComplete);
            _scope.SubscribeAll<BuildingDeathEvent>(OnBuildingDeath);
            _scope.SubscribeAll<EnemyDeathEvent>(OnEnemyDeath);
            _scope.Subscribe<EnemyDispatchEvent>(Owner.Enemy, OnEnemyDispatch);
            _scope.Subscribe<AssaultsClearedEvent>(Owner.Enemy, OnAssaultsCleared);
            _scope.SubscribeAll<GameOverEvent>(OnGameOver);
        }

        private void OnDisable()
        {
            _scope?.Dispose();
        }

        /// <summary>
        /// BGM은 다른 매니저보다 늦지 않게 시작합니다.
        /// </summary>
        public int InitOrder => 0;

        /// <summary>
        /// 스테이지(=AudioListener를 가진 Main Camera)가 로드된 뒤 BGM을 재생합니다.
        /// _Core 단독 초기화 시점에 재생하면 리스너가 없어 경고가 발생하므로 OnGameStart로 미룹니다.
        /// </summary>
        public void OnGameStart()
        {
            if (!playBgmOnStart)
                return;

            _bgm = new BgmDirector(calmTracks, tensionTracks, crisisTracks,
                bgmFadeIn, bgmFadeOut, bgmGapRange, tensionAfterSeconds, crisisMinSeconds);

            if (_bgm.HasAnyTrack())
                _bgm.Run(destroyCancellationToken);
        }

        private void OnUnitSpawn(UnitSpawnEvent evt)
            => Play(unitSpawn);

        private void OnBuildingComplete(BuildingSetupCompleteEvent evt)
            => Play(buildingComplete);

        /// <summary>
        /// 이 이벤트는 AbstractBuilding.OnDestroy에서 나가므로 씬을 내릴 때도 건물 수만큼 옵니다.
        /// 실제로 무너진 것만 소리를 냅니다 — 안 그러면 메인 메뉴로 나갈 때 붕괴음이 우르르 들립니다.
        /// </summary>
        private void OnBuildingDeath(BuildingDeathEvent evt)
        {
            if (evt.Building != null && evt.Building.IsDead)
                Play(buildingDestroyed);
        }

        private void OnEnemyDeath(EnemyDeathEvent evt)
            => Play(enemyDeath);

        /// <summary>
        /// 공세가 출발했습니다. 효과음과 함께 배경음도 위험으로 올립니다.
        /// </summary>
        private void OnEnemyDispatch(EnemyDispatchEvent evt)
        {
            Play(enemyDispatch);
            if (_bgm != null)
                _bgm.EnterCrisis();
        }

        // 공세를 물리쳤다 — 위험 곡을 내리고 기준 분위기로 돌아간다
        private void OnAssaultsCleared(AssaultsClearedEvent evt)
        {
            if (_bgm != null)
                _bgm.ClearCrisis();
        }

        /// <summary>
        /// 게임 종료 시 배경음을 걷고 승자에 따라 승리/패배 효과음을 재생합니다.
        /// 배경음 위에 겹쳐 놓으면 둘 다 뭉개져 이겼다는 느낌이 안 납니다.
        /// </summary>
        private void OnGameOver(GameOverEvent evt)
        {
            _bgm?.Finish(bgmEndFadeOut); // 순수 C# 객체다 — Unity 오브젝트가 아니라 ?. 를 써도 된다
            Play(evt.WinnerOwner == Owner.Player ? victory : defeat);
        }

        /// <summary>
        /// 지정한 SoundID가 유효할 때만 재생합니다(클립 미지정 시 무시).
        /// 분대 하나가 스폰되면 UnitSpawnEvent가 멤버 수만큼 오고 전투에서는 적이 한꺼번에 죽습니다 —
        /// 그대로 재생하면 같은 소리가 한 프레임에 겹쳐 뭉개지므로 최소 간격을 둡니다.
        /// </summary>
        private void Play(SoundID id)
            => _throttle.Play(id, sfxInterval);
    }
}
