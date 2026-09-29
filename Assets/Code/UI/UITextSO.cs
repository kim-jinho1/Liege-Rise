using UnityEngine;

namespace Code.UI
{
    /// <summary>
    /// 여러 화면이 함께 쓰는 표기입니다. 한쪽만 고치면 목록과 상세가 같은 것을 다르게 부릅니다.
    /// </summary>
    [CreateAssetMenu(fileName = "UIText", menuName = "UI/UI Text", order = 0)]
    public class UITextSO : ScriptableObject
    {
        [field: Header("기록")]
        [field: SerializeField] public string NoRecord { get; private set; } = "—";

        /// <summary>
        /// 계산은 TextFormat이 하고 부르는 말만 여기 있습니다. {0}=분 {1}=초
        /// </summary>
        [field: SerializeField] public string DurationFormat { get; private set; } = "{0}분 {1:00}초";

        /// <summary>
        /// 목표 진행도입니다. {0}=지금까지 {1}=목표
        /// </summary>
        [field: SerializeField] public string ObjectiveProgressFormat { get; private set; } = "{0} / {1}";

        [field: Header("영지 상태")]
        [field: SerializeField] public string HiddenStageName { get; private set; } = "???";
        [field: SerializeField] public string StageCleared { get; private set; } = "완료";
        [field: SerializeField] public string StageOpen { get; private set; } = "도전 가능";

        /// <summary>
        /// 상태 줄과 출정 버튼이 함께 씁니다. 잠긴 영지는 어느 쪽에서도 같은 말이어야 합니다
        /// </summary>
        [field: SerializeField] public string StageLocked { get; private set; } = "잠김";

        [field: Header("출정 버튼")]
        [field: SerializeField] public string LaunchCleared { get; private set; } = "다시 하기";
        [field: SerializeField] public string LaunchOpen { get; private set; } = "출정";

        /// <summary>
        ///  읽는 쪽이 정적 창구(TextFormat)라 인스펙터 칸이 없습니다 — SquadMarkerStyleSO와 같은 이유입니다
        /// </summary>
        private const string ResourcePath = "UIText";

        private static UITextSO _cached;

        /// <summary>
        /// 공용 표기를 읽습니다. 에셋이 없으면 코드 기본값으로 돌고 한 번만 경고합니다 —
        /// 에셋 하나가 빠졌다고 화면의 글자가 통째로 사라지면 원인을 찾기 어렵습니다.
        /// </summary>
        public static UITextSO Load()
        {
            if (_cached != null)
                return _cached;

            _cached = Resources.Load<UITextSO>(ResourcePath);
            if (_cached != null)
                return _cached;

            _cached = CreateInstance<UITextSO>();
            Debug.LogWarning($"Resources/{ResourcePath} 을 찾지 못해 기본 문구로 표시합니다.");
            return _cached;
        }
    }
}
