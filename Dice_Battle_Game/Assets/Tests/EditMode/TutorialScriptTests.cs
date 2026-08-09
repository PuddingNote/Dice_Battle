using System.Collections.Generic;
using NUnit.Framework;
using DiceBattle.Core;
using DiceBattle.AI;

namespace DiceBattle.Tests
{
    /// <summary>
    /// 튜토리얼 각본이 실제로 의도한 판을 만들어 내는지 확인한다.
    ///
    /// 각본은 주사위 눈·AI의 수·사람이 눌러야 할 곳 셋이 맞물려 돌아간다. 하나만 어긋나도
    /// "제거를 가르치는 순간 상대 줄에 같은 숫자가 없다" 같은 상태가 되는데, 그건 실행해
    /// 봐야만 드러나고 그것도 첫 실행 사용자에게 드러난다. 여기서 규칙 엔진으로 직접 돌려
    /// 결과를 못 박아 둔다.
    /// </summary>
    public class TutorialScriptTests
    {
        private const PlayerId Human = TutorialScript.Human;
        private const PlayerId Ai = TutorialScript.Ai;

        /// <summary>리롤 후보. 굴린 단계와 고르는 단계가 나뉘어 있어 사이에 들고 있어야 한다.</summary>
        private Dice _candidate;

        /// <summary>각본이 끝난 뒤에도 굴림은 계속되므로 폴백이 필요하다.</summary>
        private sealed class AlwaysSix : IDiceRoller
        {
            public int Roll(PlayerId player) => 6;
        }

        /// <summary>각본이 떨어져 AI가 여기로 넘어오면 표시가 남는다.</summary>
        private sealed class CountingAi : IAiStrategy
        {
            public int Calls { get; private set; }

            public int ChoosePrimaryLine(GameState state, PlayerId me)
            {
                Calls++;
                return FirstOpenLine(state, me);
            }

            public ExtraMove ChooseExtraMove(GameState state, PlayerId me)
            {
                Calls++;
                return new ExtraMove(me, FirstOpenLine(state, me));
            }

            private static int FirstOpenLine(GameState state, PlayerId me)
            {
                for (int i = 0; i < Field.LineCount; i++)
                    if (state.Field(me)[i].HasSpace) return i;
                return 0;
            }
        }

        // ---- 각본을 규칙 엔진에 그대로 돌려 본다 ----

        /// <summary>
        /// 사람의 수는 <see cref="TutorialScript.Steps"/>의 행동 단계를 순서대로 따르고,
        /// AI의 수는 <see cref="ScriptedAiStrategy"/>에 맡긴다. 게임 화면이 하는 것과 같은 순서다.
        /// 각본이 다 소비되면 멈춘다 — 그 뒤는 자유 배치 구간이라 정해진 답이 없다.
        /// </summary>
        private DiceGame RunScript(out CountingAi fallbackAi)
        {
            fallbackAi = new CountingAi();
            var roller = new ScriptedDiceRoller(TutorialScript.Dice, new AlwaysSix());
            var ai = new ScriptedAiStrategy(TutorialScript.AiMoves, fallbackAi);

            var game = new DiceGame(roller);
            game.Start(TutorialScript.FirstPlayer);

            Queue<TutorialStep> actions = HumanActions();
            _candidate = null;

            while (actions.Count > 0 && !game.State.IsGameOver)
            {
                if (game.State.CurrentPlayer == Ai) PlayAiTurn(game, ai);
                else PlayHumanStep(game, actions.Dequeue());
            }

            return game;
        }

        private static Queue<TutorialStep> HumanActions()
        {
            var queue = new Queue<TutorialStep>();
            foreach (TutorialStep step in TutorialScript.Steps)
                if (step.Action != TutorialAction.Read) queue.Enqueue(step);

            return queue;
        }

        private static void PlayAiTurn(DiceGame game, IAiStrategy ai)
        {
            if (game.State.Phase == TurnPhase.AwaitingPrimaryPlacement)
            {
                game.PlacePrimary(ai.ChoosePrimaryLine(game.State, Ai));
                return;
            }

            ExtraMove move = ai.ChooseExtraMove(game.State, Ai);
            game.PlaceExtra(move.Field, move.Line);
        }

        private void PlayHumanStep(DiceGame game, TutorialStep step)
        {
            switch (step.Action)
            {
                case TutorialAction.Reroll:
                    // 후보를 굴리기만 한다. 확정은 다음 단계(PickNew)에서.
                    _candidate = game.RollRerollCandidate();
                    break;

                case TutorialAction.PickNew:
                    game.ApplyReroll(_candidate);
                    _candidate = null;
                    break;

                case TutorialAction.PlaceLine:
                    // 상대 줄을 눌러도 규칙상으로는 기본 배치다(제거는 그 결과로 일어난다).
                    if (game.State.Phase == TurnPhase.AwaitingExtraPlacement)
                        game.PlaceExtra(step.Field, step.Line);
                    else
                        game.PlacePrimary(step.Line);
                    break;
            }
        }

        private static int[] Values(GameState state, PlayerId field, int line)
        {
            Line l = state.Field(field)[line];
            var values = new int[l.Count];
            for (int i = 0; i < l.Count; i++) values[i] = l.Dice[i].Value;
            return values;
        }

        // ---- 각본 자체의 앞뒤 ----

        [Test]
        public void Match_Start_Steps_Are_All_At_The_Front()
        {
            // MatchStart는 판이 시작될 때 딱 한 번뿐이다. 그 뒤에 끼워 넣은 안내는
            // 영영 표시되지 않으면서 조용히 다음 단계를 막는다.
            bool leftMatchStart = false;
            foreach (TutorialStep step in TutorialScript.Steps)
            {
                if (step.Beat != TutorialBeat.MatchStart) { leftMatchStart = true; continue; }

                Assert.IsFalse(leftMatchStart,
                    "MatchStart 단계가 다른 지점 뒤에 있다. 그 안내는 표시되지 않는다.");
            }
        }

        [Test]
        public void The_Reroll_Candidate_Differs_From_The_Die_It_Replaces()
        {
            // 같은 눈이면 DiceGame이 "다른 눈"을 찾느라 다음 값까지 삼켜 각본이 밀린다.
            int[] dice = TutorialScript.Dice;
            Assert.AreNotEqual(dice[dice.Length - 2], dice[dice.Length - 1],
                "리롤 후보가 직전 주사위와 같다. 각본이 한 칸씩 밀린다.");
        }

        [Test]
        public void The_Ai_Goes_First()
        {
            // 선공의 첫 주사위는 특수 주사위다. 사람이 선공이면 제거를 배우기도 전에
            // "제거되지 않는 주사위"부터 설명해야 한다.
            Assert.AreEqual(Ai, TutorialScript.FirstPlayer);
        }

        [Test]
        public void One_Step_Points_At_The_Opponent_Field()
        {
            // 제거를 가르치는 단계는 상대 줄을 가리켜야 한다. 내 줄을 가리키면 그냥 배치다.
            bool found = false;
            foreach (TutorialStep step in TutorialScript.Steps)
            {
                if (step.Action != TutorialAction.PlaceLine || step.Field != Ai) continue;
                found = true;
                break;
            }

            Assert.IsTrue(found, "상대 필드를 누르는 단계가 없다. 제거를 가르칠 방법이 없다.");
        }

        // ---- 돌려 본 결과 ----

        [Test]
        public void The_Script_Runs_To_The_End_Without_Falling_Back()
        {
            DiceGame game = RunScript(out CountingAi fallbackAi);

            Assert.AreEqual(0, fallbackAi.Calls,
                "각본이 다 쓰이기 전에 AI가 폴백으로 넘어갔다. AiMoves가 모자라거나 순서가 어긋났다.");
            Assert.IsFalse(game.State.IsGameOver, "각본만으로 판이 끝나 버렸다.");
        }

        [Test]
        public void The_Board_Matches_The_Progress_Table_In_The_Script_Doc()
        {
            DiceGame game = RunScript(out _);
            GameState s = game.State;

            Assert.AreEqual(new[] { 6 }, Values(s, Human, 0));
            Assert.AreEqual(new[] { 6 }, Values(s, Human, 1));
            Assert.AreEqual(new int[0], Values(s, Human, 2));

            Assert.AreEqual(new[] { 2 }, Values(s, Ai, 0));
            Assert.AreEqual(new int[0], Values(s, Ai, 1));
            Assert.AreEqual(new[] { 1 }, Values(s, Ai, 2));

            // 사람의 가운데 줄 6은 제거 보상으로 받은 특수 주사위여야 한다.
            Assert.IsTrue(s.Field(Human)[1].Dice[0].IsSpecial,
                "특수 주사위를 가르치는 단계가 특수 주사위를 놓지 않았다.");
        }

        [Test]
        public void The_Player_Ends_The_Lesson_Ahead_In_Two_Lines()
        {
            // 마지막 안내가 "위 두 줄에서 앞서 있어요"라고 단언한다. 그 말이 사실이어야 한다.
            DiceGame game = RunScript(out _);
            GameState s = game.State;

            Assert.Greater(s.Field(Human)[0].Score(), s.Field(Ai)[0].Score(), "첫째 줄");
            Assert.Greater(s.Field(Human)[1].Score(), s.Field(Ai)[1].Score(), "가운데 줄");
        }

        [Test]
        public void The_Player_Loses_A_Die_To_The_Ai_Along_The_Way()
        {
            // 각본 도중 사람의 5가 상대에게 제거된다. 이 장면이 사라지면
            // "반대로 나도 당할 수 있다"는 안내가 거짓말이 된다.
            DiceGame game = RunScript(out _);

            // 사람이 놓은 주사위는 5, 특수 6, 6 셋인데 판 위에는 둘만 남아 있어야 한다.
            int onBoard = 0;
            for (int i = 0; i < Field.LineCount; i++)
                onBoard += game.State.Field(Human)[i].Count;

            Assert.AreEqual(2, onBoard,
                "사람이 놓은 주사위가 그대로 남아 있다. AI의 제거 장면이 빠졌다.");
        }

        // ---- 각본이 어긋났을 때 ----

        [Test]
        public void The_Ai_Falls_Back_Once_Its_Script_Runs_Out()
        {
            var fallback = new CountingAi();
            var ai = new ScriptedAiStrategy(TutorialScript.AiMoves, fallback);
            var game = new DiceGame(new QueueDiceRoller(1, 1, 1, 1, 1, 1, 1, 1));
            game.Start(Ai);

            for (int i = 0; i < TutorialScript.AiMoves.Length + 2; i++)
                ai.ChoosePrimaryLine(game.State, Ai);

            Assert.Greater(fallback.Calls, 0, "각본이 떨어졌는데도 폴백으로 넘어가지 않았다.");
        }

        [Test]
        public void A_Move_Of_The_Wrong_Kind_Throws_The_Whole_Script_Away()
        {
            // 기본 배치를 물었는데 추가 배치 수가 적혀 있다 = 이미 어긋난 각본이다.
            // 한 수만 건너뛰면 그 뒤가 전부 밀려 더 이상한 판이 되므로 통째로 버려야 한다.
            var fallback = new CountingAi();
            var ai = new ScriptedAiStrategy(
                new[] { TutorialAiMove.Extra(Ai, 2), TutorialAiMove.Primary(1) }, fallback);

            var game = new DiceGame(new QueueDiceRoller(1, 1, 1, 1));
            game.Start(Ai);

            ai.ChoosePrimaryLine(game.State, Ai);
            Assert.AreEqual(1, fallback.Calls, "종류가 어긋났는데 폴백으로 넘어가지 않았다.");

            ai.ChoosePrimaryLine(game.State, Ai);
            Assert.AreEqual(2, fallback.Calls, "각본을 버리지 않고 뒤에 남은 수를 계속 쓰고 있다.");
        }

        [Test]
        public void The_Roller_Hands_Over_When_The_Script_Ends()
        {
            var roller = new ScriptedDiceRoller(new[] { 3, 4 }, new AlwaysSix());

            Assert.AreEqual(3, roller.Roll(Human));
            Assert.AreEqual(4, roller.Roll(Human));
            Assert.IsFalse(roller.HasScript);
            Assert.AreEqual(6, roller.Roll(Human));
        }
    }
}
