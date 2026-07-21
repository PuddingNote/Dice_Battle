using System;
using NUnit.Framework;
using DiceBattle.Core;
using DiceBattle.AI;

namespace DiceBattle.Tests
{
    public class DifficultyTests
    {
        private static double Average(DifficultyDiceRoller roller, PlayerId who, int n)
        {
            long sum = 0;
            for (int i = 0; i < n; i++) sum += roller.Roll(who);
            return (double)sum / n;
        }

        [Test]
        public void Weighted_Player_Average_Increases_With_Level()
        {
            // AI(P2)만 가중: Lv1(하향) < Lv3(균등) < Lv5(상향)
            double a1 = Average(new DifficultyDiceRoller(PlayerId.Two, 1, new Random(1)), PlayerId.Two, 4000);
            double a3 = Average(new DifficultyDiceRoller(PlayerId.Two, 3, new Random(1)), PlayerId.Two, 4000);
            double a5 = Average(new DifficultyDiceRoller(PlayerId.Two, 5, new Random(1)), PlayerId.Two, 4000);

            Assert.Less(a1, a3, $"Lv1({a1:F2}) < Lv3({a3:F2})");
            Assert.Less(a3, a5, $"Lv3({a3:F2}) < Lv5({a5:F2})");
        }

        [Test]
        public void Non_Weighted_Player_Is_Always_Uniform()
        {
            // 플레이어(P1)는 레벨과 무관하게 균등(평균 ~3.5)
            double a1 = Average(new DifficultyDiceRoller(PlayerId.Two, 1, new Random(2)), PlayerId.One, 8000);
            double a5 = Average(new DifficultyDiceRoller(PlayerId.Two, 5, new Random(2)), PlayerId.One, 8000);
            Assert.That(a1, Is.EqualTo(3.5).Within(0.15));
            Assert.That(a5, Is.EqualTo(3.5).Within(0.15));
        }

        [Test]
        public void Roll_Always_In_Range()
        {
            var r = new DifficultyDiceRoller(PlayerId.Two, 5, new Random(3));
            for (int i = 0; i < 1000; i++)
            {
                int v = r.Roll(PlayerId.Two);
                Assert.GreaterOrEqual(v, 1);
                Assert.LessOrEqual(v, 6);
            }
        }

        private static GameState StateWithPending(int value)
        {
            var s = new GameState();
            s.CurrentPlayer = PlayerId.One;
            s.Phase = TurnPhase.AwaitingPrimaryPlacement;
            s.PendingDice = new Dice(value, false, PlayerId.One);
            return s;
        }

        [Test]
        public void Level5_Picks_Heuristic_Best_Level1_Picks_Worst()
        {
            // 더블이 유일한 승리 수인 상태(최선=line0).
            var s = StateWithPending(4);
            s.Field(PlayerId.One)[0].Add(new Dice(4, false, PlayerId.One));
            s.Field(PlayerId.One)[1].Add(new Dice(1, false, PlayerId.One));
            s.Field(PlayerId.One)[2].Add(new Dice(1, false, PlayerId.One));
            s.Field(PlayerId.Two)[0].Add(new Dice(6, false, PlayerId.Two));
            s.Field(PlayerId.Two)[1].Add(new Dice(6, false, PlayerId.Two));
            s.Field(PlayerId.Two)[2].Add(new Dice(6, false, PlayerId.Two));

            var heuristic = new HeuristicAiStrategy();
            int best = heuristic.ChoosePrimaryLine(s, PlayerId.One);
            int worst = heuristic.WorstPrimaryLine(s, PlayerId.One);
            Assert.AreNotEqual(best, worst, "테스트 상태에서 최선/최악이 달라야 유효.");

            var lv5 = new LeveledAiStrategy(5, new Random(0));
            var lv1 = new LeveledAiStrategy(1, new Random(0));
            Assert.AreEqual(best, lv5.ChoosePrimaryLine(s, PlayerId.One), "Lv5는 최선 수.");
            Assert.AreEqual(worst, lv1.ChoosePrimaryLine(s, PlayerId.One), "Lv1은 최악 수.");
        }

        [Test]
        public void Level_Is_Clamped_To_1_5()
        {
            Assert.AreEqual(1, new LeveledAiStrategy(0).Level);
            Assert.AreEqual(5, new LeveledAiStrategy(9).Level);
        }
    }
}
