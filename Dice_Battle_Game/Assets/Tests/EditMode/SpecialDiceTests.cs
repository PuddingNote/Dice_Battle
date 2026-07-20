using NUnit.Framework;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    public class SpecialDiceTests
    {
        [Test]
        public void FirstPlayers_First_Dice_Is_Special()
        {
            var roller = new QueueDiceRoller(4);
            var game = new DiceGame(roller);
            game.Start(PlayerId.One);

            Assert.IsNotNull(game.State.PendingDice);
            Assert.IsTrue(game.State.PendingDice.IsSpecial, "선공 첫 주사위는 특수여야 한다.");
            Assert.AreEqual(4, game.State.PendingDice.Value);
        }

        [Test]
        public void Normal_Turn_Dice_Is_Not_Special()
        {
            var roller = new QueueDiceRoller(4, 3);
            var game = new DiceGame(roller);
            game.Start(PlayerId.One);
            game.PlacePrimary(0); // 선공 특수 배치 → 다음 턴 주사위 굴림

            Assert.IsNotNull(game.State.PendingDice);
            Assert.IsFalse(game.State.PendingDice.IsSpecial, "일반 턴 주사위는 특수가 아니어야 한다.");
        }

        [Test]
        public void Special_Dice_Is_Immune_To_Removal()
        {
            // P1 선공 특수 3을 line0에 두고, P2가 같은 line0에 3을 배치해도
            // P1의 3은 특수라 제거되지 않는다 → 제거/추가주사위 없음.
            var roller = new QueueDiceRoller(3, 3);
            var game = new DiceGame(roller);
            game.Start(PlayerId.One);

            game.PlacePrimary(0); // P1 특수3 → line0
            var r = game.PlacePrimary(0); // P2 3 → line0, 상대(P1) 특수3 제거 시도

            Assert.IsFalse(r.RemovalOccurred, "특수 주사위는 제거되지 않아야 한다.");
            Assert.AreEqual(1, game.State.Field(PlayerId.One)[0].Count); // 특수3 그대로
            Assert.AreEqual(TurnPhase.AwaitingPrimaryPlacement, game.State.Phase);
        }

        [Test]
        public void Extra_Dice_Can_Be_Placed_On_Opponent_Field()
        {
            // 제거 발생 후 추가 특수 주사위를 상대 필드에 배치할 수 있어야 한다.
            var roller = new QueueDiceRoller(1, 5, 5, /*extra*/ 6);
            var game = new DiceGame(roller);
            game.Start(PlayerId.One);

            game.PlacePrimary(0); // P1 특수1 → line0
            game.PlacePrimary(1); // P2 5 → line1
            var r = game.PlacePrimary(1); // P1 5 → line1 → 상대 5 제거, 추가 특수6 획득
            Assert.IsTrue(r.ExtraDicePending);

            // 추가 특수6을 상대(P2) line2에 배치
            game.PlaceExtra(PlayerId.Two, 2);
            var placed = game.State.Field(PlayerId.Two)[2].Dice[0];
            Assert.AreEqual(6, placed.Value);
            Assert.IsTrue(placed.IsSpecial);
        }
    }
}
