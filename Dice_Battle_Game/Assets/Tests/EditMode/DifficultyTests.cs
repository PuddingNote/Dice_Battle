using NUnit.Framework;
using DiceBattle.Core;
using DiceBattle.AI;

namespace DiceBattle.Tests
{
    public class DifficultyTests
    {
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
            }

            // 위쪽 끝은 천장이다 — 항상 최선 수.
            Assert.That(LeveledAiStrategy.DefaultPlayBest(10), Is.EqualTo(1.0), "Lv10은 항상 최선 수.");
            Assert.That(LeveledAiStrategy.DefaultSmartExtra(10), Is.EqualTo(1.0));
        }

        [Test]
        public void Easiest_Level_Is_Still_An_Opponent()
        {
            // 거의 매 수를 최악으로 두는 AI는 상대가 아니라 구경거리다.
            // 가장 쉬운 난이도에도 최선 수가 섞여 있어야 한다.
            Assert.Greater(LeveledAiStrategy.DefaultPlayBest(1), 0.0, "Lv1도 최선 수를 섞는다.");
            Assert.Less(LeveledAiStrategy.DefaultPlayWorst(1), 0.5, "최악 수가 과반이면 안 된다.");
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
