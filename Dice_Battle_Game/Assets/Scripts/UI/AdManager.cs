using System;
using System.Collections;
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

        // ---- 광고 신선도 ----

        /// <summary>
        /// 실어둔 광고를 이 시간이 지나면 버리고 다시 싣는다.
        ///
        /// <b>AdMob 광고는 약 1시간이 지나면 만료된다.</b> 문제는 만료를 물어볼 방법이
        /// 없다는 것이다 — <c>CanShowAd()</c>는 객체가 살아 있기만 하면 true를 준다.
        /// 만료된 광고로 Show를 부르면 광고 컨테이너는 열리는데 내용이 안 실려서
        /// <b>닫기 버튼이 없는 화면에 갇힌다.</b> 앱을 강제 종료하는 것 말고 나갈 방법이 없다.
        ///
        /// 게임을 켜 두고 한참 뒤에 처음 광고를 보는 흐름에서 이 일이 벌어진다.
        /// 리롤·보호권·2배 보상은 가끔 쓰는 기능이라 특히 오래 묵는다.
        /// 만료 시각을 정확히 알 수 없으므로 여유를 두고 1시간보다 짧게 잡는다.
        /// </summary>
        private const float MaxAdAgeSeconds = 50f * 60f;

        /// <summary>
        /// 광고를 닫은 뒤 종료 콜백을 이만큼 기다려 본다. 그래도 안 오면 잃어버린 것으로 본다.
        /// 포커스가 먼저 돌아오고 콜백이 뒤따라오는 순서가 흔해서 곧바로 판단하면 안 된다.
        /// </summary>
        private const float CallbackGraceSeconds = 1.5f;

        /// <summary>
        /// 실어둔 광고가 아직 쓸 만한지 확인하는 주기.
        ///
        /// 앱을 켜 둔 채로도 광고는 만료되므로 돌아올 때만 확인해서는 부족하다.
        /// 특히 보상형은 <see cref="IsRewardedReady"/>가 리롤 버튼의 노출을 정하는데,
        /// 만료됐다고 숨기기만 하고 다시 싣지 않으면 <b>버튼이 그 판 내내 사라진다.</b>
        /// </summary>
        private const float FreshnessCheckSeconds = 60f;

        /// <summary>
        /// 에디터에서 광고를 띄울지. 실제 광고 대신 회색 자리표시 화면이 뜬다.
        /// 켜두면 보상형뿐 아니라 <b>전면 광고도 뜬다</b> — 3판 조건이 차면
        /// 결과 화면을 나갈 때 자동으로 끼어든다. 연출 테스트에 방해되면 false로 바꾼다.
        /// </summary>
        private const bool EnableInEditor = true;

        private static AdManager _instance;

        private InterstitialAd _interstitial;
        private RewardedAd _rewarded;

        /// <summary>각 광고를 실어둔 시각(<see cref="Time.realtimeSinceStartup"/>).</summary>
        private float _interstitialLoadedAt = float.NegativeInfinity;
        private float _rewardedLoadedAt = float.NegativeInfinity;

        /// <summary>요청을 보내고 응답을 기다리는 중. 주기 점검이 같은 요청을 겹쳐 쏘는 것을 막는다.</summary>
        private bool _interstitialLoading;
        private bool _rewardedLoading;

        /// <summary>지금 화면에 떠 있는 전체 화면 광고의 종류.</summary>
        private enum FullscreenAd { None, Interstitial, Rewarded }

        /// <summary>
        /// 표시 중인 광고. <b>두 가지를 동시에 막는다:</b>
        /// 이미 떠 있는 광고를 다시 싣느라 <c>Destroy()</c> 해 버리는 것과,
        /// 종료 콜백이 두 번 들어와 정산이 두 번 도는 것.
        /// </summary>
        private FullscreenAd _showing = FullscreenAd.None;

        /// <summary>표시 중인 광고가 실제로 앱을 백그라운드로 밀어냈는가(워치독 발동 조건).</summary>
        private bool _adTookOverScreen;

        /// <summary>마지막으로 끝난 판수(전면 광고 조건).</summary>
        private int _matchesSinceAd;

        /// <summary>
        /// 마지막으로 <b>전체 화면 광고</b>를 띄운 시각. 전면과 보상형을 함께 센다.
        /// 보상형을 빼면 결과 화면에서 광고를 보고 나가는 순간 전면이 겹친다.
        /// </summary>
        private float _lastFullscreenAdTime = float.NegativeInfinity;

        private Action _onInterstitialDone;
        private Action _onRewardEarned;
        private Action _onRewardFailed;
        private bool _rewardEarned;
        private float _timeScaleBeforeAd = 1f;
        private Coroutine _watchdog;

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

        private static float Now => Time.realtimeSinceStartup;

        private static bool IsFresh(float loadedAt) => Now - loadedAt < MaxAdAgeSeconds;

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

            StartCoroutine(KeepAdsFresh());
        }

        /// <summary>실어둔 광고가 묵지 않도록 주기적으로 갈아 끼운다.</summary>
        private IEnumerator KeepAdsFresh()
        {
            var wait = new WaitForSecondsRealtime(FreshnessCheckSeconds);
            while (true)
            {
                yield return wait;
                RefreshAds();
            }
        }

        private void OnDestroy()
        {
            _interstitial?.Destroy();
            _rewarded?.Destroy();
            if (_instance == this) _instance = null;
        }

        // ---- 앱 생명주기 ----

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) OnReturnedToForeground();
            else OnLeftForeground();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) OnLeftForeground();
            else OnReturnedToForeground();
        }

        private void OnLeftForeground()
        {
            // 광고가 화면을 가져갔다는 표시. 이게 있어야 "돌아왔다"를 신뢰할 수 있다.
            if (_showing != FullscreenAd.None) _adTookOverScreen = true;
        }

        /// <summary>
        /// 앱으로 돌아왔을 때. 광고를 보고 돌아온 경우와 홈 버튼을 눌렀다 온 경우가 섞여 있다.
        /// </summary>
        private void OnReturnedToForeground()
        {
            if (!Enabled) return;

            // 광고가 화면을 가져갔다가 돌아왔는데도 표시 중으로 남아 있다면 콜백을 의심한다.
            //
            // <b>화면을 가져간 적이 있을 때만 확인한다.</b> Show를 부른 직후 광고 화면이
            // 올라오기 전에 포커스가 한 번 튀는 일이 있는데, 그것만 보고 판단하면
            // 멀쩡히 재생 중인 광고를 끊고 "광고를 못 봤다"로 처리해 버린다.
            if (_adTookOverScreen && _showing != FullscreenAd.None && _watchdog == null)
                _watchdog = StartCoroutine(RecoverIfCallbackLost());

            // 오래 자리를 비웠으면 실어둔 광고가 만료됐을 수 있다. 미리 갈아둔다.
            RefreshAds();
        }

        /// <summary>
        /// 종료 콜백이 오지 않으면 강제로 정리한다.
        ///
        /// 이게 없으면 보상형 광고에서 콜백을 잃는 순간 <see cref="Time.timeScale"/>이 0에
        /// 갇혀 게임이 영영 멈춘다. 광고가 왜 깨졌든 간에, 돌아온 뒤에 게임까지 죽어 있으면
        /// 안 된다.
        /// </summary>
        private IEnumerator RecoverIfCallbackLost()
        {
            yield return new WaitForSecondsRealtime(CallbackGraceSeconds);
            _watchdog = null;

            if (_showing == FullscreenAd.None) yield break; // 정상적으로 닫혔다

            Debug.LogWarning($"[AdManager] {_showing} 광고의 종료 콜백이 오지 않았다. 강제로 정리한다.");

            if (_showing == FullscreenAd.Rewarded) FinishRewarded();
            else FinishInterstitial();
        }

        /// <summary>
        /// 광고를 닫자마자 같은 프레임에서 다음 광고를 요청하지 않고 살짝 늦춘다.
        ///
        /// 리롤 → 2배 보상 → 전면 광고처럼 짧은 간격으로 반복해 부르는 상황에서
        /// 간헐적으로 광고 화면이 멈추는 문제가 있었다. 방금 닫힌 광고의 네이티브 쪽
        /// 정리(액티비티 종료, WebView 해제)가 끝나기 전에 새 요청이 겹치면 SDK 내부에서
        /// 꼬일 여지가 있다고 보고, 한 프레임 이상의 여유를 준다.
        /// </summary>
        private static IEnumerator ReloadAfterClose(Action load)
        {
            yield return null; // 최소 한 프레임
            yield return new WaitForSecondsRealtime(0.3f);
            load();
        }

        /// <summary>
        /// 묵었거나 아직 없는 광고를 다시 싣는다.
        /// 없는 경우까지 챙기는 이유는, 시작 직후 로드가 한 번 실패하면 그대로 비어 있어
        /// 보상형 버튼이 끝까지 안 나오기 때문이다.
        /// </summary>
        private void RefreshAds()
        {
            if (!Enabled || _showing != FullscreenAd.None) return;

            if (_interstitial == null || !IsFresh(_interstitialLoadedAt)) LoadInterstitial();
            if (_rewarded == null || !IsFresh(_rewardedLoadedAt)) LoadRewarded();
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
            _lastFullscreenAdTime = Time.unscaledTime;
            _onInterstitialDone = onDone;
            _showing = FullscreenAd.Interstitial;
            _adTookOverScreen = false;

            AudioManager.PauseBgmForAd();
            _interstitial.Show();
        }

        private bool ShouldShowInterstitial()
        {
            if (_showing != FullscreenAd.None) return false;
            if (_matchesSinceAd < MatchesPerInterstitial) return false;
            if (Time.unscaledTime - _lastFullscreenAdTime < MinIntervalSeconds) return false;

            // 아직 안 실렸거나 너무 오래 묵었다. 판수는 유지되므로 다음 판에 다시 시도한다.
            if (_interstitial == null || !_interstitial.CanShowAd() || !IsFresh(_interstitialLoadedAt))
            {
                LoadInterstitial();
                return false;
            }
            return true;
        }

        private void LoadInterstitial()
        {
            if (!Enabled || _interstitialLoading) return;
            // 떠 있는 광고를 파괴하면 종료 콜백까지 같이 사라진다.
            if (_showing == FullscreenAd.Interstitial) return;

            _interstitial?.Destroy();
            _interstitial = null;
            _interstitialLoadedAt = float.NegativeInfinity;
            _interstitialLoading = true;

            InterstitialAd.Load(InterstitialUnitId, new AdRequest(), (ad, error) =>
            {
                _interstitialLoading = false;

                if (error != null || ad == null)
                {
                    Debug.Log($"[AdManager] 전면 광고 로드 실패: {error}");
                    return;
                }

                _interstitial = ad;
                _interstitialLoadedAt = Now;
                ad.OnAdFullScreenContentClosed += FinishInterstitial;
                // 표시 자체가 실패해도 화면 전환은 되어야 한다.
                ad.OnAdFullScreenContentFailed += _ => FinishInterstitial();
            });
        }

        private void FinishInterstitial()
        {
            // 닫힘과 실패가 둘 다 들어오거나, 워치독과 콜백이 겹칠 수 있다.
            if (_showing != FullscreenAd.Interstitial) return;
            _showing = FullscreenAd.None;
            _adTookOverScreen = false;

            AudioManager.ResumeBgmAfterAd();

            Action done = _onInterstitialDone;
            _onInterstitialDone = null;

            StartCoroutine(ReloadAfterClose(LoadInterstitial)); // 다음 기회를 위해 미리 실어둔다
            done?.Invoke();
        }

        // ---- 보상형 광고 ----

        /// <summary>보상형 광고가 지금 바로 재생 가능한가(버튼 노출 여부 판단용).</summary>
        public static bool IsRewardedReady =>
            Enabled && _instance != null &&
            _instance._showing == FullscreenAd.None &&
            _instance._rewarded != null && _instance._rewarded.CanShowAd() &&
            IsFresh(_instance._rewardedLoadedAt);

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
            _showing = FullscreenAd.Rewarded;
            _adTookOverScreen = false;

            // 보상형은 판이 진행되는 도중에 뜬다. 광고를 보는 동안 AI 턴 코루틴이
            // 계속 돌면 안 되므로 게임을 멈춘다.
            _timeScaleBeforeAd = Time.timeScale;
            Time.timeScale = 0f;

            AudioManager.PauseBgmForAd();
            _rewarded.Show(_ => _rewardEarned = true);
        }

        private void LoadRewarded()
        {
            if (!Enabled || _rewardedLoading) return;
            // 떠 있는 광고를 파괴하면 종료 콜백까지 같이 사라진다.
            if (_showing == FullscreenAd.Rewarded) return;

            _rewarded?.Destroy();
            _rewarded = null;
            _rewardedLoadedAt = float.NegativeInfinity;
            _rewardedLoading = true;

            RewardedAd.Load(RewardedUnitId, new AdRequest(), (ad, error) =>
            {
                _rewardedLoading = false;

                if (error != null || ad == null)
                {
                    Debug.Log($"[AdManager] 보상형 광고 로드 실패: {error}");
                    return;
                }

                _rewarded = ad;
                _rewardedLoadedAt = Now;
                ad.OnAdFullScreenContentClosed += FinishRewarded;
                ad.OnAdFullScreenContentFailed += _ => FinishRewarded();
            });
        }

        private void FinishRewarded()
        {
            if (_showing != FullscreenAd.Rewarded) return;
            _showing = FullscreenAd.None;
            _adTookOverScreen = false;

            Time.timeScale = _timeScaleBeforeAd;
            AudioManager.ResumeBgmAfterAd();

            // 보상형도 전면 광고와 같은 전체 화면 광고다. 방금 하나 봤다는 사실을
            // 기록해 두지 않으면, 결과 화면에서 광고를 보고 "계속하기"를 누르는 순간
            // 전면 광고가 연달아 떠서 광고를 두 번 연속 보게 된다.
            _lastFullscreenAdTime = Time.unscaledTime;

            Action reward = _onRewardEarned;
            Action failed = _onRewardFailed;
            _onRewardEarned = null;
            _onRewardFailed = null;

            bool earned = _rewardEarned;
            _rewardEarned = false;

            StartCoroutine(ReloadAfterClose(LoadRewarded)); // 다음 기회를 위해 미리 실어둔다

            if (earned) reward?.Invoke();
            else failed?.Invoke();
        }
    }
}
