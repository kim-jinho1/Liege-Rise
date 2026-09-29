using Ami.BroAudio;
using Code.Core;
using UnityEngine;
using BroAudioApi = Ami.BroAudio.BroAudio;

namespace Code.Audio
{
    /// <summary>
    /// Master/BGM/SFX 볼륨을 PlayerPrefs에 저장·로드하고 BroAudio에 적용하는 영속 싱글톤입니다.
    /// </summary>
    public class AudioSettingsController : MonoSingleton<AudioSettingsController>
    {
        private const string MasterKey = "MASTER";
        private const string BgmKey = "BGM";
        private const string SfxKey = "SFX";

        private const float DefaultVolume = 1f;

        /// <summary>
        /// 효과음 계열에 묶어서 적용하는 BroAudioType 플래그입니다.
        /// </summary>
        private const BroAudioType SfxTypes =
            BroAudioType.SFX | BroAudioType.UI | BroAudioType.Ambience | BroAudioType.VoiceOver;

        /// <summary>
        /// 전체 마스터 볼륨(0~1)입니다.
        /// </summary>
        public float Master { get; private set; }

        /// <summary>
        /// 배경음(BGM) 볼륨(0~1)입니다.
        /// </summary>
        public float BGM { get; private set; }

        /// <summary>
        /// 효과음(SFX) 볼륨(0~1)입니다.
        /// </summary>
        public float SFX { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Load();
        }

        private void Start()
        {
            Apply(); // BroAudio 런타임이 준비된 뒤 한 번 더 적용하여 초기값을 보장합니다.
        }

        /// <summary>
        /// 저장된 볼륨 값을 PlayerPrefs에서 불러옵니다.
        /// </summary>
        private void Load()
        {
            Master = PlayerPrefs.GetFloat(MasterKey, DefaultVolume);
            BGM = PlayerPrefs.GetFloat(BgmKey, DefaultVolume);
            SFX = PlayerPrefs.GetFloat(SfxKey, DefaultVolume);
        }

        /// <summary>
        /// 마스터 볼륨을 설정하고 즉시 적용·저장합니다.
        /// </summary>
        public void SetMaster(float value)
        {
            Master = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterKey, Master);
            Apply();
        }

        /// <summary>
        /// 배경음(BGM) 볼륨을 설정하고 즉시 적용·저장합니다.
        /// </summary>
        public void SetBGM(float value)
        {
            BGM = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(BgmKey, BGM);
            Apply();
        }

        /// <summary>
        /// 효과음(SFX) 볼륨을 설정하고 즉시 적용·저장합니다.
        /// </summary>
        public void SetSFX(float value)
        {
            SFX = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, SFX);
            Apply();
        }

        /// <summary>
        /// 현재 볼륨 값을 BroAudio에 반영합니다.
        /// </summary>
        private void Apply()
        {
            BroAudioApi.SetVolume(BroAudioType.Music, Master * BGM);
            BroAudioApi.SetVolume(SfxTypes, Master * SFX);
        }
    }
}
