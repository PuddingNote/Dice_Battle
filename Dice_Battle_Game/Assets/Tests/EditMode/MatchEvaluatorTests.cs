using NUnit.Framework;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    public class MatchEvaluatorTests
    {
        private static void Fill(GameState s, PlayerId p, int line, params int[] values)
        {
            foreach (int v in values)
                s.Field(p)[line].Add(new Dice(v, isSpecial: false, owner: p));
        }

        [Test]
        public void Player_Wins_Two_Lines_Wins_Match()
        {
            var s = new GameState();
            // line0: P1 6 vs P2 1 → P1
            Fill(s, PlayerId.One, 0, 6); Fill(s, PlayerId.Two, 0, 1);
            // line1: P1 6 vs P2 1 → P1
            Fill(s, PlayerId.One, 1, 6); Fill(s, PlayerId.Two, 1, 1);
            // line2: P1 1 vs P2 6 → P2
            Fill(s, PlayerId.One, 2, 1); Fill(s, PlayerId.Two, 2, 6);

            var o = MatchEvaluator.Evaluate(s);
            Assert.AreEqual(PlayerId.One, o.Winner);
            Assert.AreEqual(2, o.PlayerOneLineWins);
            Assert.AreEqual(1, o.PlayerTwoLineWins);
            Assert.AreEqual(LineResult.PlayerOne, o.Lines[0]);
            Assert.AreEqual(LineResult.PlayerOne, o.Lines[1]);
            Assert.AreEqual(LineResult.PlayerTwo, o.Lines[2]);
        }

        [Test]
        public void Tied_Line_Is_Draw()
        {
            var s = new GameState();
            // line0: 동점 → 무승부
            Fill(s, PlayerId.One, 0, 3, 3); // 3+3+3 = 9
            Fill(s, PlayerId.Two, 0, 3, 3); // 9
            var o = MatchEvaluator.Evaluate(s);
            Assert.AreEqual(LineResult.Draw, o.Lines[0]);
        }

        [Test]
        public void One_Win_Two_Draws_Wins_Match()
        {
            // (1승·0패·2무): 라인 승수 1 vs 0 → 1승한 쪽이 최종 승리(무승부 아님).
            var s = new GameState();
            Fill(s, PlayerId.One, 0, 6); Fill(s, PlayerId.Two, 0, 1); // line0 → P1
            Fill(s, PlayerId.One, 1, 4); Fill(s, PlayerId.Two, 1, 4); // line1 → 무
            Fill(s, PlayerId.One, 2, 2); Fill(s, PlayerId.Two, 2, 2); // line2 → 무

            var o = MatchEvaluator.Evaluate(s);
            Assert.AreEqual(PlayerId.One, o.Winner);
            Assert.AreEqual(1, o.PlayerOneLineWins);
            Assert.AreEqual(0, o.PlayerTwoLineWins);
        }

        [Test]
        public void All_Three_Lines_Draw_Is_Match_Draw()
        {
            // (3무): 최종 무승부.
            var s = new GameState();
            Fill(s, PlayerId.One, 0, 5); Fill(s, PlayerId.Two, 0, 5);
            Fill(s, PlayerId.One, 1, 4); Fill(s, PlayerId.Two, 1, 4);
            Fill(s, PlayerId.One, 2, 2); Fill(s, PlayerId.Two, 2, 2);

            var o = MatchEvaluator.Evaluate(s);
            Assert.IsTrue(o.IsDraw);
            Assert.IsNull(o.Winner);
        }

        [Test]
        public void One_One_One_Is_Match_Draw()
        {
            var s = new GameState();
            // line0 → P1, line1 → P2, line2 → 무승부
            Fill(s, PlayerId.One, 0, 6); Fill(s, PlayerId.Two, 0, 1);
            Fill(s, PlayerId.One, 1, 1); Fill(s, PlayerId.Two, 1, 6);
            Fill(s, PlayerId.One, 2, 4); Fill(s, PlayerId.Two, 2, 4);

            var o = MatchEvaluator.Evaluate(s);
            Assert.IsTrue(o.IsDraw);
            Assert.IsNull(o.Winner);
            Assert.AreEqual(1, o.PlayerOneLineWins);
            Assert.AreEqual(1, o.PlayerTwoLineWins);
        }

        [Test]
        public void Double_Bonus_Can_Flip_Line_Winner()
        {
            var s = new GameState();
            // P1: 5,1 = 6 (보너스 없음)
            Fill(s, PlayerId.One, 0, 5, 1);
            // P2: 3,3 = 3+3+(3*1)=9 (더블 보너스로 역전)
            Fill(s, PlayerId.Two, 0, 3, 3);
            var o = MatchEvaluator.Evaluate(s);
            Assert.AreEqual(LineResult.PlayerTwo, o.Lines[0]);
        }
    }
}
