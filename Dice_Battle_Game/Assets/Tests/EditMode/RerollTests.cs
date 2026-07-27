using System;
using NUnit.Framework;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    /// <summary>리롤(판당 1회 재굴림) 규칙 테스트.</summary>
    public class RerollTests
    {
        [Test]
        public void Reroll_Candidate_Keeps_Special_And_Owner()
        {
            // 선공 첫 주사위는 특수 → 리롤 후보도 특수여야 한다.
            var roller = new QueueDiceRoller(4, 6);
            var game = new DiceGame(roller);
            game.Start(PlayerId.One);

            Dice candidate = game.RollRerollCandidate();

            Assert.AreEqual(6, candidate.Value);
            Assert.IsTrue(candidate.IsSpecial, "특수 주사위를 리롤하면 후보도 특수여야 한다.");
            Assert.AreEqual(PlayerId.One, candidate.Owner);
        }

        [Test]
        public void Reroll_Candidate_Of_Normal_Dice_Is_Normal()
        {
            var roller = new QueueDiceRoller(4, 3, 5);
            var game = new DiceGame(roller);
            game.Start(PlayerId.One);
            game.PlacePrimary(0); // 선공 특수 소진 → 다음은 일반 주사위

            Dice candidate = game.RollRerollCandidate();

            Assert.IsFalse(candidate.IsSpecial, "일반 주사위를 리롤하면 후보도 일반이어야 한다.");
        }

        [Test]
        public void RollRerollCandidate_Does_Not_Change_State()
        {
            var roller = new QueueDiceRoller(4, 6);
            var game = new DiceGame(roller);
            game.Start(PlayerId.One);

            Dice before = game.State.PendingDice;
            game.RollRerollCandidate();

            Assert.AreSame(before, game.State.PendingDice, "후보를 굴려도 대기 주사위는 그대로여야 한다.");
        }

        [Test]
        public void ApplyReroll_Replaces_Pending_Dice()
        {
            var roller = new QueueDiceRoller(4, 6);
            var game = new DiceGame(roller);
            game.Start(PlayerId.One);

            Dice candidate = game.RollRerollCandidate();
            game.ApplyReroll(candidate);

            Assert.AreSame(candidate, game.State.PendingDice);
            Assert.AreEqual(6, game.State.PendingDice.Value);
            Assert.IsTrue(game.State.PendingDice.IsSpecial, "교체 후에도 특수 속성은 유지된다.");
        }

        [Test]
        public void Rerolled_Special_Dice_Still_Immune_To_Removal()
        {
            // P1 선공 특수 4 → 리롤로 3 확정 → line0 배치.
            // P2가 같은 라인에 3을 놓아도 P1의 3은 특수라 제거되지 않는다.
            var roller = new QueueDiceRoller(4, 3, 3);
            var game = new DiceGame(roller);
            game.Start(PlayerId.One);
            game.ApplyReroll(game.RollRerollCandidate());
            game.PlacePrimary(0);

            var result = game.PlacePrimary(0); // P2가 3 배치

            Assert.IsFalse(result.RemovalOccurred, "리롤한 특수 주사위도 제거 면역이어야 한다.");
        }

        [Test]
        public void ApplyReroll_Rejects_Mismatched_Candidate()
        {
            var roller = new QueueDiceRoller(4, 6);
            var game = new DiceGame(roller);
            game.Start(PlayerId.One); // 특수 주사위 대기

            var mismatched = new Dice(6, isSpecial: false, owner: PlayerId.One);

            Assert.Throws<ArgumentException>(() => game.ApplyReroll(mismatched));
        }

        [Test]
        public void Reroll_Throws_When_No_Pending_Dice()
        {
            var roller = new QueueDiceRoller(4);
            var game = new DiceGame(roller);

            Assert.Throws<InvalidOperationException>(() => game.RollRerollCandidate());
        }
    }
}
