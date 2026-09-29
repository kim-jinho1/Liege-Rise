using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Components
{
    /// <summary>
    /// 진행 바 컴포넌트입니다. fillImage가 할당되면 Image.fillAmount 방식을 사용하고,
    /// 없으면 RectTransform offsetMin/offsetMax 방식을 사용합니다.
    /// </summary>
    public class ProgressBar : MonoBehaviour
    {
        [SerializeField] private Slider progressBar;

        private void Awake()
        {
            Debug.Assert(progressBar != null, $"Progress bar {name} is missing a Slider!");
        }

        /// <summary>
        /// 진행 바 값을 설정합니다.
        /// </summary>
        public void SetProgress(float progress)
        {
            progressBar.value = progress;
        }

        /// <summary>
        /// 진행 바의 최대값을 설정합니다.
        /// </summary>
        public void SetMaxProgress(float maxProgress)
        {
            progressBar.maxValue = maxProgress;
        }
    }
}
