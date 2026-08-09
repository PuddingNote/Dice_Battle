using System;
using NUnit.Framework;
using DiceBattle.Core;
using DiceBattle.AI;

namespace DiceBattle.Tests
{
    public class AiStrategyTests
    {
        /// <summary>테스트 편의: 해당 라인에 주사위들을 직접 배치.</summary>
        private static void Put(GameState s, PlayerId p, int line, params int[] values)
        {
            foreach (int v in values)
                s.Field(p)[line].Add(new Dice(v, false, p));
        }

        private static GameState StateWithPending(int pendingValue, bool special = false)
        {
            var s = new GameState();
            s.CurrentPlayer = PlayerId.One;
            s.Phase = TurnPhase.AwaitingPrimaryPlacement;
            s.PendingDice = new Dice(pendingValue, special, PlayerId.One);
            return s;
        }

        [Test]
        public void Heuristic_Removes_To_Flip_A_Losing_Line()
        {
            // 제거로 지던 라인을 뒤집을 수 있으면 제거를 선택해야 한다.
            //   P1: line0=[5]  (다른 라인 비어 있음)
            //   P2: line0=[6], line1=[5,5], line2=[5,5]
            // 손패=6 을 line0 에 놓으면 상대 6 제거 → 상대 line0=0, 내 5가 남아 라인0 우세.
            //
            // <b>line1/2 는 5로 채워 둔다.</b> 예전에는 여기도 6이라 손패로 지울 수 있었고,
            // 그쪽이 2개 18점으로 훨씬 큰 제거였다. 즉 "어느 제거를 고르는가"를 묻는 판이
            // 되어 있었는데, 이 테스트가 확인하려는 것은 "제거를 고르는가" 하나다.
            // 제거 후보를 line0 하나로 좁혀 그것만 남긴다.
            var s = StateWithPending(6);
            Put(s, PlayerId.One, 0, 5);
            Put(s, PlayerId.Two, 0, 6);
            Put(s, PlayerId.Two, 1, 5, 5);
            Put(s, PlayerId.Two, 2, 5, 5);

            var ai = new HeuristicAiStrategy();
            Assert.AreEqual(0, ai.ChoosePrimaryLine(s, PlayerId.One));
        }

        [Test]
        public void Heuristic_Prefers_The_Bigger_Removal()
        {
            // 제거 후보가 여럿이면 큰 쪽을 고른다.
            //   P1: line0=[5]
            //   P2: line0=[6], line1=[6,6], line2=[6,6]
            // line0 은 1개 6점, line1/2 는 2개 18점을 지운다.
            // 작은 쪽을 고르면 18점짜리 두 줄이 그대로 남아 사실상 진 판이 된다.
            var s = StateWithPending(6);
            Put(s, PlayerId.One, 0, 5);
            Put(s, PlayerId.Two, 0, 6);
            Put(s, PlayerId.Two, 1, 6, 6);
            Put(s, PlayerId.Two, 2, 6, 6);

            var ai = new HeuristicAiStrategy();
            int pick = ai.ChoosePrimaryLine(s, PlayerId.One);

            Assert.AreNotEqual(0, pick,
                "1개 6점짜리 제거를 고르고 2개 18점짜리 두 줄을 남겼다.");
        }

        [Test]
        public void Heuristic_Forms_Double_When_It_Is_The_Only_Winning_Move()
        {
            // 제거 불가. 더블을 만들어야만 라인을 이길 수 있는 상황.
            //   P1: line0=[4], line1=[1], line2=[1]
            //   P2: line0=[6], line1=[6], line2=[6]
            // line0 에 4 → [4,4]=12 로 6을 이겨 유일한 승리 라인.
            var s = StateWithPending(4);
            Put(s, PlayerId.One, 0, 4);
            Put(s, PlayerId.One, 1, 1);
            Put(s, PlayerId.One, 2, 1);
            Put(s, PlayerId.Two, 0, 6);
            Put(s, PlayerId.Two, 1, 6);
            Put(s, PlayerId.Two, 2, 6);

            var ai = new HeuristicAiStrategy();
            Assert.AreEqual(0, ai.ChoosePrimaryLine(s, PlayerId.One));
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
