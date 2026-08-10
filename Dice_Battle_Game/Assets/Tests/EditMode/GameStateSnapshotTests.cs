using NUnit.Framework;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    /// <summary>
    /// 친선대전의 "풀 상태 스냅샷 동기화"가 의존하는 왕복 변환을 검증한다.
    /// 실제 네트워크(Firebase) 없이도 Capture→Restore가 원래 상태를 정확히
    /// 재현하는지, 그리고 복원된 상태로 DiceGame이 계속 정상 진행되는지를 본다.
    /// </summary>
    public class GameStateSnapshotTests
    {
        [Test]
        public void Restore_Reproduces_Placed_Dice_In_The_Same_Order()
        {
            // P1이 서로 다른 값을 여러 라인에 놓아 placement 순서/그룹화가 갈리게 만든다.
            var game = new DiceGame(new QueueDiceRoller(3, 5, 3, 2, 4, 2));
            game.Start(PlayerId.One);

            game.PlacePrimary(0); // P1: 3 → line0
            game.PlacePrimary(0); // P2: 5 → line0
            game.PlacePrimary(1); // P1: 3 → line1
            game.PlacePrimary(1); // P2: 2 → line1

            var data = GameStateSnapshot.Capture(game.State);
            var restored = GameStateSnapshot.Restore(data);

            for (int p = 0; p < 2; p++)
            {
                var player = (PlayerId)p;
                for (int line = 0; line < Field.LineCount; line++)
                {
                    var original = game.State.Field(player)[line];
                    var copy = restored.Field(player)[line];
                    Assert.AreEqual(original.Count, copy.Count, $"{player} line{line} 개수");
                    for (int i = 0; i < original.Count; i++)
                    {
                        Assert.AreEqual(original.Dice[i].Value, copy.Dice[i].Value);
                        Assert.AreEqual(original.Dice[i].IsSpecial, copy.Dice[i].IsSpecial);
                        Assert.AreEqual(original.Dice[i].Owner, copy.Dice[i].Owner);
                    }
                }
            }
        }

        [Test]
        public void Restore_Reproduces_Turn_Phase_And_Pending_Dice()
        {
            var game = new DiceGame(new QueueDiceRoller(4, 4, 6)); // P1:4→line0 상호소멸 유도 아님, 그냥 진행
            game.Start(PlayerId.One);
            game.PlacePrimary(0); // P1 4, P2 4(다음 턴 대기), 제거 없음 확인용

            var data = GameStateSnapshot.Capture(game.State);
            var restored = GameStateSnapshot.Restore(data);

            Assert.AreEqual(game.State.Phase, restored.Phase);
            Assert.AreEqual(game.State.CurrentPlayer, restored.CurrentPlayer);
            Assert.AreEqual(game.State.FirstPlayer, restored.FirstPlayer);
            Assert.AreEqual(game.State.PendingDice.Value, restored.PendingDice.Value);
            Assert.AreEqual(game.State.PendingDice.IsSpecial, restored.PendingDice.IsSpecial);
            Assert.AreEqual(game.State.PendingDice.Owner, restored.PendingDice.Owner);
        }

        [Test]
        public void Restore_Recomputes_Result_On_GameOver_Without_Transmitting_It()
        {
            // 6,1 반복으로 제거 없이 18턴 만에 종료(GameFlowTests와 같은 패턴).
            var values = new int[18];
            for (int i = 0; i < 18; i++) values[i] = (i % 2 == 0) ? 6 : 1;
            var game = new DiceGame(new QueueDiceRoller(values));
            game.Start(PlayerId.One);
            for (int turn = 0; turn < 18; turn++)
            {
                PlayerId cur = game.State.CurrentPlayer;
                int line = game.State.Field(cur).DiceCount / 3;
                game.PlacePrimary(line);
            }
            Assert.AreEqual(TurnPhase.GameOver, game.State.Phase);

            // Capture는 Result를 담지 않는다 — DiceData/필드 데이터만으로 충분히 재현되는지 확인.
            var data = GameStateSnapshot.Capture(game.State);
            var restored = GameStateSnapshot.Restore(data);

            Assert.IsNull(restored.PendingDice, "게임 종료 상태는 대기 주사위가 없다");
            Assert.IsTrue(restored.Result.HasValue);
            Assert.AreEqual(game.State.Result.Value.Winner, restored.Result.Value.Winner);
            Assert.AreEqual(game.State.Result.Value.PlayerOneLineWins, restored.Result.Value.PlayerOneLineWins);
        }

        [Test]
        public void A_DiceGame_Built_From_A_Restored_State_Can_Keep_Playing()
        {
            // 친선대전의 실제 쓰임: 원격에서 받은 상태로 새 DiceGame을 만들고 이어서 진행한다.
            var game = new DiceGame(new QueueDiceRoller(2, 6));
            game.Start(PlayerId.One);
            game.PlacePrimary(0); // P1: 2 → line0, 제거 없음 → P2 턴으로 넘어감

            var restoredState = GameStateSnapshot.Restore(GameStateSnapshot.Capture(game.State));
            var continued = new DiceGame(new QueueDiceRoller(3), restoredState);

            Assert.AreEqual(PlayerId.Two, continued.State.CurrentPlayer);
            var result = continued.PlacePrimary(1); // P2 계속 진행 — 예외 없이 진행되면 성공
            Assert.IsFalse(result.RemovalOccurred);
            Assert.AreEqual(1, continued.State.Field(PlayerId.Two)[1].Count);
        }
    }
}
