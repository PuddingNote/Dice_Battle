using UnityEngine;

namespace DiceBattle.UI
{
    /// <summary>
    /// 사운드 재생 담당. BGM용 AudioSource 하나 + 효과음용 AudioSource 하나를 쓴다.
    /// 어디서든 AudioManager.PlayButton() 처럼 정적 메서드로 호출한다.
    /// 아직 만들어지지 않았거나 사운드가 비어 있으면 조용히 무시한다(호출부에서 검사 불필요).
    ///
    /// 볼륨/켜짐 여부는 <see cref="SoundSettings"/>를 따른다.
    /// BGM은 OFF로 바꾸면 멈추고, 다시 ON으로 바꾸면 처음부터 재생한다.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        private SoundBank _bank;
        private AudioSource _bgmSource;
        private AudioSource _sfxSource;
        // 연출이 끝나면 도중에 끊어야 하는 효과음(주사위 굴림) 전용.
        // PlayOneShot은 개별로 멈출 수 없어 소스를 따로 둔다.
        private AudioSource _shakeSource;

        /// <summary>부트스트랩에서 한 번만 호출한다.</summary>
        public static AudioManager Create(Transform parent, SoundBank bank)
        {
            var go = new GameObject("AudioManager");
            go.transform.SetParent(parent, false);

            var manager = go.AddComponent<AudioManager>();
            manager._bank = bank;
            manager.Setup();
            _instance = manager;
            return manager;
        }

        private void Setup()
        {
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;           // 끊김 없이 무한 반복
            _bgmSource.playOnAwake = false;
            _bgmSource.clip = _bank != null ? _bank.bgm : null;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
            // 효과음 볼륨은 PlayOneShot 호출마다 지정하므로 소스 볼륨은 1로 고정한다.
            _sfxSource.volume = 1f;

            _shakeSource = gameObject.AddComponent<AudioSource>();
            _shakeSource.loop = false;
            _shakeSource.playOnAwake = false;

            SoundSettings.Changed += ApplySettings;
            ApplySettings();
        }

        private void OnDestroy()
        {
            SoundSettings.Changed -= ApplySettings;
            if (_instance == this) _instance = null;
        }

        /// <summary>설정(ON/OFF·볼륨)을 현재 재생 상태에 반영한다.</summary>
        private void ApplySettings()
        {
            if (_bgmSource.clip == null) return;

            if (SoundSettings.BgmOn)
            {
                _bgmSource.volume = SoundSettings.BgmVolume;
                // Stop() 뒤의 Play()는 처음부터 재생된다.
                if (!_bgmSource.isPlaying) _bgmSource.Play();
            }
            else if (_bgmSource.isPlaying)
            {
                _bgmSource.Stop();
            }
        }

        // ---- 효과음 ----

        // UnityEngine.Object는 파괴 후에도 C# 참조가 남으므로 ?. 대신 != null 로 검사한다.
        private static SoundBank Bank => _instance != null ? _instance._bank : null;

        /// <summary>버튼 클릭(시작/설정/리롤/닫기 등 모든 버튼).</summary>
        public static void PlayButton() => Play(Bank != null ? Bank.buttonSelect : null);

        /// <summary>주사위를 라인에 배치할 때.</summary>
        public static void PlayDicePlace() => Play(Bank != null ? Bank.dicePlace : null);

        /// <summary>주사위 굴림 시작. 눈이 정해지면 <see cref="StopDiceShake"/>로 끊는다.</summary>
        public static void PlayDiceShake()
        {
            AudioClip clip = Bank != null ? Bank.diceShake : null;
            if (clip == null || _instance == null || !SoundSettings.SfxOn) return;

            var source = _instance._shakeSource;
            source.clip = clip;
            source.volume = SoundSettings.SfxVolume;
            source.Play();
        }

        /// <summary>주사위 굴림 소리를 즉시 끊는다(눈이 확정되거나 연출이 중단될 때).</summary>
        public static void StopDiceShake()
        {
            if (_instance != null) _instance._shakeSource.Stop();
        }

        /// <summary>제거 직전 주사위가 떨릴 때(사운드 미지정 시 무음).</summary>
        public static void PlayDiceRemoveStart() => Play(Bank != null ? Bank.diceRemoveStart : null);

        /// <summary>주사위끼리 부딪혀 튕겨나갈 때(사운드 미지정 시 무음).</summary>
        public static void PlayDiceRemoveHit() => Play(Bank != null ? Bank.diceRemoveHit : null);

        /// <summary>한 판이 끝나고 결과 창이 뜨는 순간.</summary>
        public static void PlayRoundEnd() => Play(Bank != null ? Bank.roundEnd : null);

        private static void Play(AudioClip clip)
        {
            if (clip == null || _instance == null) return;
            if (!SoundSettings.SfxOn) return;
            _instance._sfxSource.PlayOneShot(clip, SoundSettings.SfxVolume);
        }
    }
}
