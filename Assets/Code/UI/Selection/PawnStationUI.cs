using Code.Buildings.Base;
using Code.Buildings.Logic.Shared;
using TMPro;
using UnityEngine;

namespace Code.UI.Selection
{
    /// <summary>
    /// PawnStationHost를 가진 건물의 상주 일꾼 수를 표시합니다.
    /// 배치·회수 버튼은 커맨드 0·1번 칸이 전담하므로 여기서 다루지 않습니다.
    /// </summary>
    public class PawnStationUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI countText;

        [Header("Text")]
        // TMP의 SetText은 string.Format이 아니라 {0}·{1}만 아는 자체 파서다 — 서식 지정자를 붙이면 조용히 깨진다
        [SerializeField] private string countFormat = "상주 일꾼 {0} / {1}";

        /// <summary>
        /// 지금 표시 중인지 여부입니다. 상주 인원이 없는 건물이면 EnableFor를 받고도 false입니다.
        /// </summary>
        public bool IsShowing
            => panel != null && panel.activeSelf;

        private PawnStationHost _host;

        /// <summary>
        /// 지정된 건물에 대한 표시를 활성화하고 상주 수를 갱신합니다.
        /// </summary>
        public void EnableFor(AbstractBuilding building)
        {
            Cleanup();

            PawnStationHost host = building != null ? building.GetComponent<PawnStationHost>() : null;
            if (host == null)
            {
                panel.SetActive(false);
                return;
            }

            _host = host;
            _host.CountChanged += RefreshCount;
            RefreshCount();
            panel.SetActive(true);
        }

        /// <summary>
        /// 표시를 끄고 구독을 해제합니다.
        /// </summary>
        public void Disable()
        {
            Cleanup();
            panel.SetActive(false);
        }

        private void OnDestroy()
            => Cleanup();

        /// <summary>
        /// 상주 수 문자열을 다시 씁니다.
        /// </summary>
        private void RefreshCount()
        {
            if (_host == null)
                return;

            countText?.SetText(countFormat, _host.Count, _host.Max);
        }

        private void Cleanup()
        {
            if (_host == null)
                return;

            _host.CountChanged -= RefreshCount;
            _host = null;
        }
    }
}
