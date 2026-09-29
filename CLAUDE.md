# Medieval RTS — Claude Code 지시 파일

## 이 문서를 읽는 법

이 문서는 **규칙**과 **구조**만 담는다. 일정·할 일·작업 일지는 `TODO.md`에 있다.

- **[구현]** — 코드에 실제로 존재함. 파일 경로가 붙은 것은 확인된 것.
- **[기획]** — 의도만 있고 **코드에 없음**. 만들려면 신규 구현이다.
- 표기가 없는 서술은 기획 의도(세계관·목표)로, 구현 여부와 무관하다.

> **"CLAUDE.md에 있으니 이미 있다"고 단정하지 말 것.** 판단 전에 grep으로 확인한다.
> 이 문서는 **뒤처지는 쪽으로 틀린다** — "없다"는 서술을 근거로 신규 구현을 시작하기 전에
> 반드시 grep하고, 반대로 "있다"는 서술은 비교적 신뢰할 수 있다.
> 구조 절(씬·폴더·아키텍처)은 코드를 따라 변하므로 갱신을 요청받으면 실측해 다시 쓴다.

---

## 프로젝트 개요
- **장르**: 단판 스커미시 중세 RTS (2026-08-11에 스테이지·챕터 구조 폐기)
- **플랫폼**: PC (Windows) / Unity 2D 탑뷰 픽셀 아트
- **플레이 인원**: 1인 싱글플레이
- **플레이 타임**: 1판 약 20~40분
- **메인 테마**: 자원 채집 + 분대 단위 전술 전투
- **핵심 압박**: 자원 오브젝트가 고갈되기 전에 새 거점을 확보해야 한다.

---

## 세계관

중세 왕국. 플레이어는 본성(MainCastle)을 중심으로 영지를 확장한다.
적대 세력이 맵을 장악하고 자원 지대를 봉쇄 중이며, 플레이어는 일꾼(Pawn)으로 자원을 모아
분대를 편성하고 적 본진을 격파한다.

**맵 컨셉**: 중세 들판과 숲. 적 거점 주변에 자원 밀집.

---

## 씬 구성 [구현]

| 씬 | 빌드 인덱스 | 역할 |
|---|---|---|
| `MainMenuScene.unity` | 0 | 메인 메뉴 → 스테이지 선택 |
| `_Core.unity` | 1 | **상주 씬** — 매니저·UI. 스테이지 씬을 additive로 얹음 |
| `Stage1.unity` | 2 | 플레이 맵 — 절차 배치. `MapGenerator`가 있다 |
| `Stage2.unity` | 3 | 플레이 맵 — **손배치.** `MapGenerator`가 없다 |

`SceneId` enum(`Code/Core/SceneId.cs`)이 위 4개와 인덱스가 일치하고,
`SceneIds.LastStage`가 `Stage2`다. **이 대응이 깨지면 씬 로드가 통째로 죽는다.**

> **`Stage3.unity`는 2026-09-06에 삭제됐다** (`635ef727`). 부스 일정상 만들지 않기로 하면서
> 씬·`SceneId` 항목이 함께 빠졌다. `Stage_3.asset`은 **잠긴 장으로 메뉴에만 남는다** —
> `IsLocked: 1`이라 선택이 안 되므로 `Scene: 4`가 없는 씬을 가리켜도 로드되지 않는다.
>
> **`EditorBuildSettings`는 정리됐다 (2026-09-08, `ebbdf0d2`).** 등록 씬은 위 표의 넷뿐이고
> 인덱스 4 항목은 없다. `Assets/GameModules/Map/BakeData/Stage3 BakedData.asset`만 물어 줄 씬이
> 없어 고아로 남았다 — 빌드에는 영향이 없다.

> **`Skirmish.unity`는 없다.** 2026-08 말에 스테이지 씬으로 갈라졌다 —
> 이전 판의 "빌드 인덱스 2 = Skirmish" 서술은 폐기됐다.

---

## 맵 구조

**씬마다 다르다. 먼저 어느 씬인지부터 본다.**

| 씬 | 배치 방식 | `MapGenerator` |
|---|---|---|
| `Stage1` | **손배치** (2026-09-07 전환) — 프리팹 64개: 나무 32 · 돌 19 · 고블린 소굴 1 · 고블린 망루 2 · 본거지 1 · 해적 망루 1 · 본성 1 | **0개** |
| `Stage2` | **손배치** — 프리팹 307개가 씬에 직접 놓여 있다 | **0개. 제거됐다** |

> **두 씬 모두 손배치다.** `MapGenerator`를 쓰는 씬은 이제 없다. 아래 절차적 배치 절은 코드가
> 남아 있어 적어 둔 것이고, 새 맵을 만들 때 다시 켤 수 있다.
> `Stage1`은 `StageSO.GuidePoints`(안내 손이 짚는 자리 6개)와 `WaveRoute`(공세 길목 4개)를 함께 든다.

### Stage2 — 손배치 [구현 — 2026-09-06 실측]

절차적 배치를 버리고 손으로 찍는다. 아래 절차적 배치 절과 `MapAnchor`·`MapLayoutSO` 서술은
**이 씬에 해당하지 않는다.**

- **지형**: 타일맵 8장. `PathBaker.walkableMaps`는 `Flat`(level 0) · `Ramp`(level 0, 램프) ·
  `Plateau`(level 1) · `Bride`(level 0) 넷, `blockingMaps`는 `Cliff` 하나.
  **`Plateau`가 고도 1이라 대지 조각은 램프 없이 못 올라간다 — 의도된 것이다.**
- **배치물**: `WoodNode` 136 · `StoneNode` 140 · `GoblinTower` 11 · `GoblinCamp` 5 ·
  `GoblinHut` 4 · `GoblinCore` 1 · `MainCastle` 1 · `Cottage` 1.
- **지형이 세 띠로 갈라진다** — 남(y14~67) · 중(y60~111) · 북(y93~121). 띠 사이는 절벽이 아니라
  **타일이 없는 2~4칸 빈 줄**이고, 다리 4개가 좌·우 한 쌍씩 그 줄을 건넌다
  (남↔중 `x41` · `x81`, 중↔북 `x48` · `x86`). 이 셋이 곧 구역 개방의 단위다.

> **`EnemyBuilding` 컨테이너가 `(43.4211, 57.80712)`에 있다.** 자식들의 로컬 좌표가 그 값을
> 상쇄해 월드 좌표가 정수로 떨어진다. **컨테이너를 원점으로 옮기면 적 건물 21개가 전부 어긋난다.**

### 절차적 배치 [구현 — **현재 쓰는 씬 없음**] — `Assets/Code/MapGeneration/` 15파일 1,486줄

```
MapAnchor(추상) ─┬ BaseSlotAnchor    # 기지 자리. PairKey
                 └ SpawnZoneAnchor   # 원형 구역. Composition · GroupKey · PairKey
BaseCompositionSO   # 본부 프리팹 + 위성(SpawnEntry[])
ZoneCompositionSO   # 구역 랜덤 표 (TotalRange · MinDistinctKinds · ClusterRadius · 가중치)
ZoneGroupSO         # 후보 앵커를 묶어 그중 몇 곳만 쓰는 규칙 (Key · ActiveCount · Primary/Secondary)
MapLayoutSO         # 기지 구성 2개 + 그룹 목록 + 전역 간격
ZoneGroupSelector   # 이번 판에 어느 앵커를 어느 구성으로 채울지 확정
ClusterBounds       # 배치 후보를 한 점 주위로 묶는 원형 제한
PlacementNotes      # 미달·완화를 모아 앵커당 한 줄로 알림
BaseSlotPairing · AnchorPopulator · MapGenerator · PlacementSampler · SpawnEntry
```

규칙을 코드가 아니라 **구조**가 보장한다:
- **"반대편"은 `PairKey`가 같은 슬롯 정확히 2개**다. 기하 계산을 하지 않으므로
  지형이 완벽히 대칭이 아니어도 규칙이 깨지지 않는다.
- **"한 자원으로만 안 채워짐"은 `Roll()`이 총량 배분 전에 서로 다른 종류를
  `MinDistinctKinds`개 먼저 확보**해서 보장한다. `TotalRange.x`가 그보다 작으면 보장 불가.
- **"구역이 한쪽에만 몰리지 않음"은 `ZoneGroupSelector`가 앵커가 아니라 짝 단위로 뽑아서**
  보장한다. 아래 "구역 그룹" 참고.

**구역 그룹 [구현 — 2026-08-30 `76835dd7`, 짝 보장은 2026-09-01 `3a672b15`]**

앵커에 `GroupKey`를 달면 그 앵커는 후보가 되고, `ZoneGroupSO`가 그중 `ActiveCount`개만 골라
앞쪽 `PrimaryCount`개에 `Primary`(적 진지 포함), 나머지에 `Secondary`(자원만)를 준다.
**뽑히지 않은 앵커는 조용히 빈다 — 그게 정상 동작이다.**

- **뽑기 단위는 앵커가 아니라 "단위"다.** 단위는 같은 `PairKey`를 단 앵커 2개, 또는 `PairKey`가
  없는 앵커 1개. 짝은 함께 뽑히고 함께 빠지며 **같은 구성을 받는다** — 여기서 앵커 단위로 자르면
  짝 하나는 적 진지, 하나는 빈 자원 구역이 되어 대칭이 도로 깨진다.
- `ActiveCount`를 넘기는 단위는 **멈추지 않고 건너뛴다.** 뒤의 더 작은 단위가 마지막 칸을 채운다.
- `PairKey`가 2개로 맞지 않으면 경고만 하고 **그대로 쓴다.** `BaseSlotPairing`이 깨진 짝을 버리는
  것은 반대편을 특정 못 하면 진영 배정이 불가능해서고, 구역에는 그 제약이 없다.
- **앵커 순서는 이름순이어야 한다.** `MapGenerator.CollectZonesInStableOrder`가 정렬해 주므로
  같은 시드가 같은 지도를 낸다. 딕셔너리를 순회해 단위를 모으면 그 결정론이 깨진다.
  **이름이 겹쳐도 깨진다** — 정렬 키가 이름뿐이라 동점 순서가 씬 로드에 맡겨진다.
  두 부모에 같은 이름을 두지 말 것(삭제된 `Stage3`가 실제로 그랬다).

**클러스터 [구현 — 2026-09-01 `44546d45`]**

`ZoneOption.clustered`를 체크하면 그 항목은 구역 전체가 아니라 **앵커 중심에서
`ClusterRadius` 안**에서만 자리를 뽑는다. 적 건물을 진지처럼 뭉치는 용도다.

- **중심이 앵커 자신이라 짝 앵커가 점대칭이면 양 진영 캠프도 대칭**이 된다.
- **배치 순서는 "뭉치는 항목 먼저 → `minSpacing` 내림차순"이다**(`OrderForPlacement`).
  이게 없으면 자원이 먼저 깔려 진지 자리를 잡아먹는다. 예전엔 캠프의 `minSpacing`이 가장 커서
  우연히 먼저 놓였을 뿐이고, 뭉치려고 간격을 좁히는 순간 그 보호가 사라진다.
- 반경 안에 자리가 없으면 **구역 전체로 풀어서라도 놓는다.** 진지가 조용히 비는 쪽이 더 나쁘다.
  푼 횟수는 `AnchorPopulator.RelaxedCount`로 나가 `PlacementNotes`가 한 줄로 알린다.
- **`AnchorPopulator`는 `Debug`를 부르지 않는다.** 워커는 숫자만 보고하고 문구는 호출자가 정한다.

`MapGenerator`는 `IGameStartListener`, **`InitOrder = -100`** — `Bootstrap`이 모든 매니저보다
먼저 호출하므로 이후 초기화는 전부 기지가 존재하는 상태에서 돈다.

> **생성기는 손으로 배치한 오브젝트를 전혀 못 본다.** 간격 판정은 자기가 놓은 것만 기억하고,
> 자원 노드는 건물이 아니라 `BuildingOccupancy`에도 오르지 않는다. 씬의 손배치물을 남기면 겹친다.

**[구현] 앵커 실측 (2026-09-13)**

| 씬 | `MapGenerator` | `BaseSlotAnchor` | `SpawnZoneAnchor` | 그룹 사용 |
|---|---|---|---|---|
| `Stage1` | **0 — 2026-09-07 손배치로 전환** | — | — | — |
| `Stage2` | **0 — 손배치로 전환** | — | — | — |

> **두 씬 모두 손배치물이 씬에 직접 놓여 있다.** 생성기를 다시 켜는 씬에는 손배치물을 남기면 안 된다 —
> 생성기는 자기가 놓은 것만 기억하므로 겹친다.

> **베이크 데이터는 씬마다 따로다.** `Assets/GameModules/Map/BakeData/`에
> `Stage1 BakedData` · `Stage2 BakedData`가 있고 각 씬의 `PathBaker`가 자기 것을 문다.
> **한 씬을 베이크해도 나머지가 안 덮인다.** 고아였던 `Stage3 BakedData`와 `MapLayout.asset`은
> 9/9(`3b55b662`)에 삭제됐다.
>
> **`MapLayoutSO`는 `MapLayout_Stage1.asset` 한 장뿐이고 지금은 아무 씬도 물지 않는다.**

> **지형을 고치면 `PathBaker`의 `Bake Map (Full)`을 반드시 다시 돌린다.** 절차 배치는
> 베이크 데이터를 후보로 읽으므로, 재베이크 전엔 예전 지형 기준으로 뿌린다.
> **손배치 씬에서도 마찬가지다** — 베이크가 씬보다 오래됐는지는 셀 수로 잡을 수 있다.

**상태 [2026-09-13]**: **가동 중인 씬이 없다.** `Stage1`도 9/7에 손배치로 넘어갔다.
SO 에셋 14개(`GameModules/Map/Generation/`)와 `MapLayout_Stage1.asset`이 남아 있고
`Stage1 BakedData` · `Stage2 BakedData`는 손배치 씬의 길찾기가 여전히 쓴다(고아 `Stage3 BakedData`는 9/9에 삭제).
코드 1,486줄은 삭제 대상이 아니라 **다음 맵을 위한 보관**이다 — 결정은 사용자 몫.

---

## 프로젝트 구조 [구현] — 497파일 42,600줄 (2026-09-13 실측)

```
Assets/
├── Code/                    # 최상위 25폴더
│   ├── Animations/          # UnitAnimator, ParameterSO
│   ├── Audio/               # BroAudio 연동 9파일. GameplayAudio(SFX) · BgmDirector(분위기별 BGM)
│   │                        #   · Combat/Command/Economy/MenuAudio · SoundThrottle
│   ├── Bootstraps/          # Bootstrap(진입점), CoreSceneLoader
│   ├── Buildings/           # 53파일 4,496줄
│   │   ├── Base/            # AbstractBuilding, UnitProductionQueue
│   │   ├── Construction/    # ConstructionDummy, ConstructionProgress, GhostPlacement,
│   │   │                    #   PawnConstructionRunner, WorkerlessConstructionRunner
│   │   ├── Data/            # BuildingSO 계층 10파일
│   │   ├── Interfaces/      # IBuilding, IBuildingConstructor, IReinforcePoint …
│   │   ├── Logic/           # CoreBuilding(HeadquartersMarker 포함) · DefenseBuilding(+TowerGarrison)
│   │   │                    #   · EconomyBuilding(Farm 양 3파일 포함) · EnemyBuilding
│   │   │                    #   · ProductionBuilding(SquadDeployer · SquadOrderRefusal) · Shared
│   │   └── Utils/           # BuildingFootprint, GhostPlacementPreviewManager
│   ├── Commandable/         # AbstractCommandable, CommandableHealth(체력 협력 객체),
│   │                        #   CommandableVisuals, CommandArrayAudit, SpawnScatter + Interfaces/
│   ├── Commands/            # Base/ · Interfaces/ · Types/(Combat·Movement·Production·System)
│   ├── Core/                # Owner, MonoSingleton<T>, SceneId, SceneLoader, DataManager
│   │                        #   + Bus/ · Input/ · FactionPalette/
│   ├── Economics/           # ResourceSO, ResourceCostSO, ResourceDelivery, IResourceReceiver
│   ├── Effects/             # SelectionOutlineEffect, DeathShakeEffect(건물 붕괴), ReinforceRangeView
│   ├── EnemyAI/             # 7파일 1,348줄 — EnemyAIManager, AssaultScheduler, AssaultForce,
│   │                        #   AssaultRegistry, EnemyGarrison, EnemyLineBrain(갈래 지휘관 두뇌)
│   ├── Environments/        # ResourceObject(+ResourceVisuals), GatherPointSet, ResourceObjectManager
│   ├── FogSystem/           # FogVisibilityManager, FogVisionReadback(GPU 읽기), FogReveal, PlaceHolder
│   ├── GameEvents/          # 이벤트 struct 31개 — 아래 "게임 이벤트 목록"이 전부
│   ├── Managers/            # HeadquartersBuildingManager, PlayerHeadquartersFinder, UnitSeparationManager
│   ├── MapGeneration/       # 절차적 배치 15파일 — ★ 지금은 쓰는 씬이 없다(Stage1도 손배치로 전환)
│   ├── Military/            # 62파일 5,989줄 — Combat/(+FSM 10) · Commanders/ 13 · Commands/ 5
│   │                        #   · Data · Reinforcement · Squads/ 14(+Formations) · Stats
│   ├── Navigation/          # PathAgent, PathFollower, PathVisualizer, DestinationMarker
│   │                        #   + Baking/(PathBaker · NavGraphBaker · TerrainLevelResolver)
│   │                        #   · Graph/ · Pathfinding/ · Editor/
│   ├── Objectives/          # ObjectiveManager, ObjectiveSO, ConditionTracker + Conditions/ 22파일
│   ├── Players/             # 25파일 2,972줄 — SelectionController, CommandExecutor, CameraController
│   │                        #   (+CameraBoundsClamper · CameraConfig), BattleLineDrawer,
│   │                        #   CommanderSelectHotkeys, RightClickCommandResolver, BoothCheats,
│   │                        #   UserSupplies, UserPopulation, UnitRegistry … + Cursors/ 4파일
│   ├── Progression/         # ProgressService, RunStats(판당 전적), 세이브 + Editor/
│   ├── Rendering/           # YSortByPosition, SortingCopier, SortingOverride,
│   │                        #   RenderLayers(★ 런타임 생성물의 레이어는 여기만 안다)
│   ├── Sequences/           # 32파일 3,122줄 — SequenceManager, SequenceSO/StepSO,
│   │                        #   SequenceHighlighter(월드) · SequenceUIHighlighter(HUD) · SequenceDimOverlay
│   │                        #   + Effects/ 7(FocusCamera · GiveUnits · GiveBuilding · GiveResources
│   │                        #     · PlayTimeline · OpenMapRegion · StartAssaults)
│   │                        #   + Guide/ 10(안내 손 — GuideCursorPlayer · GuideCursorView · GuideGesture …)
│   ├── Stages/              # StageSO(맵 경계·구역·안내 지점·공세 경로), StageDatabaseSO,
│   │                        #   MapRegion, MapRegionManager + Editor/ ★ 맵 경계의 유일한 출처
│   ├── UI/                  # 57파일 6,148줄 — Alerts · Components · MainMenu · Pause · Results
│   │                        #   · Selection(CommandPanel · CommanderPortraitStrip …) · Sequence · WorldSpace
│   │                        #   + TextFormat(표기 창구) · 미니맵 3파일 · ResourceUI · RuntimeUI
│   └── Units/               # 55파일 4,053줄 — Base · BT/(Actions 16 · Conditions · Events)
│                            #   · Combat/(+Calculator) · CombatUnits · Interfaces · Pawns · Util
├── GameModules/             # SO 에셋·프리팹 14폴더 — AnimationParameters, BTVariables,
│                            #   Buildings, Effect, Enemy, FactionPalette, Map, Military,
│                            #   Sequence, Stages, SupplyResources, Tiles, UI, Units
├── Scenes/GameScene/        # MainMenuScene · _Core · Stage1 · Stage2
├── Resources/               # 인스펙터 칸이 없는 쪽이 읽는 에셋
│                            #   (StageDatabase · CursorSet · UIText · NoticeText · SquadMarkerStyle)
├── Settings/                # URP 파이프라인·렌더러 에셋 + InputSettings/(Controls.inputactions · 생성된 Controls.cs)
└── _Graphics/ · _Audio/ · BroAudio/ · GondrLib/ · Plugins/(DOTween) · TextMesh Pro/
```

**큰 폴더** (줄 수): `Players` 2,696(루트만) · `Military/Squads` 1,881 · `UI/Selection` 1,802 ·
`Units/BT/Actions` 1,632 · `Military/Commanders` 1,534 · `Sequences/Guide` 1,530 · `MapGeneration` 1,486 ·
`EnemyAI` 1,348 · `Objectives/Conditions` 1,305 · `UI/Components` 1,239 · `Military/Combat/FSM` 1,195.

**300줄 규약**: 위반 0건. 딱 300줄이 `Squad.cs` · `Stages/Editor/StageThumbnailCapture.cs` 둘 —
**`Squad`에 한 줄이라도 더 붙이면 위반이다.** 새 기능은 협력 객체로 뗀다(아래 "분대 시스템").
그 다음이 `AbstractCommandable` 296 · `RuntimeUI` 295 · `EnemyLineBrain` 295.

**존재하지 않는 것** (2026-08-19 grep 0건 재확인):
`CastleBuildingManager` · `ConstructionManager` · `ProcessingBuilding*` ·
`WallBuildingSO` · `EnemyCore` · `OverlapRestrictionSO` · `RearRaidManager`

> **`Code/UI/Editor/` 폴더는 없다.** 에디터 전용 코드는 `Navigation/Editor`(237줄) ·
> `Progression/Editor`(247줄) · `Stages/Editor`(300줄) 셋뿐이다.

---

## 반드시 지켜야 할 점
- OOP 기반 설계
- 계획부터 말하고 무조건 승인 받은 후에 작업 진행
- 최적화를 고려한 코드 작성
- 커맨드 패턴과 BT를 고려할 것
- 한 클래스 또는 스크립트에서 코드가 300줄을 넘지 말아야한다.
- 커밋을 할때는 커밋 기록을 보면서 개발 로드를 파악 할 수 있게 한번에 올리는게 아닌
기능별로 묶어서 따로따로 커밋해야한다.
- 커밋을 할때는 제목과 본문 모두 영어를 써야하며 한글을 써서는 안된다.

---

## 아키텍처 핵심 패턴

### Event Bus [구현]
모든 모듈 간 통신은 `Bus<T>`를 통해서만 한다. 직접 참조 최소화.
```csharp
// 발행
Bus<SupplyEvent>.Raise(Owner.Player, new SupplyEvent(Owner.Player, amount, resourceSO));

// 구독 (Awake에서)
Bus<GameOverEvent>.OnEvent[Owner.Unowned] += HandleGameOver;

// 해제 (OnDestroy에서 반드시)
Bus<GameOverEvent>.OnEvent[Owner.Unowned] -= HandleGameOver;

// 전체 Owner 구독
Bus<SupplyEvent>.RegisterForAll(HandleSupplyEvent);
```

### Owner 시스템 [구현]
`Owner` enum: `Player, Enemy, Unowned, Invalid`
- 플레이어 유닛/건물 → `Owner.Player`
- 적 건물/유닛 → `Owner.Enemy`
- 글로벌/시스템 이벤트 → `Owner.Unowned`

### ScriptableObject 계층 [구현]
```
BuildingSO                          # Buildings/Data/ — 실존 9종이 전부
├── PopulationBuildingSO            # MainCastle, House
├── ProductionBuildingSO            # 단일 유닛 생산 건물
├── SquadRecruitmentBuildingSO      # 분대 생산 건물 (병영)
├── SpawnerBuildingSO               # 적 유닛 스폰 건물
├── AutoTowerBuildingSO             # 자동 방어 타워
├── MannedTowerBuildingSO           # 유닛 탑승형 타워
├── FarmBuildingSO                  # 식량 생산
└── OutpostBuildingSO               # 전방 거점

ResourceSO                          # Economics/ — 에셋은 Wood · Stone · Food 3종
ResourceCostSO (Costs[])            # Economics/
AbstractUnitSO                      # Units/Base/
├── CombatUnitSO
└── PawnSO (CarryCapacity)

AbstractCommandableSO               # Commandable/ — Prefab 보유
└── SquadSO                         # Military/Data/ — 조부모 UnlockableSO가 Cost 보유
```

### 유닛 계층 [구현]
```
AbstractCommandable
├── AbstractUnit (BT, 이동)
│   ├── AbstractCombatUnit
│   │   ├── MeleeUnit / RangeUnit / BuffUnit / RepairUnit
│   └── Pawn (일꾼 — 건설·채집·자원 운반)
└── Squad (분대 — Military/Squads/Squad.cs)
```

### 분대 시스템 [구현] — `Assets/Code/Military/` 62파일 5,989줄 (2026-09-13 실측)

2026-09-07에 **전투를 "공세선" 방식으로 재설계**했다. 분대 하나하나에 명령하는 대신 지휘관이 선을 긋고
분대가 그 선에 서서 미는 구조다. 아래 폴더 표가 그 결과이고, 옛 "방진(Phalanx)"과 `SquadCommanderSlot`은
**코드에 없다**(방진 철거 9/4, 슬롯은 `CommanderRoster`로 대체).

```
Military/
├── Combat/FSM/     # SquadStateMachine + Idle·Moving·Advancing(걸으며 싸움)·Approaching·Engaging·Mounted
├── Combat/         # SquadSensor, SquadArmor(방향 피해 판정), ArmorZone
├── Commanders/     # 13파일 — Commander, CommanderRoster(명부), CommanderLineExecutor(선→분대 명령),
│                   #   BattleLine(struct) · BattleLineView(막대+화살표) · BattleLineMeshes · BattleFront
│                   #   · BattleLineProbe · LinePushTracker(적 보면 밀기) · LineSeatLayout · LineTerrain(길 따라 휘기)
│                   #   · DetourPlanner · CommanderAura(패시브)
├── Commands/       # Appoint · AssignToCommander · Reinforce · MountTower · DismountTower (5개)
├── Data/           # SquadSO(+Flag·Doctrine), CommanderSO(: CombatUnitSO, SquadCapacity), CommanderPassiveSO,
│                   #   SquadMarkerStyleSO(Resources), SquadDoctrine(Frontal·Flank)
├── Reinforcement/  # SquadReinforcement
├── Squads/         # 14파일 — Squad(300줄 ★한계), ISquad, SquadRoster, SquadOrders, SquadFacing, SquadFormation,
│                   #   SquadTowerMount, SquadVisibility, SquadFlagView(깃발+번호 배지) · SquadArcView · SquadOrderArrowView
│                   #   · ArrowMeshBuilder · ArrowStripBuilder · MarkerMaterial
│   └── Formations/ # FormationSO(Line · Wedge · Circle), FormationSelector
└── Stats/          # StatSheet, StatSO, StatModifier, StatOverride, StatEffect, ModifierOperator
```

**`Squad`는 조율자다.** 실제 일은 협력 객체가 나눠 갖는다 — `SquadRoster`(명부·배치) · `SquadOrders`(명령) ·
`SquadFacing` · `SquadFormation` · `SquadSensor` · `SquadArmor` · `SquadReinforcement` · `SquadTowerMount` ·
`SquadVisibility` · `SquadStateMachine`. **`Squad.cs`는 딱 300줄이다** — 새 기능은 `Squad`에 메서드를 붙이지 말고
협력 객체를 하나 더 만들어 위임한다.

핵심 규칙 네 가지:
- **분대 루트는 논리 객체다.** 콜라이더가 없고 멤버 위치만 추종한다.
- **분대는 멤버를 클릭해 선택한다.** 루트를 직접 클릭하는 경로는 없다 —
  `AbstractCombatUnit.SelectionProxy`가 그 접기를 담당한다. **지휘관은 자기 자신이 프록시다**(분대가 없으므로).
- **방향 피해는 `normal`로 판정한다.** `ApplyDamage(damage, position, normal)`의 `normal`이
  피격자→공격자 방향이다. `position`은 피격자 자신의 좌표라 방향 판정에 쓰면 안 된다.
- **분대 루트의 체력은 스스로 갱신되지 않는다.** 콜라이더가 없어 `TakeDamage`를 받지 못하므로
  `SquadRoster.HealthChanged`(멤버 체력 합) → `Squad.HandleRosterHealthChanged` →
  `RaiseHealthUpdated`가 **유일한 갱신 경로**다. 이 배선이 끊기면 정보 패널 체력바가 굳는다.

**지휘관 [구현 — 2026-09-08 재설계]**: `Commander : AbstractCombatUnit`은 **병영에서 생산**한다 —
`SquadBarracks.prefab`의 `AvailableCommands` 넷째 칸 `Buildings/Command/CommanderBuild.asset`이 분대 생산과 같은
큐(`SquadRecruitmentBuilding`, `IsKnown`이 `CommanderSO`도 받음)로 뽑는다.
`Military/Commands/Appoint *.asset` 셋(`AppointCommanderCommandSO`)은 **어느 프리팹도 물지 않는 잔재**다.
`CommanderRoster`가 거느린 분대 명부를 소유하고, `Ordinal`(진영 안 번호 1~)과 `Color`가 깃발·선·초상에 같이 쓰인다.
분대 상한은 `UserPopulation`이 **본성 + 지휘관별 `CommanderSO.SquadCapacity`**로 센다.

- **배속은 "같이 담고 긋기"다.** 지휘관과 분대를 한 상자에 담고 우클릭 드래그하면 `BattleLineDrawer.BindSelectedSquads`가
  `Roster.Assign`한 뒤 선을 넘긴다. 초상(`CommanderPortraitStrip`)을 눌러도 선택 분대가 배속된다(`Assign To Commander`, Slot -1).
- **선은 누른 자리→놓은 자리 그대로다.** 지형에 맞춰 옮기지 않는다. 기본 미는 방향은 지휘관에게서 멀어지는 쪽,
  `Tab`이 뒤집는다(끄는 중에도, 그은 뒤에도). **끄는 동안 가장자리 스크롤은 잠긴다**(`CameraController.EdgePanSuppressed`) —
  화면 끝으로 끌면 카메라가 밀려 끝점이 달아났었다.
- **선은 할 일이 끝나면 스스로 사라진다.** `CommanderLineExecutor.Tick`이 전원 Idle이고 보이는 적이 없으면 `Clear()`.
  적이 보이면 `LinePushTracker`가 놀고 있는 분대를 12칸 앞(또는 보이는 적 쪽)으로 민다 — **매복 부대가 지나가는 적을
  보고 스스로 튀어나가는 것은 이 동작이다.** 튜토리얼 의도와 어긋나는지는 미결.
- **적 갈래도 같은 구조다.** `EnemyLineBrain`이 갈래 지휘관에게 선을 그어 주고, 선을 명령으로 바꾸는 일은
  플레이어와 같은 `CommanderLineExecutor`가 한다. 성향은 분대 다수결(`SquadDoctrine`).
- 깃발: `SquadFlagView`는 `SquadSO.Flag`가 비면 **아무것도 안 붙인다.** 배지(지휘관 색·번호)가 붙으면 늘 보인다.
  적 분대 SO 4장에도 9/13에 깃발을 넣었다.

**증원 [구현 — 2026-08-24 `aefcc8bc`]**: `SquadReinforcement`(순수 C#)를 `Squad`가 소유하고 매 프레임 `Tick`한다.
`ReinforcePointRegistry.FindCovering`으로 커버 거점을 찾고, **거점 밖이면 진행도를 유지한 채 대기**한다.
지불은 자원 + 폰 1기(`PawnConscription`)를 함께 소모하고, 성공하면 `SquadRoster.ReinforceOne()`이 돈다.

> **함정**: `OutpostBuilding.cs`는 **스텁이 아니다** — `IReinforcePoint` 구현체로 `Start()`에서
> `ReinforcePointRegistry`에 자기를 등록한다. 다만 **아직 "자동" 증원은 아니다.**
> `Begin()`을 부르는 곳은 `ReinforceSquadCommandSO.cs:48` 하나뿐이라 플레이어가 "결원 보충" 버튼을 눌러야 시작한다.

### 구 분대 시스템 — 2026-08-21 철거 완료

`Assets/Code/Squads/` 23파일과 관련 릴레이·핫키·커맨드 SO를 전부 삭제했다.
**`Code.Squads` 네임스페이스는 코드 전체에 0건이고 분대 코드는 `Code.Military` 하나뿐이다.**
분대·지휘관도 `AbstractCommandable`이라 범용 우클릭 경로(`CommandExecutor`)가 그대로 처리한다.

> **딸려 죽은 기능 2개**: 분대 스킬(Q)·자세 순환(E) 단축키. 신 분대에는 스킬·자세 개념이 없다.
> `InputReader`의 `OnSkillKeyPressed` · `OnPostureKeyPressed`는 **구독자 없이 남아 있다** —
> 스킬을 만들 때 그대로 쓰면 된다.
---

## 코딩 컨벤션

### 네이밍
- 클래스: `PascalCase`
- 인터페이스: `I` 접두사 (`IBuilding`, `IDamageable`)
- ScriptableObject: `~SO` 접미사 (`BuildingSO`, `SquadSO`)
- 이벤트 struct: `~Event` 접미사 (`GameOverEvent`, `SupplyEvent`)
- private · protected 필드: `_camelCase`. `static readonly` · `const`는 `PascalCase`
- 이벤트 필드(`Action` · `event`): `On~` 접두 (`OnDied`, `OnHealthChanged`). 받는 쪽 메서드는 `Handle~`
- async 메서드: `~Async` 접미. `async void`는 fire-and-forget이 의도일 때만
- `Debug.Log`: `$"[{nameof(ClassName)}] …"` 접두 + 컨텍스트 오브젝트(`this`) 전달

### 직렬화 — "밖에서 읽느냐"로 가른다
- **다른 클래스가 읽는 값**: `[field: SerializeField] public Type Name { get; private set; }`.
  SO 데이터·데이터 struct·외부에 노출되는 배선 전부. `{ get; set; }`은 스폰 직후 대입처럼 이유가 있을 때만.
- **그 클래스 안에서만 쓰는 배선**(컴포넌트 참조 · UI 요소 · 조율 수치): `[SerializeField] private Type camelCase;`
- 위 둘을 겹쳐 쓰지 않는다 — `[SerializeField] private x` 옆에 `public X => x`를 따로 달지 말고 `[field:]` 한 줄로.
- **예외는 `Units/BT/Actions`의 `[SerializeReference] public BlackboardVariable<T>` 하나뿐** — Unity Behavior가 강제하는 형태다.
- `[Serializable]` 데이터 struct도 예외가 아니다. 다만 **필드→프로퍼티 전환은 직렬화 키를
  `name` → `<Name>k__BackingField`로 바꾼다.** 에셋·씬에 이미 값이 있으면 `[field: FormerlySerializedAs("name")]`을
  다리로 두고 에셋을 한 번 저장시킨 뒤 뗀다. 베이크 데이터는 재베이크로 충분하다.
- `[Header]`는 쓴다. `[Tooltip]`은 쓰지 않는다 — 설명은 필드 줄 끝 주석이 맡는다.

### 파일 하나에 타입 하나
- 파일명 = 그 파일의 public 타입명. 작은 enum · struct라도 본 클래스 앞에 끼워 넣지 않고 제 파일을 준다.
- **예외 둘**: `~ConditionSO` + `~Tracker` 짝(조건 하나가 SO와 러너를 함께 가짐), 제네릭 오버로드(`IUIElement<T>`).
- `partial`로 300줄을 피하지 않는다 — 넘치면 협력 객체를 떼어낸다.
- 모든 파일은 `Code.<폴더 경로>` 네임스페이스 안에 있다. 네임스페이스 없는 파일을 만들지 않는다.
- `using`은 `System*`을 먼저, 나머지는 알파벳순, 별칭(`using Debug = …`)은 맨 끝 — Rider 기본 정렬 그대로다.
  네임스페이스 바깥에 두고 그룹 사이에 빈 줄을 넣지 않는다.

### ★ 네임스페이스가 Unity 타입을 가린다 — 코드 쓰기 전에 볼 것

C#은 **바깥 `using`보다 자기 네임스페이스의 형제 네임스페이스를 먼저** 찾는다.
그래서 `Code.X.*` 안에서는 아래 이름들이 **Unity/System 타입이 아니라 네임스페이스로 잡혀
컴파일이 깨진다.** 코드는 멀쩡해 보이고 Unity 콘솔에서만 터진다.

| 가리는 네임스페이스 | 가려지는 것 | 영향 범위 |
|---|---|---|
| `Code.UI.Selection` | `UnityEditor.Selection` | `Code.UI.*` 전부 |
| `Code.Core.Input` | `UnityEngine.Input` | `Code.Core.*` 전부 |
| `Code.GameEvents.System` · `Code.Commands.Types.System` | `System` 루트 | 각 부모 아래 전부 |
| `Code.Navigation.Editor` · `Code.Progression.Editor` | `UnityEditor.Editor` | 각 부모 아래 전부 |

**해결은 전체 이름 하나뿐이다** — `UnityEditor.Selection.activeGameObject`.
특히 `Assets/Code/UI/Editor/`의 에디터 도구는 `Selection`을 거의 항상 쓰므로 **기본이 전체 이름**이다.

> **이 표에 `Cursor`가 없는 것은 일부러 피했기 때문이다.** 커서 코드는 `Code.Players.Cursors`
> (**복수형**)에 있다. 단수 `Code.Players.Cursor`로 지었다면 `Code.Players.*` 전체에서
> `Cursor`가 `UnityEngine.Cursor`가 아니라 네임스페이스로 잡혀 `Cursor.SetCursor`가 통째로 죽는다.
> **새 네임스페이스를 지을 때 Unity 정적 클래스와 같은 이름을 쓰지 말 것.**
(2026-08-22·23 이틀 연속으로 이 실수를 했다.)

### 주석 — 적게 쓴다

**주석은 최소한으로 쓴다.** 코드를 읽으면 알 수 있는 것은 적지 않는다.
남길 가치가 있는 것은 **"왜"** 하나뿐이다 — 함정, 되돌리면 안 되는 이유, 겉보기와 다른 동작.

**XML 문서 주석은 `<summary>` 하나뿐이고, 그 안은 순수 텍스트다.**
`<param>` · `<returns>` · `<remarks>` · `<example>`도, `<see>` · `<c>` · `<b>` · `<para>` 같은 인라인 태그도 쓰지 않는다.
식별자는 그냥 본문에 적는다 — `Awake에서 캐싱합니다`. 인자 이름만 되풀이하는 `<param>`은 잡음이다.

**summary는 "무엇 + 왜" 형식이다.** 첫 문장은 무엇을 하는지, 둘째 문장부터는 왜 그런지 · 함정 · 겉보기와 다른 동작.
무엇만으로 충분하면 한 문장으로 끝낸다. 네 문장을 넘기면 summary가 아니라 설계 문서다 — 줄인다.
어투는 합니다체로 통일한다.

```csharp
// ✅ 무엇 한 문장. 인자는 이름이 이미 설명한다
/// <summary>
/// 일꾼 1기를 건설 현장에 배치합니다.
/// </summary>
public void AssignToConstruction(ConstructionDummy dummy) { ... }

// ✅ 무엇 + 왜. 둘째 문장이 없으면 이 코드를 고치는 사람이 함정을 밟는다
/// <summary>
/// 피해를 반영합니다. 여기서는 통지하지 않습니다 — 파생 클래스가 피해량을 손본 뒤
/// 소유자가 사망 처리와 함께 한 번에 알려야 순서가 어긋나지 않습니다.
/// </summary>
public void Apply(int damage) { ... }

// ❌ 태그로 같은 말을 세 번
/// <summary>bool 파라미터를 설정합니다.</summary>
/// <param name="parameter">설정할 파라미터.</param>
/// <param name="value">설정할 bool 값.</param>
public void SetParameter(ParameterSO parameter, bool value) { ... }
```

- **`<summary>`가 의무인 곳**: 모든 타입, public/protected 메서드.
- **생략하는 곳**: 이름이 곧 설명인 자동 프로퍼티, `override` 메서드. 단 override가 **베이스와 역할이 달라지면**
  그 "왜"를 적는다(`SquadMoveCommandSO`가 방진 중 이동을 막는 것처럼).
- private 멤버는 이름으로 안 드러나는 의도가 있을 때만 붙인다.
- 필드 주석은 **줄 끝 한 줄**로. `[Tooltip]`은 쓰지 않는다.
- 본문 주석은 "왜"만. 단계를 나열하는 주석(`// 1. 찾는다 // 2. 더한다`)은 지운다.
- `#region`을 쓰지 않는다 — 접고 싶어지면 클래스가 큰 것이다.

### 인게임 문구는 세 자리로 나눈다

**"누가 바꾸는가"로 가른다.**

| 종류 | 어디에 |
|---|---|
| 계산이 섞인 표기(시간·비율) | `Code/UI/TextFormat.cs` 정적 헬퍼 |
| 여러 곳이 쓰는 같은 말 | `Resources/UIText.asset`(화면 표기) · `Resources/NoticeText.asset`(인게임 알림) |
| 한 컴포넌트만 쓰는 문구 | 그 컴포넌트의 `[SerializeField] string` |

- **`TextFormat`이 유일한 창구다.** SO를 읽는 것은 그 안쪽뿐이라 호출부는 SO의 존재를 모른다.
  **새 공용 문구도 호출부가 아니라 `TextFormat`을 거치게 한다.**
- **`Resources`인 이유는 읽는 쪽에 인스펙터 칸이 없어서다** — 정적 창구와 커맨드 SO가 쓴다.
  `SquadMarkerStyleSO`·`CursorSetSO`와 같은 이유이고, **에셋이 없으면 코드 기본값으로 돌고
  경고 한 줄만 낸다**(`CursorSet.asset`이 없을 때 커서가 통째로 죽던 전례를 되풀이하지 않는다).
- **`Get("stage.locked")` 식 키 딕셔너리는 쓰지 않는다** — 오타가 런타임에야 드러난다.
  이 규모에서는 필드로 두고 컴파일러에게 맡기는 쪽이 이긴다.
- **Unity Localization은 도입하지 않았다.** 다만 이 규칙은 그리로 가는 길을 막지 않는다 —
  둘째 칸이 통째로 StringTable이 된다.
- 순수 C# 클래스(`SquadState` 등)는 `[SerializeField]`가 불가하다. 그 문구가 플레이어에게
  보이는지부터 확인한다 — 에디터 기즈모로만 가는 것은 그대로 둔다.

> **하드코딩이 나쁜 이유는 "문자열이라서"가 아니라 같은 말이 두 벌이 되기 때문이다.**
> `"잠김"·"완료"·"도전 가능"`이 `StageDetailUI`와 `StageButtonUI`에 각각 박혀 있었고
> 판정도 두 벌이었다 — 지금은 `StageStatuses.Of()` 하나뿐이다.

---

## 최적화 규칙

### FindObjectOfType 금지
```csharp
// ❌ 절대 사용 금지
FindObjectOfType<HeadquartersBuildingManager>();

// ✅ 항상 이것으로
FindFirstObjectByType<HeadquartersBuildingManager>();
FindObjectsByType<AbstractBuilding>();
FindObjectsByType<AbstractBuilding>(FindObjectsInactive.Include);  // 비활성 포함이 필요할 때
```

> `FindObjectsSortMode`는 Unity 6000.5에서 deprecated다 (경고 CS0618).
> 인자 없는 오버로드를 쓴다. 정렬 순서는 `None`에서도 보장되지 않았으므로 잃는 것은 없다.

### Coroutine 대신 Awaitable 사용
Unity 6+ `Awaitable` API를 우선 사용한다. 단, AI 루프처럼 조건 기반 반복이 필요한 경우는 Coroutine 허용.
```csharp
// ❌ 지양
yield return new WaitForSeconds(3f);

// ✅ 우선
await Awaitable.WaitForSecondsAsync(3f);
await Awaitable.NextFrameAsync();
```

### 컴포넌트 캐싱
`GetComponent`는 `Awake()`에서만 호출하고 반드시 캐싱한다.

### Physics
- 모든 물리는 2D (`Collider2D`, `Rigidbody2D`, `Physics2D`)
- `OverlapCircleNonAlloc` / `OverlapBoxNonAlloc` **사용 금지** (deprecated)
- 대신 `ContactFilter2D` + `Physics2D.OverlapCircle` 사용 (GC 방지 배열 유지)
```csharp
// ❌ 절대 사용 금지
Physics2D.OverlapCircleNonAlloc(position, radius, results, layerMask);

// ✅ 항상 이것으로
private readonly Collider2D[] _hitBuffer = new Collider2D[10];
private ContactFilter2D _filter;
void Awake() { _filter.SetLayerMask(layerMask); _filter.useTriggers = true; }
Physics2D.OverlapCircle(position, radius, _filter, _hitBuffer);
```

### Unity 6000.5 deprecation
`GetInstanceID()` 대신 `GetHashCode()`를 쓴다 (에러가 아닌 경고).

---

## 자원 시스템

### 실제 자원 [구현]
`ResourceSO` 에셋은 **Wood · Stone · Food 3종이 전부**다.
금광·철광·가공 자원(Ingot, Planks)은 **에셋도 코드도 없다.**

| 자원 | 출처 오브젝트 | 비고 |
|---|---|---|
| Wood | `WoodNode` | 씬 배치 0개 — `MapGenerator`가 런타임에 뿌린다 |
| Stone | `StoneNode` | 씬 배치 0개 — 위와 같다 |
| Food | `FarmBuilding` | 채집 오브젝트 없음 |

### 자원 오브젝트 (`ResourceObject`) [구현]
유한 자원. Pawn이 직접 이동해 채집하고, 고갈되면 소진 처리된다.
- 채집 → 본성 귀환 입금 (`DepositSuppliesAction`)
- 고갈 시 가까운 다른 자원 자동 탐색 (`MoveToAvailableSupplyAction`)
- 채집 자리 예약은 `GatherPointSet`이 맡는다. 도착 순서가 아니라 **폰에서 가장 가까운 빈 자리**를
  주고, 통행 가능한 자리를 우선한다.
- 시야 밖에서는 **겉모습만 멈춘다.** 통행·예약 같은 게임 로직은 즉시 반영하고, 애니메이션과
  고갈 스프라이트는 다시 보일 때 한꺼번에 적용한다(`_depleteVisualPending`).
- **주의**: `Pawn_BT`의 채집 분기가 `Amount == 0` 기준이라 **한 번 캘 때마다 본성을 왕복**한다.
  `PawnSO.CarryCapacity`가 이 루프에 관여하지 않는다.

**★ `Pawn_BT` 블랙보드의 자원 변수는 두 개다 (2026-08-26 분리).** 합치면 안 된다.

| 변수 | 뜻 | 쓰는 노드 |
|---|---|---|
| `ResourceFilter` | **캐러 갈** 자원 종류 | `MoveToAvailableSupplyAction` 하나뿐 |
| `SupplySO` | **지금 든** 자원 종류 | `GatherSuppliesAction` · `DepositSuppliesAction`(2) · `MoveToTargetGameObjectAction`(3) |

`Pawn.Gather()`는 `ResourceFilter`만 바꾼다. `SupplySO`를 명령으로 덮어쓰면 운반 애니메이션과
입금되는 자원이 통째로 바뀐다. 짐을 진 채 다른 자원을 찍으면 `Command`를 `ReturnSupplies`로
보내 **납품을 먼저** 시키고, 끝나면 BT가 `Gather`로 되돌린다.

### 가공 시스템 [기획 — 미구현]
Sawmill / Smelter / Stonemason / Blacksmith와 `ProcessingBuildingSO` 레시피 구조는
**코드에 존재하지 않는다** (grep 0건). 1티어·2티어 구분도 마찬가지다.

---

## 건물 시스템 [구현]

`Assets/Code/Buildings/Logic/` 실존 목록:

| 분류 | 클래스 |
|---|---|
| 본부 | `HeadquartersBuilding` · `CoreStorage` · `PopulationBuilding` |
| 방어 | `AutoTowerBuilding` · `MannedTowerBuilding` |
| 경제 | `FarmBuilding` · `OutpostBuilding` · `OutpostRegistry` |
| 생산 | `ProductionBuilding` · `RecruitmentBuilding` · `SquadRecruitmentBuilding` |
| 적 | `SpawnerBuilding` · `SpawnUnitInfo` |
| 공용 | `PawnStationHost` · `PawnIntakeOrchestrator` |

**`HeadquartersBuilding`이 붙은 프리팹** — `MainCastle` · `MiddleCastle` · `GoblinCore` · `FishHut`.
이 4개만 승패 판정에 관여한다.

### 건설 흐름 [구현]
`GhostPlacement → ConstructionDummy → 완성 프리팹 스폰`
- 건설 실행은 `PawnConstructionRunner`(일꾼 필요) 또는 `WorkerlessConstructionRunner`(무인)
- **`ConstructionManager`는 없다** — 유휴 Pawn 자동 배정 기능은 구현되지 않았다.

### 인구 시스템 [구현] — 2026-08-17

`Players/UserPopulation.cs` (`MonoSingleton`)가 집계하고 `PopulationChangedEvent`로 알린다.
**폰 축과 분대 축이 완전히 분리돼 있다** — 폰은 분대의 재료라 합치면 안 된다.

| 축 | 수 세는 경로 | 한도 공급처 |
|---|---|---|
| 폰 | `UnitSpawnEvent`/`UnitDeathEvent`에서 **`is Pawn` 필터** | `IPawnCapacityProvider.PawnCapacity` (본부 건물) |
| 분대 | `SquadSpawnEvent`/`SquadDeathEvent` | `SquadRecruitmentBuildingSO.MaxSquads` (병영) |

- **`is Pawn` 필터가 빠지면 분대원 5기가 폰으로 잡힌다.** 분대원도 `AbstractUnit`이다.
- `UnitDeathEvent`에는 `Owner` 필드가 없어 유닛에서 직접 읽는다.
- 표시는 `UI/PopulationUI.cs` → `RatioSlotUI` 2개. `_Core`에 배선됨.
- 분대 한도 검사는 `SquadRecruitmentBuilding.cs`가 `UserPopulation.Instance`로 한다.

> **함정**: `MonoSingleton.Instance`는 **자동 생성하지 않는다** — 씬에서 찾고 없으면 `null`이다.
> `UserPopulation`이 씬에 없으면 아무것도 집계되지 않고 이벤트가 한 번도 발행되지 않아
> UI가 `0 / 0`에 멈춘다. 2026-08-17에 실제로 이 상태였다. **`_Core`에 있어야 한다.**

> **`PopulationBuildingSO.PopulationIncrease`는 없는 필드다.** 실제 필드는
> `PawnSO` · `MaxPawns` · `RespawnCooldown`이고, `PopulationBuilding`은 **`Cottage.prefab`에만**
> 붙어 있다. 폰은 플레이어가 뽑지 않는다 — Cottage가 `MaxPawns`만큼 즉시 스폰하고
> 사망 시 `RespawnCooldown` 후 재생성한다. **버튼으로 생산하는 것은 분대뿐이다.**
> `MainCastle`·`MiddleCastle`의 `AvailableCommands`에 남아 있는 `PawnBuild.asset`은 잔재다.

---

## 시퀀스·튜토리얼 [구현 — 2026-09-13 실측] — `Assets/Code/Sequences/` 32파일 + `Objectives/` 26파일

한 스테이지의 이야기는 `SequenceSO.Steps[]`(`SequenceStepSO`)이고, 스테이지 씬의 `SequenceManager`가 순서대로 돈다.
`StageSO.SequenceOverride`가 있으면 그것으로 갈아 끼운다(`Stage_Booth`가 이 경로).

```
LoadStepAsync(i): Effects 순차 실행(대기형은 await) → 제스처 없으면 강조 표시 → 대사 → (대사 끝) 안내 손 재생 + 조건 활성화
조건 완료 → ObjectiveCompletedEvent → Advance(): 강조·손 정리 → i+1
```

- **스텝 에셋 한 장이 든 것**: `Title`(로그·에디터용) · `Description`(할 일 HUD에 뜨는 글) · `Condition`(완료 조건 SO) ·
  `DialogLines[]` · `GameObjectHint`(월드 강조, enum 23종) · `UIElementHint`(HUD 강조, enum 15종) · `Gestures[]`(안내 손) · `Effects[]`.
- **조건은 22종**(`Objectives/Conditions/`, `~ConditionSO` + `~Tracker` 짝이 한 파일). 트래커는 `Subscribe`에서 이미 벌어진 일을
  **이어받아야 한다** — 앞 스텝에서 나온 공세, 이미 그어진 선처럼. `AssaultRepelled`·`AssaultApproached`가 표준.
- **효과는 7종**(`Sequences/Effects/`): FocusCamera · GiveUnits · GiveBuilding · GiveResources · PlayTimeline · OpenMapRegion · StartAssaults.
  `StartAssaults.FirstWaveDelay`는 표 전체를 당기는 값이다(0이면 즉시).
- **대사는 Space 또는 대사창 클릭으로만 넘어간다**(2026-09-13). 화면 아무 데나 누르는 왼쪽 클릭에 묶여 있어서
  유닛을 담고 명령하는 클릭에 한 줄짜리 경고가 읽기도 전에 닫혔다 — 부스에서 "옵니다"를 아무도 못 봤다.
- **HUD 강조는 `GameObjectHint == None`인 스텝만 화면을 어둡게 덮는다**(`SequenceDimOverlay`, order 90).
  채집·건설처럼 월드도 눌러야 하는 스텝에서 덮으면 방해다. 덮개는 클릭을 안 막는다.
- **안내 손(`Sequences/Guide/`)**은 자기 오버레이 캔버스(order 5000)에 포인터·마우스·키캡을 그려 제스처를 시연한다.
  `Arrows`·`WASD` 라벨은 키캡 넷을 십자로 펼친다. 손이 짚는 월드 자리는 `StageSO.GuidePoints`(키 = `GameObjectHighlightType`)에
  **지도 위에 찍어 둔다** — 계산으로 잡으면 안내 손과 조건이 다른 곳을 본다(`da1c396f`).
- **`Stage_1_Tutorial` 27스텝**: 01 카메라 → 02~05 HUD 설명 → 06~09 일꾼·채집 → 10~14 건설 → 15~18 분대·지휘관·배속
  → 19 해적 망루 → 20~21 두 번째 부대·성 앞 → 22 숲에 선 긋기 → **22b 자리 잡기**(`BattleLineSettled`) → 23 "옵니다"(공세 출발)
  → 24 역습(`AssaultApproached`: 반경 24 또는 교전) → 25 소굴(`AssaultRepelled`) → 26 되찾다.
  **부스판 `Stage_1_Booth`는 10스텝**이고 07 매복 진입에서 공세를 30초 뒤에 낸다 — 두 판이 `Cond_LineAmbush`를 공유한다.
- `SequenceManager`는 스텝 진입마다 `[SequenceManager] 스텝 진입 #n` 로그를 남긴다. **흐름이 이상하면 콘솔부터 본다.**
- `SkipCurrentStep()`이 탈출구다 — 부스 치트(`BoothCheats`, Ctrl+Shift+F1 자원·F2 강제 클리어·F3 단계 건너뛰기·F4 세이브 초기화, 두 번 눌러야 실행)가 부른다.

---

## 맵 구역 [구현 — 2026-09-06]

**"구역이 열린다" = 카메라가 볼 수 있는 반경이 넓어진다는 뜻이다.** 유닛 이동 잠금은 없다 —
지형(다리)이 이미 막는다. `Stage2`가 이 구조로 3막을 돈다.

```
StageSO.MapRegions[]   # 막마다 열리는 사각형. 비면 처음부터 맵 전체가 열린다
MapRegionManager       # _Core 상주. 지금 열린 경계를 쥐고 MapRegionOpenedEvent 발행
OpenMapRegionEffectSO  # 시퀀스 스텝의 진입 효과로 다음 구역을 연다
```

- **`MapMin`/`MapMax`는 여전히 맵 전체를 뜻한다.** 이름을 바꾸면 `<MapMin>k__BackingField`
  직렬화 키가 깨져 스테이지 에셋 세 장의 경계가 통째로 초기화된다. `MapRegions`는 그 안을
  막 단위로 쪼갠 것이고, `OnValidate`가 합집합이 어긋나면 경고한다.
- **구역은 누적이다.** `OpenThrough(i)`가 `0..i`의 합집합을 잡으므로 두 번 불려도, 순서가
  어긋나도 결과가 같다.
- **첫 구역도 이벤트로 알린다.** `CameraController.ResolveBounds`가 `_boundsResolved`로 한 번만
  읽어서, 매니저가 서기 전에 `Update`가 한 프레임 먼저 돌면 **폴백(맵 전체)을 물고 굳는다.**
- **경계를 읽는 곳은 런타임 두 곳뿐이다** — `CameraController.ResolveBounds` ·
  `MinimapCameraProvider.FitToMap`. 둘 다 매니저 → `StageSO` 순으로 폴백하므로
  **`_Core`에 매니저가 없어도 예전대로 돈다.**
- **미니맵 창 비율과 `Camera.aspect`는 반드시 같이 세팅한다.** `windowFitter`가 배선돼 있으면
  둘을 맵 비율로 맞춰 letterbox 낭비가 0이 되고, 비어 있으면 큰 축에 맞추는 예전 동작이다.
  하나만 바꾸면 미니맵이 늘어난다. 클릭·마커·뷰포트 테두리는 전부
  `WorldToViewportPoint × markerRoot.rect`를 지나 **비율이 바뀌어도 고칠 상수가 없다.**
- 안개도 따라온다 — `FogVisibilityManager`가 미니맵 UV를 매 렌더마다 다시 계산한다.

---

## 승리 / 패배 조건 [구현]

`Managers/HeadquartersBuildingManager.cs` — Owner별 본거지 생존 수를 세고,
**한쪽이 0이 되면 반대편을 승자로 `GameOverEvent` 발행.**

**세는 대상은 타입이 아니라 `HeadquartersMarker`가 붙었는지다** (2026-09-06).
`SpawnerBuilding`과 `HeadquartersBuilding`이 둘 다 `AbstractBuilding` 상속이라 한 오브젝트에
못 얹히므로, 등록만 떼어낸 마커를 씬 인스턴스에 얹어 **적 진지도 본거지로 승격**한다.
`HeadquartersBuilding`에는 `[RequireComponent(typeof(HeadquartersMarker))]`가 걸려 있다.

```csharp
public void Unregister(Owner owner)
{
    if (!_aliveCount.TryGetValue(owner, out int count))
        return;                       // ← 한 번도 Register되지 않은 Owner는 여기서 탈출
    _aliveCount[owner] = --count;
    if (count == 0) { /* GameOverEvent */ }
}
```

> **함정**: 맵에 특정 Owner의 마커가 하나도 없으면 그 Owner는 `Register`되지
> 않으므로 **영원히 패배하지 않는다.** 적 본거지를 최소 1개 배치해야 승리가 성립한다.

> **해제는 `OnDestroy`가 아니라 사망 시점이다.** `AbstractBuilding.Die`는 붕괴 연출이 끝난 뒤에야
> `Destroy`하고 `BuildingDeathEvent`도 그때 나가는데, 그 이벤트는 **씬 언로드 때도 나간다** —
> 그걸 들으면 판을 나갈 때마다 승패가 오발한다. 마커는 `AbstractCommandable.OnDied`
> (`InitDeath`에서 1회)만 듣고 `OnDestroy`에서는 구독만 끊는다.

`EnemyCore` 클래스와 `EnemyCoreDestroyedEvent`는 **존재하지 않는다.**
유사 판정이 `Objectives/`의 `AllEnemyHeadquartersDestroyedConditionSO`에 별도로 있어
판정 경로가 이중화돼 있다 — 정리 대상.

---

## 적 AI 시스템 [구현 — 상세 미검증]

`EnemyAI/` = `EnemyLineBrain`(295) · `EnemyAIManager`(269) · `AssaultForce`(235) · `AssaultScheduler`(177) ·
`EnemyGarrison`(154) · `EnemyAIConfigSO`(150) · `AssaultRegistry`(68) —
**7파일 1,348줄** (2026-09-13 실측).

**갈래마다 지휘관이 있다 (2026-09-07).** `AssaultScheduler.LaunchColumn`이 갈래를 만들 때 `EnemyAIConfigSO.Commander`를
같이 스폰하고 `AssaultForce.Brain = new EnemyLineBrain(commander, units)`를 단다. 두뇌는 **선을 그어 주는 것이 전부**이고
선을 분대 명령으로 바꾸는 일은 플레이어와 같은 `CommanderLineExecutor`가 한다. 판단은 셋 — 옆구리를 맞았으면 예비 갈래를
부르거나 돌아서고(`ReserveRange`), 우회 성향(`SquadDoctrine.Flank` 다수결)이면 근처 플레이어 분대 옆구리로(`FlankRange`),
아니면 `StageSO.WaveRoute` 길목을 차례로 지나 본부 앞에 선을 긋는다. **길목을 안 찍으면 늘 최단 경로로 온다** —
매복 자리를 만들려고 있는 값이다. 지휘관이 없는 갈래(전초 붕괴 시 `EnemyGarrison.DispatchAll`)는 두뇌 없이
`IssueOrders`로 곧장 본부를 친다.

`AssaultRegistry`는 `ClearedCount`(이번 판에 물리친 횟수)를 세고 `EnemyAIManager.Awake`가 `ResetRun()`으로 판마다 비운다 —
뒤늦게 구독한 조건(`AssaultApproached`)이 "이미 왔다 갔는지"를 이걸로 안다.

**압박의 크기는 `EnemyAIConfig.asset`의 표가 전부 정한다.** 병력이 쌓여서 나가는 경로는 없다 —
`AssaultWave`에 적은 시각이 되면 `AssaultScheduler`가 **그 자리에서 새로 생성해** 내보낸다.
`EnemyGarrison`은 출발지이자 수비대 명부이고, 전초 진지가 하나라도 살아 있으면 그쪽만 쓴다
(`IsLastResortOrigin`인 본거지는 전초가 전부 무너져야 출발지가 된다).
나간 병력은 `AssaultForce`가 넘겨받아 플레이어 본부를 순차로 노리고,
`AssaultRegistry`를 통해 미니맵(`AssaultMinimapUI`)에 표시된다.

> **`CycleType`은 없다** (2026-09-05 확인). 시간표 방식으로 재작성되며 통째로 사라졌다 —
> `CycleMultipliers` · `cycleTime`도 코드 전체에 0건이다.

**공세 개시 시점 [구현 — 2026-09-06]**: `OnGameStart`가 `StageDatabaseSO.Current.DeferAssaults`를
보고, 켜져 있으면 시계를 돌리지 않고 시퀀스가 `BeginAssaults(firstWaveDelay)`를 부를 때까지 기다린다.
`StartAssaultsEffectSO`가 그 호출자다. **표의 시각은 그 호출을 원점으로 잡고**,
`FirstWaveDelay`를 주면 첫 웨이브가 그만큼 뒤에 오도록 표 전체를 당긴다(줄 사이 간격은 유지).
`BeginAssaults`는 두 번 불려도 시계를 하나만 돌린다.

**공세를 물리친 시점**은 `AssaultRegistry`가 0이 되는 전이다(`AssaultsClearedEvent`).
웨이브 하나가 여러 갈래로 나뉘므로 **부대 하나가 죽는 것으로는 판정이 안 된다.**

> **`RearRaidManager`는 2026-08-19 시점에 삭제되고 `EnemyGarrison`으로 교체됐다.**
> 코드 전체에 `RearRaid` 문자열이 0건이다 — `RearRaidEvent` · `RearRaidWarningEvent` ·
> `RaidPoint_SW`/`_NW` · `firstRaidDelay`도 함께 사라졌다.

**존재하지 않는 것**: `AppliesPenaltyOnDestruction`(적 강화 건물) · `EnemyProductionSlowEvent`.
"강화 건물 파괴 시 생산 주기 +30%"는 기획일 뿐 구현되지 않았다.

---

## UI 구성

**[기획] 4구역**: 좌상 자원 / 좌하 미니맵 / 우상 인구·유닛 / 우하 커맨드 패널 + 정보창.

**[구현] 현재 상태 (2026-09-13 실측)** — `Code/UI/` **57파일 6,148줄**, 300줄 위반 0건(`RuntimeUI` 295가 최대).
대부분 `_Core.unity`에 배치되나 `UI/Sequence/SequenceDialogUI`는 `Stage1`·`Stage2` 두 씬과
`DialogCanvas.prefab`에 있다. `Assets/Code/UI/Editor/` 폴더는 **존재하지 않는다.**
2026-08-16~17에 사용자가 전면 리디자인했고 코드를 맞췄다(커밋 12건).

배선 확인된 것 — 전부 `_Core`에 있다:
`ResourceUI`(자원 HUD) · `PopulationUI`(인구) · `ConstructionPanelController`(건설) ·
`RuntimeUI`(선택 라우팅) · `GeneralInfoUI`(공용 정보 패널) · 미니맵 2파일

**HUD 골격 [구현 — 2026-08-26]** — 하단은 **미니맵 · 슬롯 · 커맨드** 세 칸이다.
`SelectionVar`가 좌우 스트레치(`Left 448 · Right 398 · Bottom 24 · Height 180`)로
**미니맵과 커맨드 사이의 빈 구간 그 자체**가 되고, 선택 패널·다중 선택 바는 앵커·피벗
`(0.5, 0)`만으로 그 안에 가운데 정렬된다. 값은 전부 계산에서 나온다:

```
Left  = 24(마진) + 400(미니맵) + 24(간격) = 448
Right = 24(마진) + 350(커맨드) + 24(간격) = 398
슬롯 폭 @1920 = 1920 − 448 − 398 = 1074
```

- 선택 패널 `1074` = 패딩 24 + Common 400 + 24 + Divider 2 + 24 + Detail 576 + 24
  (Detail 576 = Members 312 + 24 + Reinforce 240)
- 다중 선택 바 `1032` = UnitFrame 96 × 10 + spacing 8 × 9
- 커맨드 `350 × 174` — `GridLayoutGroup`의 `cell 83×84 · spacing 6 · 4열`이 내는 값 그대로
- 자원 바 `600` — 내용 524 + 패딩 60 = 최소 584에 여유 16
- HUD 네 모서리 마진은 전부 `24`

> **`SelectionVar`를 다시 폭 0으로 되돌리지 말 것.** 그러면 자식이 각자의 피벗대로 흩어져
> 다중 선택 바가 화면 밖으로 나가고 선택 패널이 미니맵을 파고든다. 슬롯 폭이 바뀌면
> 위 네 숫자를 전부 다시 계산해야 한다.

**타이포는 5단계다** — `40 / 28 / 22 / 18 / 15`. 제목·결과 화면 / 선택 대상 이름 /
수치·버튼 라벨 / 상태 줄·체력 / 보조 설명·진행률. 새 텍스트는 이 다섯 중 하나를 쓴다.

**General Info Container** — 아이콘·이름·체력·체력바를 담은 **씬에 하나뿐인** 공용 패널.
`GeneralInfoUI`가 담당하고 **`RuntimeUI`가 단독으로 Bind/Unbind**한다.
`RuntimeUI`가 `selectionRoot`의 활성화도 단독 소유한다 — 이게 없어서 한동안 아무 패널도 안 보였다.

**건설 패널** — `buildMenuCommands`(`BuildMenu/Base.asset`)의 커맨드 10종을 슬롯 13개에 나열한다.
`IsUnlockedByProgress()`가 false인 것만 건너뛴다(현재 `Monastery`·`Archery` 2개만 해금 게이트).

**커맨드 패널 [구현]** — `CommandPanel`이 선택 대상의 `AvailableCommands`를 **`Slot` 번호대로
고정 칸에 꽂는다.** 커맨드 타입으로 분기하지 않으므로 새 커맨드는 에셋만 만들면 나타난다.
빈 칸도 자리를 지켜 대상이 바뀌어도 버튼 위치가 흔들리지 않는다.

- `Slot < 0` = 패널 제외(우클릭 전용). `Squad Move`·`Squad Attack`이 `-1`이다.
- 여러 대상 선택 시 **전원이 공통으로 가진 커맨드만** 뜬다 (`SharedCommandSet`).
- **단축키는 `Q E R F / Z X C V`** (2026-09-13) — 4열×2행 그리드라 Slot 번호가 곧 자리다(`CommandHotkeys`).
  WASD는 카메라, 1~9는 지휘관이라 그 둘레에서 왼손이 안 움직이고 닿는 여덟 키를 골랐다.
  액션 에셋을 거치지 않고 키보드를 직접 읽는다(Q·E가 구독자 없는 `ShortcutKey` 바인딩에 잡혀 있다).
- 툴팁은 커서가 아니라 **패널 상단 기준**으로 위쪽에 펼쳐진다.
- 건물 쪽(`BuildingSelectedUI`)은 아직 타입별 특수 경로다 — 범용화 대상.

**분대 패널 [2026-08-26 재작성]** — `SquadSelectedUI`가 인원·대형·보충 상태를 표시한다.
커맨드 버튼은 `CommandPanel`이 전담하고, 이름·아이콘·체력은 `GeneralInfoUI`가 전담한다.
**분대원 칸은 씬에 고정으로 두지 않고 `MemberFrame` 프리팹에서 런타임 생성**한다 —
지휘관이 맨 앞 한 칸을 차지하고, 죽으면 칸이 사라지며, 칸마다 체력바가 붙는다(`SquadMemberSlotUI`).

**자원·인구 증감 표시** — `FloatingDeltaText`가 슬롯의 수치 텍스트를 **런타임에 복제**해
폰트·크기·정렬을 물려받으므로 프리팹을 고칠 필요가 없다. `ResourceSlotUI`·`RatioSlotUI` 공용이다.

> **크기 함정**: 물려받는 것에 **크기는 빠진다.** 복제본이 `ignoreLayout`을 켜는 순간
> 레이아웃 그룹이 손을 놓고 RectTransform이 **프리팹의 직렬화 값(0×0)으로 돌아간다.**
> 폭 0이면 TMP가 글자마다 줄을 바꿔 `+3`이 세로로 쌓인다. 그래서 원본의 앵커·피벗·
> `sizeDelta`·`anchoredPosition`을 **명시적으로 복사**하고, 그 전에
> `LayoutRebuilder.ForceRebuildLayoutImmediate`를 한 번 돌린다 —
> 슬롯이 첫 값에서 `SetActive(true)` 하므로 **원본조차 아직 0×0**인 프레임이 있다.

> **함정**: `Show()`는 즉시 재생하지 않는다. 한 프레임 모았다가 합산해 한 번만 띄운다 —
> Cottage가 폰을 한 루프에 다 만들어 이벤트가 3번 따로 오기 때문이다. 그대로 재생하면
> `+1`이 세 번 겹쳐 `+1`로 보인다. 소유한 MonoBehaviour가 `Tick()`을 매 프레임 불러야 한다.

> **우클릭 함정**: `CommandExecutor`는 `RequireClickToActivate`를 보지 않고 `AvailableCommands`
> 순서대로 첫 `CanHandle` true에서 멈춘다. 막는 방법이 둘 있다:
> - `SetFormationCommandSO`처럼 `CanHandle`을 **항상 false**로 (UI가 `Handle`을 직접 호출)
> - `AppointCommanderCommandSO`처럼 **`context.MouseButton != MouseButton.Right`** 검사
>   (우클릭 경로는 `Right`, UI 버튼 경로는 `Left`라 버튼만 통과한다 — 이쪽을 권장)

---

## 호버 커서 [구현 — 2026-08-27]

`Assets/Code/Players/Cursors/` 4파일. 마우스 아래 상황에 따라 커서를 7종으로 갈아 끼운다.

```
CursorKind              # Default · Select · Build · GatherWood · GatherStone · Attack · Invalid
CursorSetSO             # 종류 → (Texture2D, 핫스팟). Resources/CursorSet.asset
CursorContextResolver   # 우선순위 판정 (순수 C#)
CursorDriver            # 바뀐 프레임에만 Cursor.SetCursor
```

**판정은 순서다.** 자원 위에 적이 겹쳐 서면 둘 다 참이라, 먼저 걸리는 쪽을 정해 두지 않으면
커서가 프레임마다 깜빡인다. 위에서부터 첫 번째로 걸리는 것을 쓴다:

1. 포인터가 UI 위 → 기본
2. 건설 고스트 + 배치 유효 → 건설
3. 건설 고스트 + 배치 무효 → 불가
4. 지금 우클릭하면 실행될 커맨드가 있다 → 그 커맨드의 `HoverCursor`
5. 안개를 통과한 `ISelectable`이 마우스 아래 → 선택
6. 그 외 → 기본

**커서는 `CommandExecutor`와 같은 답을 본다.** 우클릭 해석부를 `RightClickCommandResolver`
(`Code/Players/`)로 떼어내 양쪽이 공유한다. **규칙을 커서 쪽에 따로 적지 말 것** — 갈라지는 순간
커서가 채집이라 말하고 실제로는 이동이 나간다. 2·3단계도 마찬가지로
`GhostPlacementPreviewManager.IsPlacementValid`를 읽을 뿐 새로 계산하지 않는다.

**커서를 밝히는 자리가 둘이고, 대상이 커맨드를 이긴다.**

| 자리 | 뜻 | 예 |
|---|---|---|
| `BaseCommandSO.HoverCursor` | 무엇을 하려는가 | `Attack command` → Attack |
| `ResourceSO.HoverCursor` | 무엇에 하려는가 | `Wood` → GatherWood · `Stone` → GatherStone |

우클릭 채집은 `Gather action` **에셋 하나**가 나무와 돌을 다 처리하므로 커맨드만으로는 도끼와
곡괭이를 못 가른다. 그래서 `ResolveFromTarget`이 이기고, 커맨드 쪽 값은 폴백으로 남는다.

> **안개 속 대상은 커서에도 반응하면 안 된다.** 시야를 잃어도 콜라이더는 살아 있어서,
> 거르지 않으면 **안개 위에 커서만 문질러 적 위치를 알아낼 수 있다.** 4·5단계 모두
> `FogVisibility.IsVisibleNow`를 통과한 대상만 본다.

> **씬 배선이 없다.** `CommandExecutor.Awake`가 고스트 매니저와 같은 방식으로
> `AddComponent<CursorDriver>()` 하고, 에셋은 `Resources.Load<CursorSetSO>("CursorSet")`로 읽는다.
> `CommandExecutor`가 `_Core`가 아니라 맵 씬 3곳에 각각 들어 있어 인스펙터 배선이면 3번 반복해야 한다.

> **`CursorSet.asset`이 없으면 드라이버가 통째로 무동작이다.** 커서가 안 바뀌면 코드가 아니라
> `Assets/Resources/CursorSet.asset`이 있는지부터 본다.

**아트**: `cursors/Cursor_01`(화살표·기본) · `Cursor_02`(손·선택) · `Cursor_03`(금지·불가) +
`Tool_01`(망치·건설) · `Tool_02`(도끼·나무) · `Tool_03`(검·전투) · `Tool_04`(곡괭이·돌).
**7종 모두 원본 캔버스 그대로 쓴다** — 도구 넷은 64 캔버스에 그림이 19~28px뿐이라 줄이면 뭉갠다.
`Cursor_04`(브래킷 128)는 어느 크기로도 겉돌아 쓰지 않는다.

> **`cursors/` 폴더는 `UI_Frames` 아틀라스에 등록돼 있다.** 새 PNG를 Sprite로 임포트하면
> 아틀라스에 빨려 들어가 커서로 못 쓴다. 반드시 `Texture Type: Cursor` · `Filter Mode: Point`.

---

## 게임 이벤트 목록 [구현] — 31개가 전부 (2026-09-13 실측)

| 폴더 | 이벤트 |
|---|---|
| `Building/` | `BuildingSpawnEvent` · `BuildingDeathEvent` · `BuildingSetupCompleteEvent` |
| `Combat/` | `DamageTakenEvent` |
| `Economy/` | `SupplyEvent` · `ResourceChangedEvent` · `PopulationChangedEvent` |
| `Enemy/` | `EnemyDeathEvent` · `EnemyDispatchEvent` · `AssaultsClearedEvent` |
| `Sequence/` | `ObjectiveCompletedEvent` · `AllObjectivesCompletedEvent` |
| `System/` | `GameOverEvent` · `CommandSelectEvent` · `BuildPanelToggledEvent` · `RefreshData` · `VisibleStateChangeEvent` · `PlaceHolderSpawnEvent` · `PlaceHolderDestroyEvent` · `NoticeEvent` · `MapRegionOpenedEvent` |
| `Unit/` | `UnitSpawnEvent` · `UnitDeathEvent` · `UnitSelectEvent` · `UnitDeselectEvent` · `ProjectileLaunchEvent` · `PoolEffectPlayEvent` · `SquadSpawnEvent` · `SquadDeathEvent` · `CommandIssuedEvent`(유닛이 커맨드를 받음) · `CommanderOrderEvent`(`Kind`: 선 긋기·배속 — 튜토리얼 조건이 듣는다) |

**분대는 `UnitDeathEvent`가 아니라 `SquadDeathEvent`로 사망을 알린다.**
**지휘관이 선을 그으면 `CommanderOrderEvent(LineDrawn, LineCenter)`가 소유자 채널로 나간다** — `BattleLineDrawnConditionSO`가 이걸로 자리를 판정한다.

**목록에 없으면 없는 것이다.** 이전 판이 나열했던 `CapacityLimitChangedEvent` ·
`UnitCountChangedEvent` · `EnemyCoreDestroyedEvent` · `EnemyProductionSlowEvent` ·
`ProcessingStatusChangedEvent` · `TowerAmmoChangedEvent` · `PawnRoute*`는 존재하지 않는다.
`RearRaidEvent` · `RearRaidWarningEvent` · `ProtocolEscalation`은 있었다가 삭제됐다.

> **`VisibleStateChangeEvent`와 `PlaceHolder*`는 이 목록에서 빠졌다** — 안개 시스템이 돌아오면서
> 같이 살아났다(2026-08-19 확인).

---

## 주의사항
- `Bus<T>` 구독은 반드시 `OnDestroy`에서 해제할 것
- `SceneId` ↔ 빌드 인덱스 대응이 깨지면 씬 로드가 전부 죽는다
- 승패는 **본부 건물 수 0**으로 판정한다. 해당 Owner의 본부 건물이 맵에 없으면 판정 자체가 안 걸린다
- Owner는 `Player` / `Enemy` / `Unowned` 만 사용
- `DamageableSensor`는 `CircleCollider2D` + `OnTriggerEnter2D` 기반 — 감지 대상에 2D Collider 필수
- 분대 루트에는 콜라이더가 없다. 피해·선택은 멤버를 거친다
- **분대 루트의 체력은 `SquadRoster.HealthChanged`로만 갱신된다.** 스스로 `TakeDamage`를 못 받는다
- **스프라이트를 복제할 때는 GameObject 레이어까지 옮긴다.** 정렬 레이어·순서만 맞추면
  Default(0)에 남아 **어떤 카메라도 그리지 않는다**. `SortingCopier`가 표준이고,
  원본에 `SortingGroup`이 있으면 렌더러 자신의 정렬값은 죽은 값이라 복사하면 안 된다
- **`new GameObject()`로 표시를 만들 때도 같은 함정을 밟는다.** 복제가 아니라 신규 생성이라
  더 자주 놓친다 — 메인 카메라 컬링 마스크는 `123896`(Ground·Water·UI·Obstacle·Unit·
  Building·Supplies·Supply·PathLine·Enemy·TileMap)이고 **Default는 없다.**
  런타임 생성물은 `Rendering/RenderLayers.PlaceOnMarkerLayer`를 거친다.
  2026-08-29에 이 함정만으로 세 기능이 조용히 안 보였다
- **UI 오브젝트를 복제해 레이아웃 밖으로 뺄 때는 사각형이 따라오지 않는다.**
  `ignoreLayout`을 켜는 순간 레이아웃 그룹이 손을 놓고 RectTransform이 **프리팹 값으로 돌아간다.**
  앵커·피벗·`sizeDelta`·`anchoredPosition`을 원본에서 명시적으로 복사하고,
  읽기 전에 `LayoutRebuilder.ForceRebuildLayoutImmediate`를 돌린다(`FloatingDeltaText`가 표준).
  **세로로 쌓이는 TMP 텍스트는 거의 항상 폭 0이다** — 폰트·정렬보다 rect를 먼저 본다
- **`SelectionVar`는 좌우 스트레치 슬롯이다.** 폭 0짜리 점으로 되돌리면 자식이 피벗대로
  흩어져 다중 선택 바가 화면 밖으로 나간다. 슬롯 폭을 바꾸면 위 "HUD 골격"의 네 숫자를 다시 계산한다
- **시야를 잃어도 콜라이더는 살아 있다.** 클릭·드래그·호버를 막으려면 반드시
  `FogVisibility.IsVisibleNow()`를 거친다. 선택은 분대로 접기 **전** 원본으로 판정한다
  (분대 프록시는 `IHideable`이 아니라 늘 통과한다)
- **우클릭이 무엇에 걸리는지는 `RightClickCommandResolver` 한 곳에서만 정한다.** 실행하는 쪽과
  커서가 이것을 공유한다 — 규칙을 커서 쪽에 다시 적으면 커서가 채집이라 말하고 이동이 나간다
- **카메라 이동은 WASD · 방향키 · 화면 가장자리다** (2026-09-13에 WASD 복귀). 그래서 커맨드 단축키는
  `Q E R F / Z X C V`이고 `UI.Navigate`에서 W/A/S/D를 뺐다 — 카메라를 움직일 때 버튼 포커스가 같이 튄다.
  커맨드 키에 W·A·S·D를 다시 넣으면 카메라와 겹친다
- **방향 피해는 `normal`로 판정한다.** `ApplyDamage`의 `position`은 호출자 4곳이 전부
  피격자 자신의 좌표를 넣으므로 방향 판정에 쓰면 대형 오프셋을 재게 된다
- 구 `Code/Squads/`는 **2026-08-21에 철거됐다.** 분대 코드는 `Code.Military` 하나뿐이다
- **사라진 커맨드 에셋을 가리키는 칸은 `null`로 역직렬화될 뿐 경고도 예외도 내지 않는다.**
  순회하는 쪽이 반드시 null을 걸러야 한다 — 표준은 `Players/RightClickCommandResolver.Fill`이다
  (`CommandExecutor.FillAvailableCommands`는 `5eb7dd8f`에서 여기로 빠져나가 **없는 이름이다**).
  `CommandPanel.Fill`도 같은 null을 조용히 건너뛰고 빈 패널을 그대로 열어서, 2026-08-29까지
  지휘관 프리팹의 깨진 칸 9건이 아무 신호 없이 살아 있었다. 이제 에디터에서
  `Commandable/CommandArrayAudit`가 `Awake`에 프리팹당 한 번 경고한다
- **프리팹 참조의 `fileID`가 루트가 아닌 자식을 가리키면 그 자식만 복제된다.** `null`이 아니라서
  null 검사에 안 걸리고, `Instantiate`도 성공하며, 콘솔도 조용하다 — 화면에만 아무것도 안 나온다.
  `Eff_GiveMilitiaSquad`·`Eff_GiveArcherSquad`가 분대 루트(`7810100805333927986`) 대신
  `MiniMapicon`(`3130961509759176023`)을 물고 있어서 **튜토리얼 지원군이 한 번도 나온 적이 없었다**
  (2026-09-06 발견). 분대·유닛 프리팹은 루트 `fileID`가 파일마다 같은 값을 쓰는 경우가 많아
  눈으로는 구분이 안 된다 — **YAML의 `--- !u!1 &<fileID>` 다음 `m_Name`이 프리팹 이름과 같은지**로 본다.
  `GiveUnitsEffectSO`는 이제 생성물에 `AbstractCommandable`이 없으면 경고한다
- **두 스테이지 모두 손배치다(2026-09-07부터).** `MapGenerator`를 쓰는 씬은 없다. 생성기를 다시
  켜는 씬에는 손배치물을 남기지 말 것 — 생성기는 자기가 놓은 것만 기억하므로 겹친다
- **구역 뽑기의 단위는 앵커가 아니라 짝이다.** `PairKey`가 같은 앵커 2개는 함께 뽑히고 함께 빠지며
  같은 구성을 받는다. `Primary`/`Secondary`를 앵커 단위로 자르면 한쪽에만 적 진지가 서서 대칭이 깨진다
- **뭉치는 항목은 반드시 먼저 놓는다**(`MapGenerator.OrderForPlacement`). 자원이 먼저 깔리면
  진지 자리가 사라진다. 예전엔 캠프의 `minSpacing`이 제일 커서 우연히 먼저 놓였을 뿐이다
- **`BakedDataSO`는 스테이지마다 따로다** — 한 씬을 베이크해도 나머지가 안 덮인다.
  베이크가 씬보다 오래됐는지는 **셀 수로 잡는다**(타일맵에서 센 통행 셀 수와 비교)
- **맵 경계는 `MapRegionManager` → `StageSO` 순으로 읽는다.** 규칙을 어느 한쪽에 다시 적지 말 것 —
  카메라와 미니맵이 다른 맵을 보게 된다. 미니맵 창 비율과 `Camera.aspect`는 항상 같이 세팅한다
- **본거지 등록은 `HeadquartersMarker`가 하고, 해제는 `OnDestroy`가 아니라 `OnDied`에서 한다.**
  `BuildingDeathEvent`는 붕괴 연출 뒤에 나가고 **씬 언로드 때도 나가서** 승패가 오발한다
- **통행 판정에서 빠진 타일맵은 조용히 벽이 된다.** `PathBaker.walkableMaps`에 없는 타일맵은
  그려져 있어도 베이커가 못 본다. `Stage2`의 `Bride`(다리 6개)가 실제로 이 상태라 지형이
  세 조각으로 끊긴 것처럼 보였다 — 지형을 의심하기 전에 이 배열부터 본다
- **`PathAgent.SetDestination`은 즉시 계산하지 않는다.** 요청은 `PathRequestScheduler` 대기열에
  오르고 프레임당 `BudgetPerFrame`건만 계산된다 — 호출 직후 `IsOnPath`/`RemainingDistance`를 읽으면
  아직 옛 값이다. 도착 판정은 반드시 `PathPending`을 함께 본다(`MovementHelper.HasArrived`가 표준)
- `Pawn_BT` 채집은 1회당 본성 왕복이다 — 자원 곡선을 조정할 때 여기부터 본다
- **`Pawn_BT`의 `ResourceFilter`(캐러 갈 것)와 `SupplySO`(지금 든 것)를 합치지 말 것.**
  명령이 `SupplySO`를 덮으면 운반 애니메이션과 입금 자원이 통째로 바뀐다
- **지형을 고쳤으면 `PathBaker ▸ Bake Map (Full)`을 다시 돌린다.** 절차 배치가 베이크 데이터를 읽는다
- **`MonoSingleton.Instance`는 자동 생성하지 않는다.** 씬에 없으면 `null`이고 조용히 아무 일도
  안 일어난다(`UserPopulation`이 실제로 이래서 인구가 `0 / 0`이었다). 새 매니저는 `_Core`에 붙인다
- **`Pretendard-Black SDF.asset`은 `skip-worktree`로 막았다 (2026-09-03).** 이제 `git status`에 안 뜬다.
  Dynamic 폰트라 아틀라스와 글리프 표는 `.otf`에서 실시간으로 굽는 **캐시**인데, 에디터가 한글을
  그릴 때마다 채워 넣어 **7.2MB · 글리프 21,889개**가 매번 커밋에 딸려 왔다.
  **이전 판의 "`git checkout`으로 되돌린다"도, 인스펙터의 `Clear Dynamic Data`도 소용없다** —
  Clear를 눌러도 씬·게임 뷰가 다시 그리는 순간 즉시 재생성된다
  (실측 2026-09-03: 7,227,888 → 7,051,587바이트, 글리프 21,889 → 21,887. 사실상 그대로).
  ```bash
  git update-index --skip-worktree "Assets/_Graphics/Font/Pretendard-Black SDF.asset"
  ```
  **클론마다 따로 걸어야 한다.** 걸린 동안 `git pull`·`git checkout`이 그 파일을 바꾸려 하면
  에러가 나므로 그때만 `--no-skip-worktree`로 풀었다 다시 건다.
  확인은 `git ls-files -v <경로>`가 `S`로 시작하는지 본다.
  걸기 전에 반드시 `git checkout`으로 HEAD와 맞춘다 — 안 그러면 커밋 안 된 차이가 플래그 뒤에 숨는다.

  > **근본 해결(미착수)은 쓰는 글자만 굽는 것이다.** 한글 음절 11,172자를 다 구우면 4K·8K로도
  > 모자라지만, 이 게임이 실제로 쓰는 고유 한글은 **823자**뿐이라 **2048×2048 한 장**이면 된다
  > (실측: 씬·프리팹 `m_text` 98 + `GameModules`·`Resources`의 `.asset` 767 + 코드 리터럴 28의 합집합).
  > Font Asset Creator의 `Character Set: Characters from File`로 굽는다.
  > 다만 **순수 Static은 안 된다** — 폴리싱·시퀀스 제작으로 글자가 계속 늘어 안 구운 글자가 네모로 뜬다.
  > **Static 주 폰트 + Dynamic Fallback**으로 가고 더러워지는 fallback에만 `skip-worktree`를 건다.
  > 글자 목록은 일회용 `.txt`가 아니라 재실행 가능한 추출 도구로 뽑는다.
- **새 스크립트가 `CS0246`("타입 없음")으로 나오면 코드를 의심하기 전에**
  `Library/Bee/artifacts/*.dag/Assembly-CSharp.rsp`의 소스 목록을 본다. 목록에 파일이 없으면
  에셋 데이터베이스 desync다 — `.meta` 삭제 + 파일 재작성 후 `Ctrl+R`
- **`UnityEngine.Object`에 `is null` / `is not null` / `?.`을 쓰지 말 것.** 패턴 매칭도 null 조건 연산자도
  참조가 null인지만 보므로, "파괴됨"을 null로 보여 주는 **Unity의 `==` 오버로드를 통째로 우회한다.**
  `!= null`을 쓴다 — 콜라이더 없는 분대 루트에서 `is not null` 가드가 통과해
  `MissingComponentException`이 났었고, `RuntimeUI`의 `generalInfoUI?.Unbind()`가 **파괴된**
  패널에서 실행돼 게임 종료 때마다 `MissingReferenceException`이 났었다.
  **`?.`는 순수 C# 객체에만 쓴다**(`_subscriptionScope?.Dispose()`는 그대로 옳다)
- **`Select()`는 대상의 Owner 채널로 이벤트를 쏜다.** 구독하는 쪽이 `Owner.Player`만 듣고 있으면
  적을 클릭해도 아무 패널이 안 뜬다. 선택은 되고 있는데 표시만 안 되는 것이라 오진하기 쉽다
- **BT 액션이 `Failure`를 내면 그 시퀀스의 뒤가 통째로 안 돈다.** "할 일이 없었다"와 "실패했다"를
  구분한다 — `DepositSuppliesAction`이 빈손 귀환을 `Failure`로 내는 바람에 뒤의 `Command` 되돌리기가
  실행되지 않아 일꾼이 본성에 얼어붙었다. 자원이 고갈되는 순간 `Collect()`가 0을 주므로 자주 일어난다
- **`Physics2D.Overlap*` 버퍼 크기는 탐색 반경과 함께 본다.** 버퍼가 차면 원하는 대상이 결과에서
  밀려난다 — `FindClosestHeadQuarterAction`이 버퍼 20개에 `SearchRadius` 100이라 본성을 놓쳤다
- **공격은 발을 멈춘 뒤에 시작한다.** 멈추는 줄을 스윙 시작 뒤에 두면 쿨다운 분기가 에이전트를
  건드리지 않고, 애니메이터 없는 유닛은 `CombatSwingHelper`가 인라인으로 때려 이동 중 공격이 나간다
- **`Slider`는 최대치도 매번 넣어야 한다.** `Bind` 시점에 대상이 `Start` 전이면 `MaxHealth`가 0이라
  `maxValue`가 0으로 굳고 바가 영원히 빈다. 현재값만 갱신하는 코드를 의심한다
- **`ContentSizeFitter`와 가운데 정렬을 같이 쓰면 자식이 밀린다.** 폭이 내용 따라 변하므로
  칸이 켜질 때마다 양쪽으로 벌어져 맨 끝 자식이 움직인다. 고정 폭으로 못 박는다
- **`Awake`에서 던진 예외 하나가 매니저를 통째로 죽인다.** 구독조차 안 걸려 아무 일도 안 일어나고
  예외는 한 번만 찍힌다 — `DataManager`가 `ToDictionary` 중복 키로 실제로 이랬다.
  사전을 만들 때는 빈 칸·중복을 건너뛰고 경고만 남긴다
- **에디터 버전을 왕복시키지 말 것.** URP `m_AssetVersion` 마이그레이션은 **올라가기만 한다** —
  `UniversalRenderPipelineGlobalSettings.UpgradeAsset`의 분기가 전부 `if (m_AssetVersion < N)`이라
  내려오는 경로가 아예 없다. 상위 에디터로 **한 번만 열어도** 숫자가 올라가고, 하위로 돌아오면
  `IsAtLastVersion()`이 영원히 false라 **빌드가 통째로 막힌다**(`URPBuildDataValidator`가
  `"is not at last version"`으로 죽이고, Library를 지워도 안 낫는다).
  되돌리려면 에디터를 닫고 `Assets/UniversalRenderPipelineGlobalSettings.asset`의
  `m_AssetVersion`을 손으로 내리는 수밖에 없다 (2026-09-03에 6000.6.0f1로 열려 11이 됐고
  6000.5.5f1의 `k_LastVersion`은 10이었다). **기준 에디터는 `6000.5.5f1` 하나다.**
