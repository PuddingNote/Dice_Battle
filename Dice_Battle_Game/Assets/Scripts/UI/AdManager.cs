using System;
using UnityEngine;
using GoogleMobileAds.Api;

namespace DiceBattle.UI
{
    /// <summary>
    /// 광고 담당. <see cref="AudioManager"/>와 같이 정적 메서드로 호출한다.
    ///
    /// <b>AdMob SDK 타입은 이 클래스 밖으로 새어 나가지 않는다.</b>
    /// 나중에 미디에이션을 붙이거나 SDK를 갈아치울 때 고칠 곳을 여기 하나로 묶기 위해서다.
    ///
    /// 광고가 없거나 로드에 실패해도 <b>게임은 정상 진행된다.</b>
    /// 호출부는 광고가 떴는지 여부를 신경 쓰지 않고 콜백만 받으면 된다
    /// (<see cref="AudioManager"/>가 클립이 없을 때 무음으로 넘어가는 것과 같다).
    /// </summary>
    public sealed class AdManager : MonoBehaviour
    {
        // ---- 광고 단위 ID ----
        //
        // 실제 광고 단위 ID다. 에디터에서는 SDK가 자리표시 광고로 대체하므로
        // 실제 광고 요청이 나가지 않지만, <b>실기기에서 확인할 때는 반드시
        // AdMob 콘솔 → 설정 → 테스트 기기에 그 기기를 먼저 등록할 것.</b>
        // 등록 없이 내 광고를 반복해 누르면 무효 트래픽으로 계정이 정지될 수 있다.
        // 테스트 ID로 되돌려야 할 때는 docs/AdMobSetup.md 6장 참고.
        private const string InterstitialUnitId = "ca-app-pub-6387288948977074/9451912784";
        private const string RewardedUnitId = "ca-app-pub-6387288948977074/8597016170";

        // ---- 전면 광고 노출 규칙 ----

        /// <summary>이 판수를 채워야 전면 광고를 띄운다. 첫 판이 자연히 제외된다.</summary>
        private const int MatchesPerInterstitial = 3;

        /// <summary>
        /// 직전 노출로부터 최소 이만큼 지나야 다시 띄운다.
        /// 판수 조건만 두면 한 판을 빠르게 포기하고 재시작할 때 광고가 연달아 뜬다.
        /// </summary>
        private const float MinIntervalSeconds = 90f;

        /// <summary>
        /// 에디터에서 광고를 띄울지. 실제 광고 대신 회색 자리표시 화면이 뜬다.
        /// 켜두면 보상형뿐 아니라 <b>전면 광고도 뜬다</b> — 3판 조건이 차면
        /// 결과 화면을 나갈 때 자동으로 끼어든다. 연출 테스트에 방해되면 false로 바꾼다.
        /// </summary>
        private const bool EnableInEditor = true;

        private static AdManager _instance;

        private InterstitialAd _interstitial;
        private RewardedAd _rewarded;

        /// <summary>마지막 전면 광고 이후 끝난 판수.</summary>
        private int _matchesSinceAd;
        private float _lastInterstitialTime = float.NegativeInfinity;

        private Action _onInterstitialDone;
        private Action _onRewardEarned;
        private Action _onRewardFailed;
        private bool _rewardEarned;
        private float _timeScaleBeforeAd = 1f;

        private static bool Enabled
        {
            get
            {
#if UNITY_EDITOR
                return EnableInEditor;
#else
                return true;
#endif
            }
        }

        /// <summary>부트스트랩에서 한 번만 호출한다.</summary>
        public static AdManager Create(Transform parent)
        {
            var go = new GameObject("AdManager");
            go.transform.SetParent(parent, false);

            var manager = go.AddComponent<AdManager>();
            _instance = manager;
            manager.Initialize();
            return manager;
        }

        private void Initialize()
        {
            if (!Enabled) return;

            // 이 줄이 없으면 광고 콜백이 백그라운드 스레드에서 온다.
            // 거기서 Unity API를 만지면 크래시나므로 초기화보다 먼저 켠다.
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            MobileAds.Initialize(_ =>
            {
                LoadInterstitial();
                LoadRewarded();
            });
        }

        private void OnDestroy()
        {
            _interstitial?.Destroy();
            _rewarded?.Destroy();
            if (_instance == this) _instance = null;
        }

        // ---- 전면 광고 ----

        /// <summary>
        /// 한 판이 끝나고 화면을 전환할 때 호출한다.
        /// 조건을 만족하면 광고를 띄우고, 아니면 즉시 넘어간다.
        /// <paramref name="onDone"/>은 <b>어느 쪽이든 반드시 한 번 호출된다</b> —
        /// 호출부는 광고 여부로 분기할 필요가 없다.
        /// </summary>
        public static void ShowInterstitial(Action onDone)
        {
            if (_instance == null || !Enabled)
            {
                onDone?.Invoke();
                return;
            }
            _instance.ShowInterstitialInternal(onDone);
        }

        private void ShowInterstitialInternal(Action onDone)
        {
            _matchesSinceAd++;

            if (!ShouldShowInterstitial())
            {
                onDone?.Invoke();
                return;
            }

            _matchesSinceAd = 0;
            _lastInterstitialTime = Time.unscaledTime;
            _onInterstitialDone = onDone;

            AudioManager.PauseBgmForAd();
            _interstitial.Show();
        }

        private bool ShouldShowInterstitial()
        {
            if (_matchesSinceAd < MatchesPerInterstitial) return false;
            if (Time.unscaledTime - _lastInterstitialTime < MinIntervalSeconds) return false;

            if (_interstitial == null || !_interstitial.CanShowAd())
            {
                // 아직 안 실렸다. 판수는 유지되므로 다음 판에 다시 시도한다.
                LoadInterstitial();
                return false;
            }
            return true;
        }

        private void LoadInterstitial()
        {
            if (!Enabled) return;

            _interstitial?.Destroy();
            _interstitial = null;

            InterstitialAd.Load(InterstitialUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.Log($"[AdManager] 전면 광고 로드 실패: {error}");
                    return;
                }

                _interstitial = ad;
                ad.OnAdFullScreenContentClosed += FinishInterstitial;
                // 표시 자체가 실패해도 화면 전환은 되어야 한다.
                ad.OnAdFullScreenContentFailed += _ => FinishInterstitial();
            });
        }

        private void FinishInterstitial()
        {
            AudioManager.ResumeBgmAfterAd();

            Action done = _onInterstitialDone;
            _onInterstitialDone = null;

            LoadInterstitial(); // 다음 기회를 위해 미리 실어둔다
            done?.Invoke();
        }

        // ---- 보상형 광고 ----

        /// <summary>보상형 광고가 지금 바로 재생 가능한가(버튼 노출 여부 판단용).</summary>
        public static bool IsRewardedReady =>
            Enabled && _instance != null &&
            _instance._rewarded != null && _instance._rewarded.CanShowAd();

        /// <summary>
        /// 보상형 광고를 띄운다.
        /// <paramref name="onReward"/>는 <b>끝까지 본 경우에만</b> 호출된다.
        /// 중간에 닫거나 광고가 준비되지 않았으면 <paramref name="onUnavailable"/>이 호출된다.
        /// 둘 중 하나는 반드시 호출된다.
        /// </summary>
        public static void ShowRewarded(Action onReward, Action onUnavailable)
        {
            if (!IsRewardedReady)
            {
                _instance?.LoadRewarded(); // 다음 기회를 위해
                onUnavailable?.Invoke();
                return;
            }
            _instance.ShowRewardedInternal(onReward, onUnavailable);
        }

        private void ShowRewardedInternal(Action onReward, Action onUnavailable)
        {
            _onRewardEarned = onReward;
            _onRewardFailed = onUnavailable;
            _rewardEarned = false;

            // 보상형은 판이 진행되는 도중에 뜬다. 광고를 보는 동안 AI 턴 코루틴이
            // 계속 돌면 안 되므로 게임을 멈춘다.
            _timeScaleBeforeAd = Time.timeScale;
            Time.timeScale = 0f;

            AudioManager.PauseBgmForAd();
            _rewarded.Show(_ => _rewardEarned = true);
        }

        private void LoadRewarded()
        {
            if (!Enabled) return;

            _rewarded?.Destroy();
            _rewarded = null;

            RewardedAd.Load(RewardedUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.Log($"[AdManager] 보상형 광고 로드 실패: {error}");
                    return;
                }

                _rewarded = ad;
                ad.OnAdFullScreenContentClosed += FinishRewarded;
                ad.OnAdFullScreenContentFailed += _ => FinishRewarded();
            });
        }

        private void FinishRewarded()
        {
            Time.timeScale = _timeScaleBeforeAd;
            AudioManager.ResumeBgmAfterAd();

            Action reward = _onRewardEarned;
            Action failed = _onRewardFailed;
            _onRewardEarned = null;
            _onRewardFailed = null;

            bool earned = _rewardEarned;
            _rewardEarned = false;

            LoadRewarded(); // 다음 기회를 위해 미리 실어둔다

            if (earned) reward?.Invoke();
            else failed?.Invoke();
        }
    }
}
