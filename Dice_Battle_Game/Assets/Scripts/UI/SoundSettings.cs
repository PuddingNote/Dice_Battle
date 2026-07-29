using System;
using UnityEngine;

namespace DiceBattle.UI
{
    /// <summary>
    /// BGM/효과음 설정(켜짐 여부 + 볼륨 0~1). PlayerPrefs에 저장돼 다음 실행에도 유지된다.
    /// 아직 실제 재생 담당은 없다. 재생기를 만들면 <see cref="Changed"/>를 구독해
    /// <see cref="BgmVolumeEffective"/> / <see cref="SfxVolumeEffective"/>를 그대로 반영하면 된다.
    /// (UnityEngine.AudioSettings와 이름이 겹치지 않도록 SoundSettings로 둔다.)
    /// </summary>
    public static class SoundSettings
    {
        private const string BgmOnKey = "dicebattle.sound.bgmOn";
        private const string SfxOnKey = "dicebattle.sound.sfxOn";
        private const string BgmVolumeKey = "dicebattle.sound.bgmVolume";
        private const string SfxVolumeKey = "dicebattle.sound.sfxVolume";

        // 저장된 값이 없는 첫 실행 때 쓰는 기본 볼륨(슬라이더 0~1 기준, 20% / 50%).
        // 음원마다 원본 크기가 달라 BGM을 많이 낮춰 둔다.
        private const float DefaultBgmVolume = 0.2f;
        private const float DefaultSfxVolume = 0.5f;

        /// <summary>설정이 바뀔 때마다 발생.</summary>
        public static event Action Changed;

        public static bool BgmOn
        {
            get => PlayerPrefs.GetInt(BgmOnKey, 1) != 0;
            set => SetInt(BgmOnKey, value ? 1 : 0);
        }

        public static bool SfxOn
        {
            get => PlayerPrefs.GetInt(SfxOnKey, 1) != 0;
            set => SetInt(SfxOnKey, value ? 1 : 0);
        }

        public static float BgmVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, DefaultBgmVolume));
            set => SetFloat(BgmVolumeKey, value);
        }

        public static float SfxVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume));
            set => SetFloat(SfxVolumeKey, value);
        }

        /// <summary>실제로 적용할 BGM 볼륨(꺼져 있으면 0).</summary>
        public static float BgmVolumeEffective => BgmOn ? BgmVolume : 0f;

        /// <summary>실제로 적용할 효과음 볼륨(꺼져 있으면 0).</summary>
        public static float SfxVolumeEffective => SfxOn ? SfxVolume : 0f;

        private static void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        // 슬라이더는 드래그 중 매 프레임 호출되므로 디스크 저장은 하지 않는다(Flush에서 한 번에).
        private static void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            Changed?.Invoke();
        }

        /// <summary>미뤄둔 볼륨 값을 디스크에 저장한다(설정 창을 닫을 때 호출).</summary>
        public static void Flush() => PlayerPrefs.Save();
    }
}
