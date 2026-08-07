using System;

namespace DiceBattle.Core
{
    /// <summary>
    /// 코인 잔액과 날짜에 묶인 상태(출석, 오늘 쓴 보호권).
    /// <see cref="StatsData"/>와 같이 JSON 한 덩어리로 저장되므로 필드는 전부 public이다.
    ///
    /// <b>"오늘"은 밖에서 넣어 준다.</b> 안에서 DateTime.Now를 부르면 테스트에서 날짜를
    /// 넘길 수가 없다. UI 계층이 <see cref="Today"/>로 만든 날짜 번호를 인자로 준다.
    /// </summary>
    [Serializable]
    public sealed class WalletData
    {
        public const int CurrentVersion = 1;

        /// <summary>날짜 번호의 기준일. 2020-01-01은 <b>수요일</b>이고, 주 계산이 이 사실에 기댄다.</summary>
        private static readonly DateTime Epoch = new DateTime(2020, 1, 1);

        /// <summary>
        /// 기준일이 수요일이라 월요일까지 이틀을 당겨야 한 주의 시작이 0에 맞는다.
        /// </summary>
        private const int EpochToMonday = 2;

        /// <summary>
        /// 한국 표준시(UTC+9).
        ///
        /// <b>TimeZoneInfo를 쓰지 않는다.</b> 시간대 ID가 플랫폼마다 다르고
        /// (윈도우는 "Korea Standard Time", 안드로이드는 "Asia/Seoul"), 기기에 시간대
        /// 데이터가 없으면 예외가 난다. 한국은 1988년 이후 서머타임이 없어 고정 +9로
        /// 계산해도 언제나 정확하다.
        /// </summary>
        public static readonly TimeSpan KoreaOffset = TimeSpan.FromHours(9);

        public int version = CurrentVersion;

        public int coins;

        /// <summary>다음에 받을 출석 칸(0부터 순환).</summary>
        public int attendanceIndex;

        /// <summary>마지막으로 출석을 받은 날짜 번호. 아직 없으면 <see cref="NeverClaimed"/>.</summary>
        public int lastAttendanceDay = NeverClaimed;

        /// <summary>코인으로 보호권을 쓴 마지막 날짜 번호.</summary>
        public int coinProtectDay = NeverClaimed;

        /// <summary>광고로 보호권을 쓴 마지막 날짜 번호.</summary>
        public int adProtectDay = NeverClaimed;

        /// <summary>승리 코인 2배 광고를 마지막으로 본 날짜 번호.</summary>
        public int doubleRewardDay = NeverClaimed;

        /// <summary>그 날 2배 광고를 본 횟수. 날짜가 바뀌면 0부터 다시 센다.</summary>
        public int doubleRewardCount;

        public const int NeverClaimed = int.MinValue;

        /// <summary>
        /// <b>한국 시간 자정</b> 기준 날짜 번호. UTC 시각을 넣으면 된다.
        ///
        /// 기기 로컬 시간이 아니라 한국 시간으로 고정한 것은, 해외에 있거나 시간대를
        /// 바꿔 둔 기기에서도 초기화 시점이 모두 같아야 하기 때문이다.
        /// </summary>
        public static int Today(DateTime utcNow)
            => (int)((utcNow + KoreaOffset).Date - Epoch).TotalDays;

        /// <summary>날짜 번호를 실제 날짜로 되돌린다(표시·디버그용).</summary>
        public static DateTime DateOf(int dayNumber) => Epoch.AddDays(dayNumber);

        /// <summary>
        /// 날짜 번호가 속한 주(월요일 시작). 월요일 00:00 KST에 1 늘어난다.
        /// </summary>
        public static int WeekOf(int dayNumber) => FloorDiv(dayNumber + EpochToMonday, 7);

        /// <summary>음수에서도 아래로 내림한다. C#의 / 는 0 방향으로 자르므로 그대로 못 쓴다.</summary>
        private static int FloorDiv(int value, int divisor)
        {
            int q = value / divisor;
            if (value % divisor != 0 && ((value < 0) != (divisor < 0))) q--;
            return q;
        }

        // ---- 코인 ----

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            coins += amount;
        }

        public bool CanAfford(int price) => price >= 0 && coins >= price;

        /// <summary>잔액이 모자라면 아무것도 하지 않고 false를 돌려준다.</summary>
        public bool TrySpend(int price)
        {
            if (!CanAfford(price)) return false;
            coins -= price;
            return true;
        }

        // ---- 출석 ----

        /// <summary>
        /// 오늘 출석을 받을 수 있는가.
        ///
        /// <b>기기 시계를 과거로 돌린 경우는 받을 수 없다</b>(today가 마지막 수령일보다
        /// 작으면 조건이 성립하지 않는다). 미래로 돌리는 것은 서버 없이 막을 수 없다.
        /// 순위표가 없는 싱글 게임이라 여기에 더 투자하지 않는다.
        /// </summary>
        public bool CanClaimAttendance(int today)
        {
            bool newDay = lastAttendanceDay == NeverClaimed || today > lastAttendanceDay;
            // 그 주 일곱 칸을 다 받았으면 월요일까지 더 받을 수 없다.
            // 이 검사가 없으면 진행도가 한 바퀴 감겨 여덟 번째 수령이 열린다.
            return newDay && AttendanceIndexIn(today) < CoinRules.AttendanceCycleLength;
        }

        /// <summary>
        /// 이번 주에 몇 번째 출석까지 왔는가(0부터). 화면이 어느 칸을 강조할지 정한다.
        ///
        /// 저장된 값을 그대로 쓰지 않고 <b>매번 주가 바뀌었는지 확인해서</b> 돌려준다.
        /// 앱을 안 켠 채 주가 바뀌면 초기화를 실행할 기회가 없기 때문이다.
        /// </summary>
        public int AttendanceIndexIn(int today)
            => IsSameWeekAsLastClaim(today) ? attendanceIndex : 0;

        /// <summary>오늘 받을 보상 액수(받을 수 없으면 0).</summary>
        public int PendingAttendanceReward(int today)
            => CanClaimAttendance(today) ? CoinRules.AttendanceReward(AttendanceIndexIn(today)) : 0;

        /// <summary>
        /// 출석 보상을 수령한다. 받은 액수를 돌려주고, 받을 수 없으면 0.
        ///
        /// <b>진행은 매주 월요일 00:00 KST에 1일차로 돌아간다.</b> 그 주 안에서는
        /// 순차로 진행하므로, 하루 빠져도 다음에 올 때 그 다음 칸을 이어서 받는다.
        /// 대신 마지막 칸은 그 주에 일곱 번 다 와야 닿는다.
        /// </summary>
        public int ClaimAttendance(int today)
        {
            if (!CanClaimAttendance(today)) return 0;

            int index = AttendanceIndexIn(today);
            int reward = CoinRules.AttendanceReward(index);
            AddCoins(reward);

            // 마지막 칸까지 받아도 0으로 되돌리지 않는다. 이번 주는 여기서 끝이고,
            // 월요일이 되면 주가 바뀌어 저절로 1일차로 돌아간다.
            attendanceIndex = index + 1;
            if (attendanceIndex > CoinRules.AttendanceCycleLength)
                attendanceIndex = CoinRules.AttendanceCycleLength;

            lastAttendanceDay = today;
            return reward;
        }

        /// <summary>이번 주에 이미 한 번이라도 받았는가.</summary>
        private bool IsSameWeekAsLastClaim(int today)
            => lastAttendanceDay != NeverClaimed
               && WeekOf(lastAttendanceDay) == WeekOf(today);

        // ---- 패배 보호권 ----

        /// <summary>코인으로 쓰는 보호권이 오늘 남아 있는가(하루 1회).</summary>
        public bool CanUseCoinProtection(int today)
            => coinProtectDay == NeverClaimed || today != coinProtectDay;

        /// <summary>광고로 쓰는 보호권이 오늘 남아 있는가(하루 1회).</summary>
        public bool CanUseAdProtection(int today)
            => adProtectDay == NeverClaimed || today != adProtectDay;

        /// <summary>
        /// 코인을 내고 보호권을 쓴다. 잔액이 모자라거나 오늘 이미 썼으면 false.
        /// </summary>
        public bool TryUseCoinProtection(int today, int price)
        {
            if (!CanUseCoinProtection(today)) return false;
            if (!TrySpend(price)) return false;

            coinProtectDay = today;
            return true;
        }

        /// <summary>광고로 보호권을 쓴다. 오늘 이미 썼으면 false.</summary>
        public bool TryUseAdProtection(int today)
        {
            if (!CanUseAdProtection(today)) return false;

            adProtectDay = today;
            return true;
        }

        // ---- 승리 코인 2배 광고 ----

        /// <summary>오늘 2배 광고를 이미 몇 번 봤는가. 날짜가 바뀌었으면 0.</summary>
        public int DoubleRewardUsedToday(int today)
            => doubleRewardDay == today ? doubleRewardCount : 0;

        /// <summary>
        /// 오늘 2배를 더 쓸 수 있는가.
        ///
        /// 횟수를 제한하는 이유는 광고 수익이 아니라 <b>코인 경제</b> 때문이다.
        /// 무제한이면 수입이 보호권 하루치를 크게 넘겨 남는 코인이 쌓이기만 한다.
        /// </summary>
        public bool CanDoubleReward(int today)
            => DoubleRewardUsedToday(today) < CoinRules.DailyDoubleRewardLimit;

        /// <summary>2배 사용을 기록한다. 한도를 넘었으면 false.</summary>
        public bool TryUseDoubleReward(int today)
        {
            if (!CanDoubleReward(today)) return false;

            // 날짜가 바뀌었으면 세던 값을 버리고 새로 센다.
            doubleRewardCount = DoubleRewardUsedToday(today) + 1;
            doubleRewardDay = today;
            return true;
        }

        /// <summary>손상되거나 낡은 저장본 보정. 읽은 직후와 쓰기 전에 부른다.</summary>
        public void Repair()
        {
            if (coins < 0) coins = 0;

            // 0 ~ 칸 수까지 허용한다. 칸 수와 같으면 "이번 주는 다 받음"이라는 뜻이라
            // 나머지 연산으로 감으면 안 된다(감으면 그 주에 처음부터 다시 받게 된다).
            int cycle = CoinRules.AttendanceCycleLength;
            if (attendanceIndex < 0) attendanceIndex = 0;
            if (attendanceIndex > cycle) attendanceIndex = cycle;

            if (doubleRewardCount < 0) doubleRewardCount = 0;
            if (doubleRewardCount > CoinRules.DailyDoubleRewardLimit)
                doubleRewardCount = CoinRules.DailyDoubleRewardLimit;

            version = CurrentVersion;
        }
    }
}
