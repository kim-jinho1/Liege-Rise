using Code.Buildings.Construction;
using Code.UI.Components;
using TMPro;
using UnityEngine;

namespace Code.UI.Selection
{
    /// <summary>
    /// 건설 현장 머리 위에 건물 이름과 진행률을 띄우는 월드 스페이스 표시입니다.
    /// 건설 더미 프리팹의 자식으로 두면 스스로 부모의 더미를 찾아 붙습니다.
    /// </summary>
    public class ConstructionTimerUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI progressText;

        // CommandableHPUI에서 물려받은 바. 그 프리팹의 CommandableHealthBarUI는 이 변형에서
        // 제거돼 있어서, 배선하지 않으면 슬라이더가 프리팹 값에 굳은 채 안 움직인다
        [SerializeField] private ProgressBar progressBar;

        [Header("Settings")]
        [SerializeField, Min(0.02f)] private float refreshInterval = 0.1f; // 초당 10회 갱신(매 프레임 갱신 X)

        [Header("Text")]
        [SerializeField] private string pausedFormat = "{0} - 중단";

        // 숫자 서식이라 인스펙터로 뺄 것이 없다
        private const string ProgressFormat = "{0:0} %";

        private ConstructionDummy _dummy;
        private float _timer;

        private void Awake()
        {
            _dummy = GetComponentInParent<ConstructionDummy>();

            Debug.Assert(_dummy != null, $"[{name}] 부모에서 ConstructionDummy를 찾지 못했습니다.", this);
            Debug.Assert(titleText != null, $"[{name}] Name Text가 비어 있습니다.", this);
            Debug.Assert(progressText != null, $"[{name}] Remaining Text가 비어 있습니다.", this);
            Debug.Assert(progressBar != null, $"[{name}] Progress Bar가 비어 있습니다.", this);
        }

        /// <summary>
        /// 초기화 시 건물 이름과 진행률을 표시합니다.
        /// </summary>
        private void Start()
        {
            if (_dummy == null || _dummy.BuildingSO == null)
                return;

            titleText?.SetText(_dummy.BuildingSO.Name);
            progressText?.SetText(ProgressFormat, 0f);

            // 최대치를 안 넣으면 프리팹 직렬화 값에 갇혀 바가 끝까지 안 찬다
            progressBar?.SetMaxProgress(1f);
            progressBar?.SetProgress(0f);
        }

        /// <summary>
        /// 건설 진행률을 일정 간격으로 갱신합니다. 일꾼이 없으면 중단 표시를 냅니다.
        /// </summary>
        private void LateUpdate()
        {
            if (_dummy == null || progressText == null)
                return;

            _timer -= Time.deltaTime;
            if (_timer > 0f)
                return;

            _timer = refreshInterval;
            Render();
        }

        /// <summary>
        /// 건설 진행률을 백분율로 표시합니다. 일꾼이 없어 진행이 멈춘 동안에는 진행률 대신 중단 표시를 냅니다.
        /// </summary>
        private void Render()
        {
            if (_dummy.ConstructionSpeed <= 0f)
            {
                // 멈춘 동안에도 바는 그대로 둔다 — 어디까지 지었는지가 사라지면 안 된다
                titleText.SetText(string.Format(pausedFormat, _dummy.BuildingSO.Name));
                return;
            }

            float progress = Mathf.Clamp01(_dummy.ProgressData.Progress);

            titleText.SetText(_dummy.BuildingSO.Name);
            progressText.SetText(ProgressFormat, progress * 100f);
            progressBar?.SetProgress(progress);
        }
    }
}
