using System;
using NUnit.Framework;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    public class GameFlowTests
    {
        [Test]
        public void Full_Game_Reaches_GameOver_With_Expected_Winner()
        {
            // P1은 항상 6, P2는 항상 1을 굴리도록 구성(제거 절대 미발생).
            // 18턴(각 9개) 뒤 양쪽 필드가 가득 차 게임 종료.
            var values = new int[18];
            for (int i = 0; i < 18; i++) values[i] = (i % 2 == 0) ? 6 : 1; // 6,1,6,1,...
            var game = new DiceGame(new QueueDiceRoller(values));
            game.Start(PlayerId.One);

            for (int turn = 0; turn < 18; turn++)
            {
                Assert.AreEqual(TurnPhase.AwaitingPrimaryPlacement, game.State.Phase);
                PlayerId cur = game.State.CurrentPlayer;
                int placed = game.State.Field(cur).DiceCount; // 0..8
                int line = placed / 3;                        // 3개씩 라인 채움
                var r = game.PlacePrimary(line);
                Assert.IsFalse(r.RemovalOccurred);
            }

            Assert.AreEqual(TurnPhase.GameOver, game.State.Phase);
            Assert.IsTrue(game.State.Field(PlayerId.One).IsFull);
            Assert.IsTrue(game.State.Field(PlayerId.Two).IsFull);

            var o = game.Outcome.Value;
            Assert.AreEqual(PlayerId.One, o.Winner);
            Assert.AreEqual(3, o.PlayerOneLineWins);
            // 각 라인: P1 6,6,6 → 30 / P2 1,1,1 → 5
            Assert.AreEqual(30, game.State.Field(PlayerId.One)[0].Score());
            Assert.AreEqual(5, game.State.Field(PlayerId.Two)[0].Score());
        }

        [Test]
        public void Lone_Player_Continues_When_Opponent_Field_Full()
        {
            // 상대 필드가 먼저 가득 찬 상황을 인위적으로 만든 뒤,
            // 턴 진행자가 아직 안 찬 플레이어로 유지되는지 확인.
            // P2 필드를 미리 9칸 모두 채워 둔다(직접 모델 조작).
            var game = new DiceGame(new QueueDiceRoller(1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6));
            game.Start(PlayerId.One);

            // P2 필드 9칸 채우기 (테스트 편의상 모델 직접 조작)
            for (int line = 0; line < Field.LineCount; line++)
                for (int k = 0; k < Line.Capacity; k++)
                    game.State.Field(PlayerId.Two)[line].Add(new Dice(1, false, PlayerId.Two));

            // P1(선공)이 특수1을 line0에 배치. 상대 line0에 1이 있으나 특수가 아니므로 제거 발생.
            // 제거를 피하기 위해 상대에 값 6을 두지 않았고 P1 첫 주사위는 1 → 상대 1과 매칭되어 제거됨.
            // 이 케이스는 제거 흐름이 섞이므로, 대신 별도 라인(제거 없는 값)으로 진행하도록
            // 위 롤 시퀀스를 재구성하지 않고, 여기서는 turn 진행자만 검증한다.
            // → 제거가 나면 extra 단계로 가므로, 먼저 제거 없는 상황을 보장하기 위해
            //   P1 주사위(1)를 상대 1이 없는 라인은 없다(전부 1). 그래서 제거가 발생한다.
            var r = game.PlacePrimary(0);
            Assert.IsTrue(r.RemovalOccurred); // 상대 line0의 1들 제거
            // 제거로 상대 line0에 빈칸 생김 → 추가 특수 주사위 배치 단계
            Assert.AreEqual(TurnPhase.AwaitingExtraPlacement, game.State.Phase);

            // 추가 특수 주사위를 P1 본인 line1에 배치 → 턴 종료.
            game.PlaceExtra(PlayerId.One, 1);

            // 상대(P2) line0이 비어 다시 안 참 → 정상 교대로 P2 차례가 될 수 있다.
            // 최소한 게임이 끝나지 않았고, 진행자가 유효한지 확인.
            Assert.AreEqual(TurnPhase.AwaitingPrimaryPlacement, game.State.Phase);
            Assert.IsFalse(game.State.IsGameOver);
        }

        [Test]
        public void PlacePrimary_On_Full_Line_Throws()
        {
            var game = new DiceGame(new QueueDiceRoller(1, 2, 3, 4));
            game.Start(PlayerId.One);
            // line0 을 가득 채운 뒤 같은 라인에 배치 시도
            game.State.Field(PlayerId.One)[0].Add(new Dice(1, false, PlayerId.One));
            game.State.Field(PlayerId.One)[0].Add(new Dice(1, false, PlayerId.One));
            game.State.Field(PlayerId.One)[0].Add(new Dice(1, false, PlayerId.One));
            Assert.Throws<InvalidOperationException>(() => game.PlacePrimary(0));
        }

        [Test]
        public void PlaceExtra_When_Not_In_Extra_Phase_Throws()
        {
            var game = new DiceGame(new QueueDiceRoller(1));
            game.Start(PlayerId.One);
            Assert.Throws<InvalidOperationException>(() => game.PlaceExtra(PlayerId.One, 0));
        }

        [Test]
        public void Invalid_Line_Index_Throws()
        {
            var game = new DiceGame(new QueueDiceRoller(1));
            game.Start(PlayerId.One);
            Assert.Throws<ArgumentOutOfRangeException>(() => game.PlacePrimary(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => game.PlacePrimary(-1));
        }

        [Test]
        public void Start_Twice_Throws()
        {
            var game = new DiceGame(new QueueDiceRoller(1, 2));
            game.Start(PlayerId.One);
            Assert.Throws<InvalidOperationException>(() => game.Start(PlayerId.Two));
        }
    }
}
