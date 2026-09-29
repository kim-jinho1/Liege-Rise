using Code.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.MainMenu
{
    /// <summary>
    /// 설정 패널의 Master/BGM/SFX 슬라이더를 AudioSettingsController에 바인딩합니다.
    /// </summary>
    public class SettingsPanelUI : MonoBehaviour
    {
        [Header("Sliders")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Optional Value Labels")]
        [SerializeField] private TextMeshProUGUI masterValueText;
        [SerializeField] private TextMeshProUGUI bgmValueText;
        [SerializeField] private TextMeshProUGUI sfxValueText;

        [Header("UI Elements")]
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject overlay;

        private const string PercentFormat = "{0}%";

        private AudioSettingsController _audioSettingsController;
        private MenuAudio _menuAudio;

        private void OnEnable()
        {
            if (_audioSettingsController == null)
                return;

            InitSlider(masterSlider, _audioSettingsController.Master);
            InitSlider(bgmSlider, _audioSettingsController.BGM);
            InitSlider(sfxSlider, _audioSettingsController.SFX);

            UpdateLabel(masterValueText, _audioSettingsController.Master);
            UpdateLabel(bgmValueText, _audioSettingsController.BGM);
            UpdateLabel(sfxValueText, _audioSettingsController.SFX);
        }

        private void Awake()
        {
            if(AudioSettingsController.Instance == null || MenuAudio.Instance == null)
                return;

            _audioSettingsController = AudioSettingsController.Instance;
            _menuAudio = MenuAudio.Instance;
            if (masterSlider == null || bgmSlider == null || sfxSlider == null || closeButton == null)
                return;

            masterSlider.onValueChanged.AddListener(HandleMasterChanged);
            bgmSlider.onValueChanged.AddListener(HandleBgmChanged);
            sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
            closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (masterSlider == null || bgmSlider == null || sfxSlider == null || closeButton == null)
                return;

            masterSlider.onValueChanged.RemoveListener(HandleMasterChanged);
            bgmSlider.onValueChanged.RemoveListener(HandleBgmChanged);
            sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
            closeButton.onClick.RemoveListener(Close);
        }

        private void HandleMasterChanged(float value)
        {
            _audioSettingsController?.SetMaster(value);
            UpdateLabel(masterValueText, value);
        }

        private void HandleBgmChanged(float value)
        {
            _audioSettingsController?.SetBGM(value);
            UpdateLabel(bgmValueText, value);
        }

        private void HandleSfxChanged(float value)
        {
            _audioSettingsController?.SetSFX(value);
            UpdateLabel(sfxValueText, value);
        }

        /// <summary>
        /// 설정 패널을 닫습니다.
        /// </summary>
        private void Close()
        {
            _menuAudio?.ButtonClick();
            gameObject.SetActive(false);
            overlay?.SetActive(false);
        }

        /// <summary>
        /// 슬라이더 범위를 0~1로 맞추고 콜백 없이 현재 값을 반영합니다.
        /// </summary>
        private static void InitSlider(Slider slider, float value)
        {
            if (slider == null)
                return;

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(value);
        }

        /// <summary>
        /// 값 라벨을 퍼센트로 표시합니다.
        /// </summary>
        private static void UpdateLabel(TextMeshProUGUI label, float value)
        {
            if (label != null)
                label.SetText(string.Format(PercentFormat, Mathf.RoundToInt(value * 100f).ToString()));
        }
    }
}
