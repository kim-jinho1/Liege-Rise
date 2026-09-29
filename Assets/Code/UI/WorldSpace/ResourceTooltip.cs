using Code.UI.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.WorldSpace
{
    /// <summary>
    /// 자원 오브젝트 옆에 표시되는 월드 스페이스 툴팁입니다.
    /// </summary>
    public class ResourceTooltip : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private ProgressBar countProgress;

        private const string CountFormat = "{0} / {1}";

        private void Awake()
        {
            Debug.Assert(iconImage != null, "Icon Image is not assigned.", this);
            Debug.Assert(nameText != null, "NameText is not assigned.", this);
            Debug.Assert(countText != null, "CountText is not assigned.", this);
            Debug.Assert(countProgress != null, "CountProgress is not assigned.", this);

            // 툴팁이 커서를 가로채면 그 밑의 자원·유닛을 고를 수 없다
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;

            tooltipPanel.SetActive(false);
        }

        /// <summary>
        /// 툴팁 데이터를 표시하고 지정된 월드 좌표에 배치합니다.
        /// </summary>
        public void EnableFor(TooltipData data, Vector2 worldPosition)
        {
            iconImage.sprite = data.Icon;
            nameText.SetText(data.Name);
            countText.SetText(CountFormat, data.Current, data.Max);
            countProgress.SetProgress(data.NormalizedProgress);
            tooltipPanel.SetActive(true);
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
        }

        /// <summary>
        /// 표시 중인 툴팁의 수량·진행도만 실시간으로 갱신합니다(위치·아이콘·이름 유지).
        /// </summary>
        public void UpdateCount(int current, int max)
        {
            if (!tooltipPanel.activeSelf)
                return;

            countText.SetText(CountFormat, current, max);
            countProgress.SetProgress(max > 0 ? (float)current / max : 0f);
        }

        /// <summary>
        /// 툴팁을 숨깁니다.
        /// </summary>
        public void Disable()
        {
            tooltipPanel.SetActive(false);
        }
    }
}
