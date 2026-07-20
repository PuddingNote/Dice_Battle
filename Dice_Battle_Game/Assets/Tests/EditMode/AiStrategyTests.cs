using System;
using NUnit.Framework;
using DiceBattle.Core;
using DiceBattle.AI;

namespace DiceBattle.Tests
{
    public class AiStrategyTests
    {
        [Test]
        public void Heuristic_Prefers_Removal_Line()
        {
            // 상대 line1 에 일반 5가 있고, 이번 주사위가 5라면 제거 가능한 line1 을 골라야 한다.
            // 롤: Start(P1)=1, P2=5, P1=5
            var game = new DiceGame(new QueueDiceRoller(1, 5, 5));
            game.Start(PlayerId.One);
            game.PlacePrimary(0); // P1 특수1 → line0
            game.PlacePrimary(1); // P2 5 → line1
            // 이제 P1 차례, 손패=5. 상대 line1 의 5를 제거할 수 있는 line1 을 선택해야 한다.
            var ai = new HeuristicAiStrategy();
            int line = ai.ChoosePrimaryLine(game.State, PlayerId.One);
            Assert.AreEqual(1, line);
        }

        [Test]
        public void Heuristic_Forms_Double_When_It_Is_The_Only_Winning_Move()
        {
            // 제거가 불가능하고, 더블을 만들어야만 라인을 이길 수 있는 상황.
            // 최종 배치 직전 상태(P1 결정 손패=4):
            //   P1: line0=[4], line1=[1], line2=[1]
            //   P2: line0=[6], line1=[6], line2=[6]
            // line0 에 4를 더하면 [4,4]=12 로 6을 이겨 유일하게 승리 라인 확보 → line0 선택해야 한다.
            // 롤: P1 first. [1,6,1,6,4,6,4]
            var game = new DiceGame(new QueueDiceRoller(1, 6, 1, 6, 4, 6, 4));
            game.Start(PlayerId.One);
            game.PlacePrimary(1); // t1 P1 특수1 → line1
            game.PlacePrimary(0); // t2 P2 6 → line0
            game.PlacePrimary(2); // t3 P1 1 → line2
            game.PlacePrimary(1); // t4 P2 6 → line1
            game.PlacePrimary(0); // t5 P1 4 → line0
            game.PlacePrimary(2); // t6 P2 6 → line2

            // t7 P1 결정, 손패=4. 제거 불가(상대는 6뿐).
            var ai = new HeuristicAiStrategy();
            int line = ai.ChoosePrimaryLine(game.State, PlayerId.One);
            Assert.AreEqual(0, line);
        }

        [Test]
        public void Random_Vs_Random_Game_Completes()
        {
            for (int seed = 0; seed < 25; seed++)
            {
                var game = new DiceGame(new RandomDiceRoller(seed));
                var p1 = new RandomAiStrategy(seed * 2 + 1);
                var p2 = new RandomAiStrategy(seed * 2 + 2);

                Assert.DoesNotThrow(() =>
                {
                    var outcome = HeadlessGameRunner.Play(game, PlayerId.One, p1, p2);
                    // 종료 시 양쪽 필드가 가득 차 있어야 한다.
                    Assert.IsTrue(game.State.Field(PlayerId.One).IsFull);
                    Assert.IsTrue(game.State.Field(PlayerId.Two).IsFull);
                    Assert.AreEqual(TurnPhase.GameOver, game.State.Phase);
                    // 라인 결과 배열 길이 확인.
                    Assert.AreEqual(Field.LineCount, outcome.Lines.Count);
                });
            }
        }

        [Test]
        public void Heuristic_Vs_Heuristic_Game_Completes()
        {
            for (int seed = 0; seed < 25; seed++)
            {
                var game = new DiceGame(new RandomDiceRoller(seed));
                var p1 = new HeuristicAiStrategy();
                var p2 = new HeuristicAiStrategy();

                Assert.DoesNotThrow(() =>
                {
                    HeadlessGameRunner.Play(game, PlayerId.One, p1, p2);
                    Assert.IsTrue(game.State.BothFieldsFull);
                    Assert.AreEqual(TurnPhase.GameOver, game.State.Phase);
                });
            }
        }

        [Test]
        public void Heuristic_Is_Deterministic_For_Same_Seed()
        {
            // 같은 주사위 시드 + 결정적 전략 → 결과가 재현되어야 한다.
            MatchOutcome Run()
            {
                var game = new DiceGame(new RandomDiceRoller(1234));
                return HeadlessGameRunner.Play(
                    game, PlayerId.One, new HeuristicAiStrategy(), new HeuristicAiStrategy());
            }

            var a = Run();
            var b = Run();
            Assert.AreEqual(a.Winner, b.Winner);
            Assert.AreEqual(a.PlayerOneLineWins, b.PlayerOneLineWins);
            Assert.AreEqual(a.PlayerTwoLineWins, b.PlayerTwoLineWins);
        }

        [Test]
        public void Heuristic_Generally_Beats_Random_Over_Many_Games()
        {
            // 규칙 기반(보통)이 무작위(낮음)보다 유의미하게 강해야 한다.
            int heuristicWins = 0;
            int randomWins = 0;
            int draws = 0;
            const int games = 60;

            for (int seed = 0; seed < games; seed++)
            {
                var game = new DiceGame(new RandomDiceRoller(seed));
                // P1 = 휴리스틱, P2 = 랜덤
                var outcome = HeadlessGameRunner.Play(
                    game, PlayerId.One,
                    new HeuristicAiStrategy(),
                    new RandomAiStrategy(seed + 500));

                if (outcome.Winner == PlayerId.One) heuristicWins++;
                else if (outcome.Winner == PlayerId.Two) randomWins++;
                else draws++;
            }

            // 결정적 우세까지는 아니어도, 휴리스틱이 랜덤보다 확실히 많이 이겨야 한다.
            Assert.Greater(heuristicWins, randomWins,
                $"heuristic={heuristicWins}, random={randomWins}, draws={draws}");
        }
    }
}
