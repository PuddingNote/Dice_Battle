using NUnit.Framework;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    public class RankSystemTests
    {
        [Test]
        public void LevelForScore_Boundaries()
        {
            Assert.AreEqual(1, RankSystem.LevelForScore(0));
            Assert.AreEqual(1, RankSystem.LevelForScore(99));
            Assert.AreEqual(2, RankSystem.LevelForScore(100));
            Assert.AreEqual(2, RankSystem.LevelForScore(299));
            Assert.AreEqual(3, RankSystem.LevelForScore(300));
            Assert.AreEqual(3, RankSystem.LevelForScore(599));
            Assert.AreEqual(4, RankSystem.LevelForScore(600));
            Assert.AreEqual(4, RankSystem.LevelForScore(999));
            Assert.AreEqual(5, RankSystem.LevelForScore(1000));
            Assert.AreEqual(5, RankSystem.LevelForScore(5000));
        }

        [Test]
        public void ApplyResult_Win_Lose_Draw()
        {
            Assert.AreEqual(20, RankSystem.ApplyResult(0, PlayerMatchResult.Win));
            Assert.AreEqual(190, RankSystem.ApplyResult(200, PlayerMatchResult.Lose));
            Assert.AreEqual(500, RankSystem.ApplyResult(500, PlayerMatchResult.Draw));
        }

        [Test]
        public void ApplyResult_Floor_At_Zero()
        {
            Assert.AreEqual(0, RankSystem.ApplyResult(0, PlayerMatchResult.Lose));
            Assert.AreEqual(0, RankSystem.ApplyResult(5, PlayerMatchResult.Lose));
            Assert.AreEqual(40, RankSystem.ApplyResult(50, PlayerMatchResult.Lose));
        }

        [Test]
        public void ResultFor_Maps_Outcome_To_Player()
        {
            var empty = new LineResult[0];
            var p1Win = new MatchOutcome(PlayerId.One, 2, 1, empty);
            var p2Win = new MatchOutcome(PlayerId.Two, 1, 2, empty);
            var draw = new MatchOutcome(null, 1, 1, empty);

            Assert.AreEqual(PlayerMatchResult.Win, RankSystem.ResultFor(p1Win, PlayerId.One));
            Assert.AreEqual(PlayerMatchResult.Lose, RankSystem.ResultFor(p2Win, PlayerId.One));
            Assert.AreEqual(PlayerMatchResult.Draw, RankSystem.ResultFor(draw, PlayerId.One));
        }
    }
}
