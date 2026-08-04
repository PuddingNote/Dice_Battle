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
            // AI(P2)만 가중. 난이도가 오를수록 하향 편향이 옅어져 평균이 올라간다.
            // Lv10은 편향이 없어 공정(평균 3.5)이며, 그 위로는 올라가지 않는다.
            double a1 = Average(new DifficultyDiceRoller(PlayerId.Two, 1, new Random(1)), PlayerId.Two, 4000);
            double a5 = Average(new DifficultyDiceRoller(PlayerId.Two, 5, new Random(1)), PlayerId.Two, 4000);
            double a10 = Average(new DifficultyDiceRoller(PlayerId.Two, 10, new Random(1)), PlayerId.Two, 4000);

            Assert.Less(a1, a5, $"Lv1({a1:F2}) < Lv5({a5:F2})");
            Assert.Less(a5, a10, $"Lv5({a5:F2}) < Lv10({a10:F2})");
            Assert.That(a10, Is.EqualTo(3.5).Within(0.15), "최고 난이도는 공정한 주사위다.");
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
            for (int level = 1; level <= 10; level++)
            {
                var r = new DifficultyDiceRoller(PlayerId.Two, level, new Random(3));
                for (int i = 0; i < 500; i++)
                {
                    int v = r.Roll(PlayerId.Two);
                    Assert.GreaterOrEqual(v, 1, $"Lv{level}");
                    Assert.LessOrEqual(v, 6, $"Lv{level}");
                }
            }
        }

        [Test]
        public void Ai_Spectrum_Spans_Every_Level_Without_Gaps()
        {
            // 10단계로 나눈 뒤 인접 레벨이 완전히 같아지면 그 구간은 껍데기가 된다.
            // 각 수치가 양 끝 사이에서 단조롭게 움직이는지만 확인한다(체감 크기는 별개).
            for (int level = 2; level <= 10; level++)
            {
                Assert.GreaterOrEqual(LeveledAiStrategy.DefaultPlayBest(level),
                    LeveledAiStrategy.DefaultPlayBest(level - 1), $"Lv{level} playBest");
                Assert.LessOrEqual(LeveledAiStrategy.DefaultPlayWorst(level),
                    LeveledAiStrategy.DefaultPlayWorst(level - 1), $"Lv{level} playWorst");
                Assert.GreaterOrEqual(LeveledAiStrategy.DefaultSmartExtra(level),
                    LeveledAiStrategy.DefaultSmartExtra(level - 1), $"Lv{level} smartExtra");
                Assert.LessOrEqual(DifficultyDiceRoller.DefaultLowBias(level),
                    DifficultyDiceRoller.DefaultLowBias(level - 1), $"Lv{level} diceLowBias");
            }

            // 위쪽 끝은 천장이다 — 항상 최선 수 + 공정한 주사위.
            Assert.That(LeveledAiStrategy.DefaultPlayBest(10), Is.EqualTo(1.0), "Lv10은 항상 최선 수.");
            Assert.That(LeveledAiStrategy.DefaultSmartExtra(10), Is.EqualTo(1.0));
            Assert.That(DifficultyDiceRoller.DefaultLowBias(10), Is.EqualTo(0.0), "Lv10은 공정한 주사위.");
        }

        [Test]
        public void Easiest_Level_Is_Still_An_Opponent()
        {
            // 거의 매 수를 최악으로 두는 AI는 상대가 아니라 구경거리다.
            // 가장 쉬운 난이도에도 최선 수가 섞여 있어야 한다.
            Assert.Greater(LeveledAiStrategy.DefaultPlayBest(1), 0.0, "Lv1도 최선 수를 섞는다.");
            Assert.Less(LeveledAiStrategy.DefaultPlayWorst(1), 0.5, "최악 수가 과반이면 안 된다.");

            // 주사위 편향이 세면 AI가 낮은 눈만 뽑는 게 눈에 띄어 게임이 시시해진다.
            Assert.LessOrEqual(DifficultyDiceRoller.DefaultLowBias(1), 0.7);
        }

        [Test]
        public void Dice_Bias_Falls_All_The_Way_To_The_Top()
        {
            // 편향이 중간에 0으로 떨어지면 그 위 단계들이 전부 공정한 주사위가 되고,
            // 남는 차이는 배치 실력뿐인데 한 판의 분산이 그걸 덮어 서로 구분되지 않는다.
            // 그래서 마지막 단계까지 매 레벨 다른 값이어야 한다.
            for (int level = 2; level <= 10; level++)
            {
                Assert.Less(DifficultyDiceRoller.DefaultLowBias(level),
                    DifficultyDiceRoller.DefaultLowBias(level - 1),
                    $"Lv{level}의 편향이 Lv{level - 1}과 같거나 크다.");
            }

            Assert.Greater(DifficultyDiceRoller.DefaultLowBias(9), 0.0,
                "Lv9까지는 편향이 남아 Lv10과 구분되어야 한다.");
            Assert.That(DifficultyDiceRoller.DefaultLowBias(10), Is.EqualTo(0.0),
                "천장만 공정한 주사위다.");
        }

        [Test]
        public void Best_And_Worst_Probabilities_Never_Exceed_One()
        {
            // playBest + playWorst > 1 이면 무작위 구간이 사라지고 확률 배분이 깨진다.
            for (int level = 1; level <= 10; level++)
            {
                double sum = LeveledAiStrategy.DefaultPlayBest(level)
                           + LeveledAiStrategy.DefaultPlayWorst(level);
                Assert.LessOrEqual(sum, 1.0, $"Lv{level} 확률 합 {sum:F2}");
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
        public void Probabilities_Route_To_Best_Or_Worst()
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

            // 확률을 직접 지정해 난수에 기대지 않는다. 레벨 기본값은 튜닝으로 계속
            // 바뀌므로, 특정 레벨이 특정 수를 두는지로 검증하면 밸런스를 만질 때마다 깨진다.
            var alwaysBest = new LeveledAiStrategy(playBest: 1.0, playWorst: 0.0, smartExtra: 1.0);
            var alwaysWorst = new LeveledAiStrategy(playBest: 0.0, playWorst: 1.0, smartExtra: 0.0);

            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(best, alwaysBest.ChoosePrimaryLine(s, PlayerId.One));
                Assert.AreEqual(worst, alwaysWorst.ChoosePrimaryLine(s, PlayerId.One));
            }
        }

        [Test]
        public void Level_Is_Clamped_To_The_Table_Range()
        {
            Assert.AreEqual(DifficultyTable.MinLevel, new LeveledAiStrategy(0).Level);
            Assert.AreEqual(DifficultyTable.MaxLevel, new LeveledAiStrategy(99).Level);
            Assert.AreEqual(10, DifficultyTable.MaxLevel, "AI 범위와 난이도 표 범위는 같아야 한다.");
        }
    }
}
