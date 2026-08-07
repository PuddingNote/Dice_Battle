using System;
using UnityEngine;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 코인 지갑과 날짜에 묶인 상태를 PlayerPrefs에 저장/로드한다.
    /// <see cref="PlayerStats"/>와 같은 방식(키 하나 + JSON 한 덩어리)이다.
    ///
    /// "오늘"이 무엇인지 아는 곳은 여기 하나뿐이다. <see cref="WalletData"/>는 날짜 번호를
    /// 인자로만 받으므로 테스트에서 아무 날짜나 넣어 볼 수 있다.
    /// </summary>
    public static class PlayerWallet
    {
        private const string WalletKey = "dicebattle.wallet";

        private static WalletData _data;

        public static WalletData Data => _data ??= Load();

        /// <summary>
        /// 한국 시간 자정 기준 오늘.
        /// 기기 시간대와 무관하게 모두 같은 시점에 초기화되도록 UtcNow를 넘긴다.
        /// </summary>
        public static int Today
        {
            get
            {
                int today = WalletData.Today(DateTime.UtcNow);
#if UNITY_EDITOR
                today += EditorDayOffset; // 에디터에서 날짜를 밀어 볼 때만 0이 아니다
#endif
                return today;
            }
        }

#if UNITY_EDITOR
        private const string DayOffsetKey = "dicebattle.editor.dayoffset";

        /// <summary>
        /// 에디터 전용: "오늘"을 며칠 밀어서 볼지.
        ///
        /// 출석은 하루 한 번뿐이라 2일차 이후를 확인하려면 날짜가 지나야 하는데,
        /// 실제로 기다릴 수도 없고 윈도우 시계를 만지면 다른 프로그램까지 영향을 받는다.
        /// 그래서 게임이 보는 "오늘"만 밀어 준다. 보호권의 하루 제한에도 그대로 적용된다.
        ///
        /// PlayerPrefs에 담아 두므로 재컴파일이나 플레이 모드 진입에도 남는다.
        /// UNITY_EDITOR 밖에서는 이 값 자체가 존재하지 않는다.
        /// </summary>
        public static int EditorDayOffset
        {
            get => PlayerPrefs.GetInt(DayOffsetKey, 0);
            set
            {
                PlayerPrefs.SetInt(DayOffsetKey, value);
                PlayerPrefs.Save();
            }
        }
#endif

        public static int Coins => Data.coins;

        // ---- 코인 ----

        /// <summary>한 판 결과로 코인을 지급하고 지급액을 돌려준다.</summary>
        public static int GrantMatchReward(PlayerMatchResult result, int playedLevel)
        {
            int amount = CoinRules.CoinsFor(result, PlayerProgress.Tier(playedLevel));
            Data.AddCoins(amount);
            Save();
            return amount;
        }

        // ---- 출석 ----

        public static bool CanClaimAttendance => Data.CanClaimAttendance(Today);

        /// <summary>
        /// 이번 주에 다음으로 받을 칸(0부터). 출석 창이 어느 칸을 강조할지 정한다.
        /// 일곱 칸을 다 받았으면 칸 수와 같은 값이 나온다.
        /// </summary>
        public static int AttendanceIndex => Data.AttendanceIndexIn(Today);

        /// <summary>수령액을 돌려준다. 받을 수 없으면 0.</summary>
        public static int ClaimAttendance()
        {
            int reward = Data.ClaimAttendance(Today);
            if (reward > 0) Save();
            return reward;
        }

        // ---- 패배 보호권 ----

        public static int ProtectionPrice(int level)
            => CoinRules.ProtectionPrice(PlayerProgress.Tier(level));

        /// <summary>
        /// 지금 코인으로 보호권을 쓸 수 있는가.
        /// 잔액과 하루 1회 제한을 모두 만족해야 한다.
        /// </summary>
        public static bool CanUseCoinProtection(int level)
            => Data.CanUseCoinProtection(Today) && Data.CanAfford(ProtectionPrice(level));

        /// <summary>성공하면 코인을 차감하고 true. 점수 복구는 호출부가 한다.</summary>
        public static bool TryUseCoinProtection(int level)
        {
            bool used = Data.TryUseCoinProtection(Today, ProtectionPrice(level));
            if (used) Save();
            return used;
        }

        /// <summary>광고로 쓰는 보호권이 오늘 남아 있는가. 코인 제한과는 별개로 센다.</summary>
        public static bool CanUseAdProtection => Data.CanUseAdProtection(Today);

        /// <summary>광고 시청이 끝난 뒤에 부른다. 점수 복구는 호출부가 한다.</summary>
        public static bool TryUseAdProtection()
        {
            bool used = Data.TryUseAdProtection(Today);
            if (used) Save();
            return used;
        }

        // ---- 승리 코인 2배 ----

        public static bool CanDoubleReward => Data.CanDoubleReward(Today);

        /// <summary>오늘 남은 2배 횟수.</summary>
        public static int DoubleRewardsLeft
            => CoinRules.DailyDoubleRewardLimit - Data.DoubleRewardUsedToday(Today);

        /// <summary>
        /// 광고 시청이 끝난 뒤에 부른다. 이미 받은 액수만큼 한 번 더 지급하고
        /// 그 추가분을 돌려준다. 한도를 넘었으면 0.
        /// </summary>
        public static int GrantDoubleReward(int alreadyGranted)
        {
            if (!Data.TryUseDoubleReward(Today)) return 0;

            int bonus = CoinRules.DoubleRewardBonus(alreadyGranted);
            Data.AddCoins(bonus);
            Save();
            return bonus;
        }

        // ---- 저장 ----

        private static WalletData Load()
        {
            string json = PlayerPrefs.GetString(WalletKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return new WalletData();

            WalletData loaded = null;
            try
            {
                loaded = JsonUtility.FromJson<WalletData>(json);
            }
            catch
            {
                // 지갑 하나 깨졌다고 게임이 멈추면 안 된다. 진행도는 별개 키라 멀쩡하다.
                Debug.LogWarning("[PlayerWallet] 지갑 데이터를 읽지 못해 새로 시작합니다.");
            }

            if (loaded == null) return new WalletData();

            loaded.Repair();
            return loaded;
        }

        private static void Save()
        {
            Data.Repair();
            PlayerPrefs.SetString(WalletKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        /// <summary>디버그/테스트용 초기화.</summary>
        public static void Reset()
        {
            _data = new WalletData();
            Save();
        }

#if UNITY_EDITOR
        /// <summary>에디터 전용: 코인을 직접 지정한다.</summary>
        public static void EditorSetCoins(int coins)
        {
            Data.coins = coins < 0 ? 0 : coins;
            Save();
        }

        /// <summary>에디터 전용: 오늘 쓴 보호권·출석·2배 기록을 지워 다시 쓸 수 있게 한다.</summary>
        public static void EditorClearDailyLimits()
        {
            Data.coinProtectDay = WalletData.NeverClaimed;
            Data.adProtectDay = WalletData.NeverClaimed;
            Data.lastAttendanceDay = WalletData.NeverClaimed;
            Data.doubleRewardDay = WalletData.NeverClaimed;
            Data.doubleRewardCount = 0;
            Save();
        }
#endif
    }
}
