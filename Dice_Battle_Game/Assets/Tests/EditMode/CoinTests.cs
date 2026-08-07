using NUnit.Framework;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    /// <summary>
    /// 코인 획득량·보호권 가격과 지갑의 날짜 규칙.
    /// 저장(PlayerPrefs)은 UI 계층이라 다루지 않는다.
    /// </summary>
    public class CoinTests
    {
        /// <summary>실제 밸런스 곡선. 표에 적어 둔 값과 어긋나면 여기서 잡힌다.</summary>
        private static DifficultyTable Table() => DifficultyCurve.Default.Build();

        // ---- 획득량 ----

        [Test]
        public void Win_Coins_Match_The_Balance_Table()
        {
            var table = Table();
            int[] expected = { 2, 3, 4, 5, 7, 9, 12, 16, 22, 30 };

            for (int i = 0; i < expected.Length; i++)
            {
                int level = DifficultyTable.MinLevel + i;
                Assert.AreEqual(expected[i], CoinRules.WinCoins(table[level]), $"Lv.{level}");
            }
        }

        [Test]
        public void Lose_Coins_Match_The_Balance_Table()
        {
            var table = Table();
            int[] expected = { 1, 1, 1, 2, 2, 3, 4, 5, 7, 9 };

            for (int i = 0; i < expected.Length; i++)
            {
                int level = DifficultyTable.MinLevel + i;
                Assert.AreEqual(expected[i], CoinRules.LoseCoins(table[level]), $"Lv.{level}");
            }
        }

        [Test]
        public void A_Draw_Pays_The_Same_As_A_Loss()
        {
            var table = Table();
            for (int level = DifficultyTable.MinLevel; level <= DifficultyTable.MaxLevel; level++)
                Assert.AreEqual(CoinRules.LoseCoins(table[level]), CoinRules.DrawCoins(table[level]));
        }

        [Test]
        public void Higher_Levels_Never_Pay_Less()
        {
            var table = Table();
            for (int level = DifficultyTable.MinLevel + 1; level <= DifficultyTable.MaxLevel; level++)
            {
                Assert.GreaterOrEqual(
                    CoinRules.WinCoins(table[level]), CoinRules.WinCoins(table[level - 1]),
                    "낮은 난이도를 파밍하는 것이 이득이 되면 안 된다");
            }
        }

        [Test]
        public void Every_Level_Pays_At_Least_One_Coin()
        {
            var table = Table();
            for (int level = DifficultyTable.MinLevel; level <= DifficultyTable.MaxLevel; level++)
            {
                Assert.GreaterOrEqual(CoinRules.LoseCoins(table[level]), 1);
                Assert.GreaterOrEqual(CoinRules.WinCoins(table[level]), 1);
            }
        }

        // ---- 보호권 가격 ----

        [Test]
        public void Protection_Price_Match_The_Balance_Table()
        {
            var table = Table();
            int[] expected = { 40, 40, 80, 80, 120, 160, 200, 280, 400, 560 };

            for (int i = 0; i < expected.Length; i++)
            {
                int level = DifficultyTable.MinLevel + i;
                Assert.AreEqual(expected[i], CoinRules.ProtectionPrice(table[level]), $"Lv.{level}");
            }
        }

        [Test]
        public void Price_Per_Protected_Point_Is_Constant()
        {
            var table = Table();

            // 가격이 차감 점수에 비례해야 "막아 주는 점수 대비 가격"이 전 구간 같다.
            // 고정 가격이면 상위 난이도에서 코인이 남아돌아 화폐가 무의미해진다.
            for (int level = DifficultyTable.MinLevel; level <= DifficultyTable.MaxLevel; level++)
            {
                var tier = table[level];
                double ratio = (double)CoinRules.ProtectionPrice(tier) / tier.LosePoints;
                Assert.AreEqual(4d, ratio, 1e-9, $"Lv.{level}");
            }
        }

        // ---- 지갑: 코인 ----

        [Test]
        public void Spending_Fails_When_Short_And_Leaves_The_Balance_Alone()
        {
            var w = new WalletData();
            w.AddCoins(30);

            Assert.IsFalse(w.TrySpend(40));
            Assert.AreEqual(30, w.coins);
        }

        [Test]
        public void Spending_Deducts_Exactly_The_Price()
        {
            var w = new WalletData();
            w.AddCoins(100);

            Assert.IsTrue(w.TrySpend(40));
            Assert.AreEqual(60, w.coins);
        }

        // ---- 지갑: 출석 ----

        /// <summary>그 주 월요일의 날짜 번호. 2020-01-06(월)이 5번이다.</summary>
        private static int Monday(int weeksAfterFirst = 0) => 5 + weeksAfterFirst * 7;

        [Test]
        public void Attendance_Can_Be_Claimed_Once_Per_Day()
        {
            var w = new WalletData();
            int mon = Monday();

            Assert.AreEqual(5, w.ClaimAttendance(mon), "1일차는 5코인");
            Assert.AreEqual(0, w.ClaimAttendance(mon), "같은 날 두 번은 안 된다");
            Assert.AreEqual(5, w.ClaimAttendance(mon + 1), "다음 날은 다시 받는다");
        }

        [Test]
        public void Attendance_Walks_The_Week_In_Order()
        {
            var w = new WalletData();
            int mon = Monday();
            int[] expected = { 5, 5, 10, 10, 15, 15, 40 };

            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], w.ClaimAttendance(mon + i), $"{i + 1}일차");

            Assert.AreEqual(100, w.coins, "한 주를 다 채우면 100코인");
        }

        [Test]
        public void Missing_A_Day_Continues_Instead_Of_Restarting()
        {
            var w = new WalletData();
            int mon = Monday();

            w.ClaimAttendance(mon);         // 1일차
            w.ClaimAttendance(mon + 1);     // 2일차

            // 수요일을 건너뛰고 목요일에 와도 3일차부터 이어 받는다.
            Assert.AreEqual(10, w.ClaimAttendance(mon + 3), "빠진 날 때문에 되돌아가지 않는다");
        }

        [Test]
        public void Progress_Resets_On_Monday()
        {
            var w = new WalletData();
            int mon = Monday();

            w.ClaimAttendance(mon);         // 1일차 (5)
            w.ClaimAttendance(mon + 1);     // 2일차 (5)
            w.ClaimAttendance(mon + 2);     // 3일차 (10)

            // 일요일까지는 이어진다.
            Assert.AreEqual(10, w.ClaimAttendance(mon + 6), "같은 주라 4일차");

            // 다음 월요일에 1일차로 돌아간다.
            Assert.AreEqual(5, w.ClaimAttendance(Monday(1)), "월요일이면 처음부터");
        }

        [Test]
        public void A_Full_Week_Caps_At_One_Hundred()
        {
            var w = new WalletData();
            int mon = Monday();

            // 월~일 일곱 날. 수령은 날짜가 엄격히 증가해야 하므로 한 주에 일곱 번이 최대다.
            for (int i = 0; i < CoinRules.AttendanceCycleLength; i++)
                w.ClaimAttendance(mon + i);

            Assert.AreEqual(100, w.coins, "한 주 상한은 100코인");
            Assert.AreEqual(CoinRules.AttendanceCycleLength, w.AttendanceIndexIn(mon + 6),
                "일곱 칸을 다 받으면 남은 칸이 없다");
            Assert.IsFalse(w.CanClaimAttendance(mon + 6), "같은 날 재수령은 안 된다");
        }

        [Test]
        public void A_Corrupted_Index_Does_Not_Wrap_Around_To_Day_One()
        {
            var w = new WalletData();
            int mon = Monday();
            w.ClaimAttendance(mon); // 1일차만 받은 상태

            // 저장본이 손상돼 "이번 주 다 받음"으로 적혀 있는 경우.
            // 상한 검사가 없으면 AttendanceReward(7)이 0번 칸으로 감겨
            // 같은 주에 1일차 보상이 또 나온다.
            w.attendanceIndex = CoinRules.AttendanceCycleLength;

            Assert.IsFalse(w.CanClaimAttendance(mon + 1));
            Assert.AreEqual(0, w.ClaimAttendance(mon + 1));
            Assert.AreEqual(5, w.coins, "처음 받은 5코인 그대로");
        }

        [Test]
        public void The_Next_Monday_Opens_Day_One_Again()
        {
            var w = new WalletData();
            int mon = Monday();

            for (int i = 0; i < CoinRules.AttendanceCycleLength; i++)
                w.ClaimAttendance(mon + i);

            // 한 주를 꽉 채운 바로 다음 날이 곧 다음 월요일이다.
            Assert.IsTrue(w.CanClaimAttendance(Monday(1)));
            Assert.AreEqual(5, w.ClaimAttendance(Monday(1)));
            Assert.AreEqual(105, w.coins);
        }

        [Test]
        public void Skipping_A_Whole_Week_Still_Restarts_At_Day_One()
        {
            var w = new WalletData();

            w.ClaimAttendance(Monday());     // 1일차
            w.ClaimAttendance(Monday() + 1); // 2일차

            // 몇 주를 통째로 쉬고 돌아와도 1일차부터다.
            Assert.AreEqual(5, w.ClaimAttendance(Monday(5)));
        }

        [Test]
        public void Turning_The_Clock_Back_Does_Not_Grant_Another_Reward()
        {
            var w = new WalletData();
            int mon = Monday(10);
            w.ClaimAttendance(mon);

            Assert.AreEqual(0, w.ClaimAttendance(mon - 50), "과거로 돌린 날짜로는 받을 수 없다");
            Assert.AreEqual(0, w.ClaimAttendance(mon));
            Assert.AreEqual(5, w.ClaimAttendance(mon + 1), "실제로 하루가 지나야 받는다");
        }

        // ---- 날짜·주 계산 ----

        [Test]
        public void Week_Rolls_Over_On_Monday_Not_Sunday()
        {
            // 2020-01-06은 월요일이다.
            int monday = WalletData.Today(new System.DateTime(2020, 1, 6, 3, 0, 0));
            int sunday = monday - 1;
            int nextMonday = monday + 7;

            Assert.AreEqual(WalletData.WeekOf(monday), WalletData.WeekOf(monday + 6),
                "월~일은 같은 주다");
            Assert.AreNotEqual(WalletData.WeekOf(sunday), WalletData.WeekOf(monday),
                "일요일과 그 다음 월요일은 다른 주다");
            Assert.AreEqual(WalletData.WeekOf(monday) + 1, WalletData.WeekOf(nextMonday));
        }

        [Test]
        public void The_Day_Rolls_Over_At_Korean_Midnight()
        {
            // UTC 14:59 = KST 23:59 (같은 날), UTC 15:00 = KST 다음 날 00:00.
            var beforeUtc = new System.DateTime(2026, 8, 6, 14, 59, 0);
            var afterUtc = new System.DateTime(2026, 8, 6, 15, 0, 0);

            Assert.AreEqual(WalletData.Today(beforeUtc) + 1, WalletData.Today(afterUtc),
                "한국 시간 자정에 날짜가 바뀐다");
        }

        [Test]
        public void The_Day_Does_Not_Roll_Over_At_Utc_Midnight()
        {
            var lateUtc = new System.DateTime(2026, 8, 6, 23, 0, 0);   // KST 8/7 08:00
            var earlyUtc = new System.DateTime(2026, 8, 7, 1, 0, 0);   // KST 8/7 10:00

            Assert.AreEqual(WalletData.Today(lateUtc), WalletData.Today(earlyUtc),
                "UTC 자정은 한국 시간으로 아침이라 날짜가 바뀌면 안 된다");
        }

        // ---- 지갑: 보호권 ----

        [Test]
        public void Coin_Protection_Is_Once_Per_Day()
        {
            var w = new WalletData();
            w.AddCoins(1000);

            Assert.IsTrue(w.TryUseCoinProtection(10, 120));
            Assert.IsFalse(w.TryUseCoinProtection(10, 120), "같은 날 두 번은 안 된다");
            Assert.IsTrue(w.TryUseCoinProtection(11, 120), "다음 날은 다시 쓴다");
            Assert.AreEqual(760, w.coins);
        }

        [Test]
        public void Failed_Protection_Does_Not_Consume_The_Daily_Use()
        {
            var w = new WalletData();
            w.AddCoins(50);

            Assert.IsFalse(w.TryUseCoinProtection(10, 120), "잔액 부족");
            Assert.AreEqual(50, w.coins, "코인이 줄면 안 된다");

            w.AddCoins(100);
            Assert.IsTrue(w.TryUseCoinProtection(10, 120), "실패는 하루 사용을 소모하지 않는다");
        }

        [Test]
        public void Ad_Protection_Is_Counted_Separately_From_Coin_Protection()
        {
            var w = new WalletData();
            w.AddCoins(1000);

            w.TryUseCoinProtection(10, 120);

            // 코인 1회 + 광고 1회 = 하루 2회. 서로의 제한을 잡아먹지 않는다.
            Assert.IsTrue(w.CanUseAdProtection(10));
            Assert.IsTrue(w.TryUseAdProtection(10));
            Assert.IsFalse(w.TryUseAdProtection(10));
        }

        // ---- 승리 코인 2배 광고 ----

        [Test]
        public void Double_Reward_Is_Limited_Per_Day()
        {
            var w = new WalletData();
            int day = Monday();

            for (int i = 0; i < CoinRules.DailyDoubleRewardLimit; i++)
                Assert.IsTrue(w.TryUseDoubleReward(day), $"{i + 1}번째");

            Assert.IsFalse(w.CanDoubleReward(day), "한도를 넘으면 막힌다");
            Assert.IsFalse(w.TryUseDoubleReward(day));
        }

        [Test]
        public void Double_Reward_Count_Resets_The_Next_Day()
        {
            var w = new WalletData();
            int day = Monday();

            for (int i = 0; i < CoinRules.DailyDoubleRewardLimit; i++)
                w.TryUseDoubleReward(day);

            Assert.AreEqual(0, w.DoubleRewardUsedToday(day + 1), "날짜가 바뀌면 0부터");
            Assert.IsTrue(w.CanDoubleReward(day + 1));
        }

        [Test]
        public void Double_Reward_Is_Counted_Apart_From_Protection()
        {
            var w = new WalletData();
            int day = Monday();
            w.AddCoins(1000);

            w.TryUseCoinProtection(day, 120);
            w.TryUseAdProtection(day);

            // 보호권을 둘 다 썼어도 2배는 그대로 남아 있어야 한다.
            Assert.IsTrue(w.CanDoubleReward(day));
            Assert.AreEqual(CoinRules.DailyDoubleRewardLimit, CoinRules.DailyDoubleRewardLimit
                - w.DoubleRewardUsedToday(day));
        }

        [Test]
        public void Doubling_Pays_The_Same_Amount_Again()
        {
            var table = Table();

            // Lv.10 승리는 30코인이고, 2배면 30을 한 번 더 받아 60이 된다.
            int granted = CoinRules.WinCoins(table[DifficultyTable.MaxLevel]);
            Assert.AreEqual(granted, CoinRules.DoubleRewardBonus(granted));
        }

        // ---- 손상 데이터 ----

        [Test]
        public void Repair_Fixes_Negative_Coins_And_Out_Of_Range_Cycle()
        {
            var w = new WalletData();
            w.coins = -50;
            w.attendanceIndex = 99;

            w.Repair();

            Assert.AreEqual(0, w.coins);
            // 칸 수와 같은 값은 "이번 주 다 받음"이라는 뜻이라 허용된다.
            Assert.LessOrEqual(w.attendanceIndex, CoinRules.AttendanceCycleLength);
            Assert.GreaterOrEqual(w.attendanceIndex, 0);
        }

        [Test]
        public void A_Fresh_Wallet_Can_Claim_And_Protect_Immediately()
        {
            var w = new WalletData();

            Assert.IsTrue(w.CanClaimAttendance(0), "첫 실행에도 출석은 받을 수 있어야 한다");
            Assert.IsTrue(w.CanUseCoinProtection(0));
            Assert.IsTrue(w.CanUseAdProtection(0));
        }

        [Test]
        public void The_Same_Korean_Day_Has_One_Day_Number()
        {
            // 둘 다 KST 2026-08-07 (UTC 15:00 / 다음 날 UTC 14:00)
            var justAfterMidnight = new System.DateTime(2026, 8, 6, 15, 0, 0);
            var justBeforeMidnight = new System.DateTime(2026, 8, 7, 14, 59, 0);

            Assert.AreEqual(WalletData.Today(justAfterMidnight),
                WalletData.Today(justBeforeMidnight),
                "한국 시간으로 같은 날이면 같은 날짜 번호");
        }
    }
}
