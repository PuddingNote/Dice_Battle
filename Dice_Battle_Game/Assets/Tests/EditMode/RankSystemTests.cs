using NUnit.Framework;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    /// <summary>
    /// 점수 산술. 난이도 해금 판정은 DifficultyTableTests가 맡는다.
    /// 수치는 확정 전이므로 실제 밸런스 표를 쓰지 않고 티어를 직접 만들어 검증한다.
    /// </summary>
    public class RankSystemTests
    {
        private static DifficultyTier Tier(int win, int lose)
            => new DifficultyTier(level: 1, unlockScore: 0, winPoints: win, losePoints: lose);

        private static DifficultyTier TierWithStreak(int win, int lose, int streakBonus)
            => new DifficultyTier(level: 1, unlockScore: 0, winPoints: win, losePoints: lose,
                streakBonusPoints: streakBonus);

        [Test]
        public void Delta_Comes_From_The_Tier()
        {
            var easy = Tier(win: 20, lose: 10);
            var hard = Tier(win: 300, lose: 140);

            Assert.AreEqual(20, RankSystem.DeltaFor(PlayerMatchResult.Win, easy));
            Assert.AreEqual(-10, RankSystem.DeltaFor(PlayerMatchResult.Lose, easy));

            // 같은 결과라도 난이도가 높으면 폭이 커진다.
            Assert.AreEqual(300, RankSystem.DeltaFor(PlayerMatchResult.Win, hard));
            Assert.AreEqual(-140, RankSystem.DeltaFor(PlayerMatchResult.Lose, hard));
        }

        [Test]
        public void Draw_Never_Moves_The_Score()
        {
            // 무승부는 난이도와 무관하게 0이다.
            Assert.AreEqual(0, RankSystem.DeltaFor(PlayerMatchResult.Draw, Tier(20, 10)));
            Assert.AreEqual(0, RankSystem.DeltaFor(PlayerMatchResult.Draw, Tier(300, 140)));
            Assert.AreEqual(500, RankSystem.ApplyResult(500, PlayerMatchResult.Draw, Tier(300, 140)));
        }

        [Test]
        public void ApplyResult_Adds_And_Subtracts()
        {
            var tier = Tier(win: 70, lose: 30);
            Assert.AreEqual(70, RankSystem.ApplyResult(0, PlayerMatchResult.Win, tier));
            Assert.AreEqual(170, RankSystem.ApplyResult(200, PlayerMatchResult.Lose, tier));
        }

        [Test]
        public void ApplyResult_Floors_At_Zero()
        {
            var tier = Tier(win: 70, lose: 30);
            Assert.AreEqual(0, RankSystem.ApplyResult(0, PlayerMatchResult.Lose, tier));
            Assert.AreEqual(0, RankSystem.ApplyResult(10, PlayerMatchResult.Lose, tier),
                "차감량보다 점수가 적어도 음수로 내려가지 않는다.");
        }

        [Test]
        public void The_First_Win_Gets_No_Streak_Bonus()
        {
            // priorStreak=0은 "직전 판이 승리가 아니었다"는 뜻이다.
            var tier = TierWithStreak(win: 100, lose: 40, streakBonus: 50);
            Assert.AreEqual(100, RankSystem.DeltaFor(PlayerMatchResult.Win, tier, priorStreak: 0));
        }

        [Test]
        public void The_Second_Straight_Win_Gets_The_Streak_Bonus()
        {
            // priorStreak=1 이상은 직전 판도 이겼다는 뜻이고, 그때부터 매 승리에 붙는다.
            var tier = TierWithStreak(win: 100, lose: 40, streakBonus: 50);
            Assert.AreEqual(150, RankSystem.DeltaFor(PlayerMatchResult.Win, tier, priorStreak: 1));
            Assert.AreEqual(150, RankSystem.DeltaFor(PlayerMatchResult.Win, tier, priorStreak: 7),
                "연승 길이와 무관하게 보너스는 승점에 비례한 고정폭이어야 한다.");
        }

        [Test]
        public void The_Streak_Bonus_Never_Touches_Loss_Or_Draw()
        {
            // 연승 중에 지면 그 판은 그냥 패배다. 보너스가 손해를 줄여 주면 안 된다.
            var tier = TierWithStreak(win: 100, lose: 40, streakBonus: 50);
            Assert.AreEqual(-40, RankSystem.DeltaFor(PlayerMatchResult.Lose, tier, priorStreak: 5));
            Assert.AreEqual(0, RankSystem.DeltaFor(PlayerMatchResult.Draw, tier, priorStreak: 5));
        }

        [Test]
        public void ApplyResult_Threads_The_Streak_Bonus_Through()
        {
            var tier = TierWithStreak(win: 100, lose: 40, streakBonus: 50);
            Assert.AreEqual(150, RankSystem.ApplyResult(0, PlayerMatchResult.Win, tier, priorStreak: 1));
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
