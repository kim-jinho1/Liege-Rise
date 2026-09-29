using System;
using Code.Core;
using Code.EnemyAI;
using Code.Sequences;
using UnityEngine;

namespace Code.Stages
{
    /// <summary>
    /// 스테이지 선택 UI에 표시되는 단일 스테이지의 데이터입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Stage", menuName = "Stages/Stage")]
    public class StageSO : ScriptableObject
    {
        [field: SerializeField] public string DisplayName { get; private set; }

        [field: SerializeField, TextArea] public string Description { get; private set; }

        [field: SerializeField] public Sprite Thumbnail { get; private set; }

        [field: SerializeField] public Sprite ListThumbnail { get; private set; } // 비어 있으면 Thumbnail로 대체된다

        /// <summary>
        /// 로드할 씬입니다(SceneId enum, Build Settings 인덱스와 일치).
        /// </summary>
        [field: SerializeField] public SceneId Scene { get; private set; }

        [field: Header("맵 규격")]
        // 카메라가 벗어나지 못하는 한계이자 미니맵이 비추는 범위다.
        // 카메라·미니맵이 같은 숫자를 봐야 하므로 스테이지 정의를 유일한 출처로 둔다
        [field: SerializeField] public Vector2 MapMin { get; private set; }

        [field: SerializeField] public Vector2 MapMax { get; private set; } = new(128f, 128f);

        // 막마다 열리는 구역. 비어 있으면 처음부터 맵 전체가 열린다.
        // 구역은 누적되므로 뒤 구역이 앞 구역을 품는다
        [field: SerializeField] public MapRegion[] MapRegions { get; private set; } = Array.Empty<MapRegion>();

        [field: Header("적 압박")]
        // 켜면 공세 시간표가 저절로 돌지 않고 시퀀스가 부를 때까지 기다린다.
        // 가르치는 중에 공세가 들이닥치면 튜토리얼이 끊긴다.
        // EnemyAIManager는 _Core 상주라 씬마다 체크박스를 달 수 없어 여기가 유일한 자리다
        [field: SerializeField] public bool DeferAssaults { get; private set; }

        // 이 스테이지만의 공세 시간표. 비워 두면 _Core의 기본 표를 쓴다.
        // 튜토리얼은 딱 한 번, 작게 와야 해서 본편 표를 같이 쓸 수 없다
        [field: SerializeField] public EnemyAIConfigSO AssaultConfig { get; private set; }

        [field: Header("시퀀스")]
        // 비워 두면 씬의 SequenceManager에 꽂힌 시퀀스를 쓴다. 같은 씬을 다른 시퀀스로 돌릴 때(부스용 짧은 판)만 채운다
        [field: SerializeField] public SequenceSO SequenceOverride { get; private set; }

        // 튜토리얼이 짚을 자리를 사람이 지도를 보고 찍어 둔 것. 비어 있는 종류는 코드가 계산한다.
        // 성 기준 오프셋으로 추측하면 지형을 못 봐서 나무·물 위를 짚는다
        [field: SerializeField] public GuidePoint[] GuidePoints { get; private set; } = Array.Empty<GuidePoint>();

        // 적 공세가 지나갈 길목. 소굴 쪽에서 성 쪽 순서로 찍는다.
        // 비워 두면 최단 경로로 곧장 온다 — 그러면 길이 늘 같아서 매복도 협공도 성립하지 않는다
        [field: SerializeField] public Vector2[] WaveRoute { get; private set; } = Array.Empty<Vector2>();

        /// <summary>
        /// 찍어 둔 자리가 있으면 그 좌표입니다.
        /// </summary>
        public bool TryGetGuidePoint(GameObjectHighlightType key, out Vector2 position)
        {
            if (GuidePoints != null)
            {
                foreach (GuidePoint point in GuidePoints)
                {
                    if (point == null || point.Key != key)
                        continue;

                    position = point.Position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        /// <summary>
        /// 맵의 중심 좌표입니다.
        /// </summary>
        public Vector2 MapCenter => (MapMin + MapMax) * 0.5f;

        /// <summary>
        /// 맵의 가로·세로 크기입니다.
        /// </summary>
        public Vector2 MapSize => MapMax - MapMin;

        /// <summary>
        /// true이면 잠금 상태로 표시되어 선택할 수 없습니다(준비중 등 수동 잠금용).
        /// </summary>
        [field: SerializeField] public bool IsLocked { get; private set; }

        /// <summary>
        /// 이 스테이지가 해금되기 위해 먼저 클리어되어야 하는 선행 스테이지들입니다.
        /// 비어 있으면 진척과 무관하게 처음부터 해금된 상태입니다.
        /// </summary>
        [field: SerializeField] public SceneId[] PrerequisiteStages { get; private set; } = Array.Empty<SceneId>();

        /// <summary>
        /// 이 스테이지를 클리어할 때 부여되는 해금 키 목록입니다(요소 해금·대사 트리거 등에 사용).
        /// </summary>
        [field: SerializeField] public string[] GrantsUnlockKeysOnClear { get; private set; } = Array.Empty<string>();

#if UNITY_EDITOR

        /// <summary>
        /// 구역 합집합이 맵 규격과 어긋나면 알립니다. 둘이 갈라지면 카메라와 미니맵이 다른 맵을 봅니다.
        /// </summary>
        private void OnValidate()
        {
            if (MapRegions == null || MapRegions.Length == 0)
                return;

            Vector2 min = MapRegions[0].Min;
            Vector2 max = MapRegions[0].Max;
            foreach (MapRegion region in MapRegions)
            {
                min = Vector2.Min(min, region.Min);
                max = Vector2.Max(max, region.Max);
            }

            if (min != MapMin || max != MapMax)
                Debug.LogWarning($"[{name}] 구역 합집합 {min}~{max} 이(가) 맵 규격 {MapMin}~{MapMax} 과 다릅니다.", this);
        }

#endif
    }
}
