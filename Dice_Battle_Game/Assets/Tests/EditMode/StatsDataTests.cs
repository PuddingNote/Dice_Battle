using NUnit.Framework;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    /// <summary>
    /// 누적 전적. 저장/로드(PlayerPrefs)는 UI 계층이라 여기서 다루지 않고,
    /// 집계 규칙과 손상 데이터 보정만 검증한다.
    /// </summary>
    public class StatsDataTests
    {
        private static StatsData Fresh() => new StatsData();

        [Test]
        public void Fresh_Stats_Are_Empty()
        {
            var s = Fresh();

            Assert.AreEqual(0, s.TotalMatches);
            Assert.AreEqual(0d, s.WinRate);
            Assert.AreEqual(0d, s.AverageRemoved);
            Assert.AreEqual(0, s.bestStreak);
        }

        [Test]
        public void Win_Loss_Draw_Are_Counted_Separately()
        {
            var s = Fresh();

            s.Apply(PlayerMatchResult.Win, 3, 0);
            s.Apply(PlayerMatchResult.Lose, 3, 0);
            s.Apply(PlayerMatchResult.Draw, 3, 0);

            Assert.AreEqual(1, s.wins);
            Assert.AreEqual(1, s.losses);
            Assert.AreEqual(1, s.draws);
            Assert.AreEqual(3, s.TotalMatches);
        }

        [Test]
        public void Win_Rate_Counts_Draws_In_The_Denominator()
        {
            var s = Fresh();

            s.Apply(PlayerMatchResult.Win, 1, 0);
            s.Apply(PlayerMatchResult.Draw, 1, 0);

            // 2판 중 1승이므로 50%다. 무승부를 빼면 100%가 되어 버린다.
            Assert.AreEqual(0.5d, s.WinRate, 1e-9);
        }

        [Test]
        public void A_Draw_Breaks_The_Streak()
        {
            var s = Fresh();

            s.Apply(PlayerMatchResult.Win, 1, 0);
            s.Apply(PlayerMatchResult.Win, 1, 0);
            s.Apply(PlayerMatchResult.Draw, 1, 0);

            Assert.AreEqual(0, s.currentStreak, "무승부는 연승을 끊는다");
            Assert.AreEqual(2, s.bestStreak, "끊겨도 최고 기록은 남는다");
        }

        [Test]
        public void A_Loss_Breaks_The_Streak()
        {
            var s = Fresh();

            s.Apply(PlayerMatchResult.Win, 1, 0);
            s.Apply(PlayerMatchResult.Lose, 1, 0);

            Assert.AreEqual(0, s.currentStreak);
            Assert.AreEqual(1, s.bestStreak);
        }

        [Test]
        public void Best_Streak_Keeps_The_Longest_Run()
        {
            var s = Fresh();

            // 3연승 뒤 끊고, 다시 2연승. 최고는 3이어야 한다.
            for (int i = 0; i < 3; i++) s.Apply(PlayerMatchResult.Win, 1, 0);
            s.Apply(PlayerMatchResult.Lose, 1, 0);
            for (int i = 0; i < 2; i++) s.Apply(PlayerMatchResult.Win, 1, 0);

            Assert.AreEqual(3, s.bestStreak);
            Assert.AreEqual(2, s.currentStreak);
        }

        [Test]
        public void Removed_Dice_Average_Is_Per_Match()
        {
            var s = Fresh();

            s.Apply(PlayerMatchResult.Win, 1, 3);
            s.Apply(PlayerMatchResult.Lose, 1, 1);

            Assert.AreEqual(4, s.removedDice);
            Assert.AreEqual(2d, s.AverageRemoved, 1e-9);
        }

        [Test]
        public void Records_Are_Kept_Per_Level()
        {
            var s = Fresh();

            s.Apply(PlayerMatchResult.Win, 2, 0);
            s.Apply(PlayerMatchResult.Win, 2, 0);
            s.Apply(PlayerMatchResult.Lose, 7, 0);

            s.LevelRecord(2, out int w2, out int l2, out int d2);
            s.LevelRecord(7, out int w7, out int l7, out int d7);

            Assert.AreEqual(2, w2);
            Assert.AreEqual(0, l2);
            Assert.AreEqual(0, d2);

            Assert.AreEqual(0, w7);
            Assert.AreEqual(1, l7);
            Assert.AreEqual(0, d7);
        }

        [Test]
        public void Out_Of_Range_Levels_Are_Clamped_Not_Dropped()
        {
            var s = Fresh();

            // 저장 데이터가 손상되었거나 난이도 수가 줄어든 경우. 집계를 잃는 것보다 낫다.
            s.Apply(PlayerMatchResult.Win, 0, 0);
            s.Apply(PlayerMatchResult.Win, 99, 0);

            s.LevelRecord(DifficultyTable.MinLevel, out int wMin, out _, out _);
            s.LevelRecord(DifficultyTable.MaxLevel, out int wMax, out _, out _);

            Assert.AreEqual(1, wMin);
            Assert.AreEqual(1, wMax);
            Assert.AreEqual(2, s.TotalMatches);
        }

        [Test]
        public void Repair_Rebuilds_Missing_Level_Arrays()
        {
            var s = Fresh();

            // JsonUtility는 JSON에 배열이 없으면 null을 그대로 남긴다.
            s.winsByLevel = null;
            s.lossesByLevel = new int[3];   // 난이도 수가 늘기 전에 저장된 데이터
            s.drawsByLevel = null;

            s.Repair();

            Assert.AreEqual(DifficultyTable.LevelCount, s.winsByLevel.Length);
            Assert.AreEqual(DifficultyTable.LevelCount, s.lossesByLevel.Length);
            Assert.AreEqual(DifficultyTable.LevelCount, s.drawsByLevel.Length);
        }

        [Test]
        public void Repair_Keeps_Existing_Level_Counts()
        {
            var s = Fresh();

            s.lossesByLevel = new int[] { 4, 7 }; // 짧은 구버전 배열
            s.Repair();

            Assert.AreEqual(4, s.lossesByLevel[0]);
            Assert.AreEqual(7, s.lossesByLevel[1]);
        }

        [Test]
        public void Repair_Fixes_Negative_And_Inconsistent_Values()
        {
            var s = Fresh();

            s.wins = -5;
            s.removedDice = -1;
            s.currentStreak = 9;
            s.bestStreak = 2; // 최고가 현재보다 작을 수는 없다

            s.Repair();

            Assert.AreEqual(0, s.wins);
            Assert.AreEqual(0, s.removedDice);
            Assert.AreEqual(9, s.bestStreak);
        }

        [Test]
        public void Apply_Works_On_A_Corrupted_Record_Without_Throwing()
        {
            var s = Fresh();
            s.winsByLevel = null; // Repair를 부르지 않은 채 바로 집계

            Assert.DoesNotThrow(() => s.Apply(PlayerMatchResult.Win, 5, 2));
            Assert.AreEqual(1, s.wins);
        }
    }
}
