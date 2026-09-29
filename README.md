# Liege & Rise

일꾼으로 자원을 모아 부대를 만들고, 지휘관에게 선을 그어 전선을 지휘하는 2D 탑뷰 중세 RTS입니다.
졸업 작품 프로젝트이며 2026년 9월 부스에서 관람객 시연을 했습니다.

- **플레이 영상**: https://youtu.be/xRs26fw_PSY
- **팀**: 2명 (프로그래머 1명, 아트 1명) · 저는 팀장 · 프로그래밍 · 기획
- **엔진**: Unity 6 (6000.5.5f1) · C# · URP 2D

> 이 저장소는 **코드 열람용**입니다. `Assets/Code`만 들어 있고 씬·프리팹·아트·유료 에셋은 빠져 있어서
> 이 저장소만으로는 빌드되지 않습니다. 원본 저장소는 커밋 히스토리에 유료 에셋이 섞여 있어 공개하지 않았습니다.

## 먼저 볼 코드

| 무엇 | 어디 | 한 줄 설명 |
|---|---|---|
| 지휘관 공세선 | `Assets/Code/Military/Commanders/` | 선을 분대 명령으로 바꾸는 두뇌(`CommanderLineExecutor`)를 유닛과 분리했습니다. 적 AI도 선만 그어 주면 같은 두뇌로 움직입니다 |
| 선 긋기 입력 | `Assets/Code/Players/BattleLineDrawer.cs` | 우클릭 드래그로 선을 긋고, Tab으로 미는 방향을 뒤집습니다 |
| 적 AI | `Assets/Code/EnemyAI/` | 시간표 기반 공세, 갈래마다 지휘관 두뇌(`EnemyLineBrain`) |
| 분대 상태 머신 | `Assets/Code/Military/Combat/FSM/` | 대기 · 이동 · 전진 · 접근 · 교전 · 탑승 |
| 방향 피해 | `Assets/Code/Military/Combat/SquadArmor.cs` | 부대의 옆과 뒤가 앞보다 약합니다 |
| 길찾기 부하 분산 | `Assets/Code/Navigation/Pathfinding/PathRequestScheduler.cs` | 경로 요청을 대기열에 쌓고 한 프레임에 2ms까지만 처리합니다 |
| 튜토리얼 시퀀스 | `Assets/Code/Sequences/` · `Assets/Code/Objectives/` | 스텝 · 완료 조건 · 효과 · 안내 손 |

**길찾기 측정 (에디터, 조건별 5회 평균)**: 분대가 이동할 때 30~60ms 스파이크가 나던 것을 보고 시간 분할을 넣었습니다.
조치를 끄고 켜서 비교한 결과, 길찾기가 한 프레임에 쓰는 최대 시간이 **7.3ms → 3.1ms**로 줄었습니다.
같은 측정에서 남은 병목은 길찾기가 아니라 2D 물리 트리거 접촉이었습니다.

## 출처

숨기지 않고 구분해 둡니다.

| 부분 | 출처 |
|---|---|
| 이벤트 버스(`Bus<T>`), 커맨드 SO, 유닛 계층, 단일 유닛 전투, 안개 | 학교 RTS 강의를 따라 만든 구조에서 출발 |
| 타일맵 A* · 경로 베이킹 | 다른 프로젝트 강의에서 가져와 고도 · 램프 베이킹을 더함 |
| 분대 · 지휘관 공세선 · 방향 피해 · 적 AI · 튜토리얼 시퀀스 · 길찾기 부하 분산 | 이 프로젝트에서 새로 설계 |
| `Assets/GondrLib` (오브젝트 풀 등) | 수업 라이브러리 — 이 저장소에는 없습니다 |

새로 설계한 부분은 방향과 판단은 제가 정했고, 구현은 AI 코딩 에이전트(Claude Code)와 함께 했습니다.

## AI 에이전트와 개발한 방식

[`CLAUDE.md`](CLAUDE.md)가 에이전트가 매 작업마다 읽는 **규칙 문서**입니다.
한 클래스 300줄 제한, 계획을 먼저 설명받고 승인한 뒤 작업, 기능별 커밋, 금지 API, 과거에 밟은 함정 기록이 들어 있습니다.
컴파일이 통과해도 에디터에서 직접 플레이해 확인한 뒤에 커밋했습니다.

## 컨벤션 적용 현황

`CLAUDE.md`의 코딩 컨벤션은 2026년 9월에 문서로 정리했고, 전체 코드에 나눠서 적용하는 중입니다.

- **지켜진 것**: 300줄 제한 위반 0건 · 금지 API(`FindObjectOfType`, `OverlapCircleNonAlloc`) 0건 · `[Tooltip]` · `#region` 0건
  · 줄바꿈 · 인코딩 · `using` 정렬 · 문서 주석 문체 통일
- **남은 것**: 문서 주석(`<summary>`)이 없는 타입 약 130개 · 이벤트 필드 `On~` 접두 개명 19건 · 데이터 struct의 프로퍼티 전환

## 의존성 (이 저장소에 포함되지 않음)

Unity Behavior · Input System · URP · TextMesh Pro · DOTween · Odin Inspector · BroAudio · Reflex · GondrLib(수업 라이브러리)
