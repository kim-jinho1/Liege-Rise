using Ami.BroAudio;
using Code.Core;
using UnityEngine;
using BroAudioApi = Ami.BroAudio.BroAudio;

namespace Code.Audio
{
    /// <summary>
    /// 메뉴 씬의 배경음 재생과 공용 버튼 효과음을 담당하는 싱글톤입니다.
    /// SoundID는 BroAudio 라이브러리에서 인스펙터로 지정합니다.
    /// </summary>
    public class MenuAudio : MonoSingleton<MenuAudio>
    {
        [Header("Sound IDs (assign from BroAudio Library)")]
        [SerializeField] private SoundID bgm;
        [SerializeField] private SoundID buttonClick;

        [Header("Options")]
        [SerializeField] private bool playBgmOnStart = true;
        [SerializeField] private float bgmFadeIn = 1f;

        private void Start()
        {
            if (playBgmOnStart && bgm.IsValid())
                BroAudioApi.Play(bgm, bgmFadeIn);
        }

        /// <summary>
        /// 공용 버튼 클릭 효과음을 재생합니다.
        /// </summary>
        private void PlayButtonClick()
        {
            if (buttonClick.IsValid())
                BroAudioApi.Play(buttonClick);
        }

        /// <summary>
        /// 인스턴스가 있으면 버튼 클릭 효과음을 재생합니다(정적 헬퍼).
        /// </summary>
        public void ButtonClick()
        {
            if (Instance != null)
                Instance.PlayButtonClick();
        }
    }
}
