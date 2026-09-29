using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Alerts
{
    /// <summary>
    /// 피격이 이어지는 동안 화면 가장자리를 붉게 물들이는 비네트입니다.
    /// 이벤트를 직접 듣지 않습니다 — 언제 얼마나 붉힐지는 부르는 쪽이 정합니다.
    /// </summary>
    public class DamageVignetteUI : MonoBehaviour
    {
        [SerializeField] private Image vignette;
        [SerializeField, Range(0.05f, 1f)] private float hitBoost = 0.34f; // 한 대 맞을 때 오르는 강도. 이 값의 역수만큼 연달아 맞으면 최대가 된다
        [SerializeField, Min(0.1f)] private float fadeDuration = 1.6f; // 최대 강도에서 완전히 사라지기까지 걸리는 시간
        [SerializeField, Range(0.1f, 1f)] private float maxAlpha = 0.55f; // 강도가 1일 때의 알파

        private float _intensity;

        private void Awake()
        {
            if (vignette == null)
                return;

            // 화면 전체를 덮으므로 클릭을 먹으면 안 된다
            vignette.raycastTarget = false;
            vignette.enabled = false;
        }

        /// <summary>
        /// 비네트를 한 단계 짙게 합니다. 이후에는 스스로 옅어집니다.
        /// </summary>
        public void Pulse()
        {
            _intensity = Mathf.Min(1f, _intensity + hitBoost);
            Apply();
        }

        /// <summary>
        /// 비네트를 즉시 지웁니다.
        /// </summary>
        public void Clear()
        {
            _intensity = 0f;
            Apply();
        }

        /// <summary>
        /// 매 프레임마다 강도를 옅게 하고, 현재 강도에 따라 비네트 색상을 갱신합니다.
        /// </summary>
        private void Update()
        {
            if (_intensity <= 0f)
                return;

            _intensity = Mathf.MoveTowards(_intensity, 0f, Time.deltaTime / fadeDuration);
            Apply();
        }

        /// <summary>
        /// 현재 강도에 따라 비네트 색상을 갱신합니다. 강도가 0이면 비네트를 끕니다.
        /// </summary>
        private void Apply()
        {
            if (vignette == null)
                return;

            // 다 옅어지면 컴포넌트를 꺼서 전체 화면 오버드로를 없앤다
            bool visible = _intensity > 0f;
            if (vignette.enabled != visible)
                vignette.enabled = visible;

            if (!visible)
                return;

            Color color = vignette.color;
            color.a = _intensity * maxAlpha;
            vignette.color = color;
        }
    }
}
