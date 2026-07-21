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
    /// 점수/등급 시스템(순수 로직).
    /// - 시작 0점, 하한 0(패배해도 0 미만으로 내려가지 않음)
    /// - 승 +20, 패 -10, 무 0
    /// - 등급: Lv1 0~100, Lv2 100~300, Lv3 300~600, Lv4 600~1000, Lv5 1000+
    ///   (경계값은 상위 등급에 포함: 100→Lv2, 300→Lv3, 600→Lv4, 1000→Lv5)
    /// </summary>
    public static class RankSystem
    {
        public const int StartScore = 0;
        public const int MinScore = 0;
        public const int WinDelta = 20;
        public const int LoseDelta = -10;
        public const int DrawDelta = 0;

        public static int LevelForScore(int score)
        {
            if (score < 100) return 1;
            if (score < 300) return 2;
            if (score < 600) return 3;
            if (score < 1000) return 4;
            return 5;
        }

        public static int DeltaFor(PlayerMatchResult result)
        {
            switch (result)
            {
                case PlayerMatchResult.Win: return WinDelta;
                case PlayerMatchResult.Lose: return LoseDelta;
                default: return DrawDelta;
            }
        }

        /// <summary>결과를 반영한 새 점수(하한 0 적용).</summary>
        public static int ApplyResult(int score, PlayerMatchResult result)
        {
            int next = score + DeltaFor(result);
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
