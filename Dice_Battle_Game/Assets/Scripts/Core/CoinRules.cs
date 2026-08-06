using System;

namespace DiceBattle.Core
{
    /// <summary>
    /// 다이스 코인의 획득량과 패배 보호권 가격.
    ///
    /// <b>모든 값이 난이도에 비례한다.</b> 고정 보상을 주면 승률이 높은 Lv.1을 무한히
    /// 파밍하는 것이 코인 효율 최고가 되고, 그러면 난이도를 올려 갈 이유가 사라진다
    /// (docs/Difficulty.md 6장이 연승 보너스에 대해 적어 둔 것과 같은 이유다).
    ///
    /// 가격도 비례여야 한다. 고정 가격이면 상위 난이도에서 하루 수입이 가격의 몇 배가 되어
    /// 코인이 남아돈다 — 소비처가 보호권 하나뿐이라 그 순간 화폐가 무의미해진다.
    /// </summary>
    public static class CoinRules
    {
        /// <summary>승점 몇 점당 코인 1개인가.</summary>
        private const int PointsPerCoin = 10;

        /// <summary>패배·무승부는 승리의 이 비율만큼 받는다(위로 성격).</summary>
        private const double ConsolationRatio = 0.3d;

        /// <summary>보호권 가격 = 그 난이도의 차감 점수 × 이 배수.</summary>
        private const int PriceMultiplier = 4;

        /// <summary>출석 보상(7일 순환). 난이도와 무관한 유일한 획득처다.</summary>
        public static readonly int[] AttendanceRewards = { 5, 5, 10, 10, 15, 15, 40 };

        public static int AttendanceCycleLength => AttendanceRewards.Length;

        /// <summary>승리 코인. Lv.1 = 2, Lv.10 = 30.</summary>
        public static int WinCoins(DifficultyTier tier)
            => AtLeastOne(Round((double)tier.WinPoints / PointsPerCoin));

        /// <summary>패배 코인. 승리의 30%.</summary>
        public static int LoseCoins(DifficultyTier tier)
            => AtLeastOne(Round(WinCoins(tier) * ConsolationRatio));

        /// <summary>
        /// 무승부 코인. <b>패배와 같다.</b>
        /// 무승부는 점수 변동도 0이라 보상도 최소치로 두는 편이 일관된다.
        /// </summary>
        public static int DrawCoins(DifficultyTier tier) => LoseCoins(tier);

        public static int CoinsFor(PlayerMatchResult result, DifficultyTier tier)
        {
            switch (result)
            {
                case PlayerMatchResult.Win: return WinCoins(tier);
                case PlayerMatchResult.Lose: return LoseCoins(tier);
                default: return DrawCoins(tier);
            }
        }

        /// <summary>
        /// 패배 보호권 가격. Lv.1 = 40, Lv.10 = 560.
        /// 차감 점수에 비례하므로 "막아 주는 점수 대비 가격"이 전 구간 같다.
        /// </summary>
        public static int ProtectionPrice(DifficultyTier tier)
            => AtLeastOne(tier.LosePoints * PriceMultiplier);

        /// <summary>순환 위치(0부터)의 출석 보상.</summary>
        public static int AttendanceReward(int index)
        {
            if (AttendanceRewards.Length == 0) return 0;
            int wrapped = index % AttendanceRewards.Length;
            if (wrapped < 0) wrapped += AttendanceRewards.Length;
            return AttendanceRewards[wrapped];
        }

        private static int Round(double value)
            => (int)Math.Round(value, MidpointRounding.AwayFromZero);

        private static int AtLeastOne(int value) => value < 1 ? 1 : value;
    }
}
