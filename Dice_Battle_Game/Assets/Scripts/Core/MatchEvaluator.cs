using System.Collections.Generic;

namespace DiceBattle.Core
{
    /// <summary>최종 결과(라인별 승패 + 최종 승자).</summary>
    public readonly struct MatchOutcome
    {
        /// <summary>최종 승자. 무승부면 null.</summary>
        public PlayerId? Winner { get; }
        public int PlayerOneLineWins { get; }
        public int PlayerTwoLineWins { get; }
        public IReadOnlyList<LineResult> Lines { get; }

        public bool IsDraw => Winner == null;

        public MatchOutcome(PlayerId? winner, int p1Wins, int p2Wins, LineResult[] lines)
        {
            Winner = winner;
            PlayerOneLineWins = p1Wins;
            PlayerTwoLineWins = p2Wins;
            Lines = lines;
        }
    }

    /// <summary>
    /// 기획서 8번 승패 판정:
    /// - 라인별 점수 비교(높은 쪽 승, 같으면 해당 라인 무승부).
    /// - 라인 승수가 더 많은 플레이어가 최종 승리, 승수가 같으면 최종 무승부.
    ///   (3개 라인이므로 최종 무승부는 (1승·1패·1무) 또는 (3무) 두 경우뿐이다.)
    ///   기획서의 "2개 이상 라인 승리"와 (1승·0패·2무)를 제외한 모든 경우가 동일하며,
    ///   (1승·0패·2무)는 승수 우위에 따라 1승한 쪽의 승리로 판정한다.
    /// </summary>
    public static class MatchEvaluator
    {
        public static MatchOutcome Evaluate(GameState state)
        {
            var lines = new LineResult[Field.LineCount];
            int p1Wins = 0;
            int p2Wins = 0;

            for (int i = 0; i < Field.LineCount; i++)
            {
                int s1 = state.Field(PlayerId.One)[i].Score();
                int s2 = state.Field(PlayerId.Two)[i].Score();

                if (s1 > s2) { lines[i] = LineResult.PlayerOne; p1Wins++; }
                else if (s2 > s1) { lines[i] = LineResult.PlayerTwo; p2Wins++; }
                else lines[i] = LineResult.Draw;
            }

            PlayerId? winner =
                p1Wins > p2Wins ? PlayerId.One :
                p2Wins > p1Wins ? PlayerId.Two :
                (PlayerId?)null;

            return new MatchOutcome(winner, p1Wins, p2Wins, lines);
        }
    }
}
