namespace DiceBattle.Core
{
    /// <summary>플레이어 기준 한 판의 결과.</summary>
    public enum PlayerMatchResult
    {
        Win,
        Lose,
        Draw
    }

    /// <summary>
    /// 점수 산술(순수 로직).
    /// - 시작 0점, 하한 0(패배해도 0 미만으로 내려가지 않음)
    /// - 승/패로 얼마나 움직이는지는 <b>그 판을 플레이한 난이도</b>가 정한다
    ///   (<see cref="DifficultyTier"/>). 무승부는 난이도와 무관하게 0.
    ///
    /// 난이도 해금 판정은 여기가 아니라 <see cref="DifficultyTable"/>이 맡는다.
    /// 해금은 현재 점수가 아니라 최고 달성 점수로 판정해야 하므로, 점수 산술과
    /// 같은 자리에 두면 잘못된 값으로 판정하기 쉽다.
    /// </summary>
    public static class RankSystem
    {
        public const int StartScore = 0;
        public const int MinScore = 0;

        /// <summary>이번 판의 점수 증감(패배는 음수).</summary>
        public static int DeltaFor(PlayerMatchResult result, DifficultyTier tier)
        {
            switch (result)
            {
                case PlayerMatchResult.Win: return tier.WinPoints;
                case PlayerMatchResult.Lose: return -tier.LosePoints;
                default: return 0;
            }
        }

        /// <summary>결과를 반영한 새 점수(하한 <see cref="MinScore"/> 적용).</summary>
        public static int ApplyResult(int score, PlayerMatchResult result, DifficultyTier tier)
        {
            int next = score + DeltaFor(result, tier);
            return next < MinScore ? MinScore : next;
        }

        /// <summary>MatchOutcome을 사람(human) 기준 결과로 변환.</summary>
        public static PlayerMatchResult ResultFor(MatchOutcome outcome, PlayerId human)
        {
            if (outcome.IsDraw) return PlayerMatchResult.Draw;
            return outcome.Winner == human ? PlayerMatchResult.Win : PlayerMatchResult.Lose;
        }
    }
}
