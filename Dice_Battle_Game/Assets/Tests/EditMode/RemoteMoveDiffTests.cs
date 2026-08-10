using NUnit.Framework;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    /// <summary>
    /// 친선대전 상대 턴 연출 재현이 의존하는 <see cref="RemoteMoveDiff"/>를 검증한다.
    /// GameState의 internal setter로 원하는 국면을 직접 구성해(InternalsVisibleTo),
    /// 실제 게임 흐름을 처음부터 재생하지 않고도 각 모양(배치만/제거 동반/견제 배치/
    /// 자기 파괴 제거/변화 없음)을 정확히 만든다.
    /// </summary>
    public class RemoteMoveDiffTests
    {
        [Test]
        public void Compute_Detects_Simple_Placement_Without_Removal()
        {
            var state = new GameState
            {
                FirstPlayer = PlayerId.One,
                CurrentPlayer = PlayerId.One,
                Phase = TurnPhase.AwaitingPrimaryPlacement,
                PendingDice = new Dice(3, true, PlayerId.One),
            };
            var before = GameStateSnapshot.Capture(state);

            var game = new DiceGame(new QueueDiceRoller(1), state);
            game.PlacePrimary(0); // 상대 line0가 비어 있어 제거 없음

            var after = GameStateSnapshot.Capture(game.State);
            var diff = RemoteMoveDiff.Compute(before, after);

            Assert.IsNotNull(diff);
            Assert.IsFalse(diff.RemovalOccurred);
            Assert.AreEqual(PlayerId.One, diff.PlaceField);
            Assert.AreEqual(0, diff.Line);
            Assert.AreEqual(0, diff.InsertIndex);
            Assert.AreEqual(3, diff.Placed.Value);
            Assert.IsTrue(diff.Placed.IsSpecial);
            Assert.AreEqual(PlayerId.One, diff.Placed.Owner);
            Assert.AreEqual(0, diff.Shifted.Length);
        }

        [Test]
        public void Compute_Detects_Grouped_Insert_With_Shift()
        {
            var state = new GameState
            {
                FirstPlayer = PlayerId.One,
                CurrentPlayer = PlayerId.One,
                Phase = TurnPhase.AwaitingPrimaryPlacement,
                PendingDice = new Dice(2, false, PlayerId.One),
            };
            state.Field(PlayerId.One)[0].Add(new Dice(2, false, PlayerId.One));
            state.Field(PlayerId.One)[0].Add(new Dice(5, false, PlayerId.One));
            var before = GameStateSnapshot.Capture(state);

            var game = new DiceGame(new QueueDiceRoller(1), state);
            game.PlacePrimary(0); // 기존 2 뒤에 그룹 삽입 → 5가 한 칸 밀린다

            var after = GameStateSnapshot.Capture(game.State);
            var diff = RemoteMoveDiff.Compute(before, after);

            Assert.IsNotNull(diff);
            Assert.IsFalse(diff.RemovalOccurred);
            Assert.AreEqual(1, diff.InsertIndex);
            Assert.AreEqual(2, diff.Placed.Value);
            Assert.AreEqual(1, diff.Shifted.Length);
            Assert.AreEqual(5, diff.Shifted[0].Value);
        }

        [Test]
        public void Compute_Detects_Removal_When_Placer_Survives()
        {
            var state = new GameState
            {
                FirstPlayer = PlayerId.One,
                CurrentPlayer = PlayerId.One,
                Phase = TurnPhase.AwaitingPrimaryPlacement,
                PendingDice = new Dice(4, true, PlayerId.One), // 특수 → 상호 소멸에서 생존
            };
            state.Field(PlayerId.Two)[0].Add(new Dice(4, false, PlayerId.Two));
            var before = GameStateSnapshot.Capture(state);

            var game = new DiceGame(new QueueDiceRoller(1), state); // 제거 후 추가 특수 굴림용
            var result = game.PlacePrimary(0);
            Assert.IsTrue(result.RemovalOccurred);

            var after = GameStateSnapshot.Capture(game.State);
            var diff = RemoteMoveDiff.Compute(before, after);

            Assert.IsNotNull(diff);
            Assert.IsTrue(diff.RemovalOccurred);
            Assert.AreEqual(PlayerId.One, diff.PlaceField);
            Assert.AreEqual(0, diff.Line);
            Assert.AreEqual(4, diff.Placed.Value);
            Assert.IsTrue(diff.Placed.IsSpecial);
            Assert.AreEqual(PlayerId.Two, diff.RemovedField);
            Assert.AreEqual(1, diff.PreRemoval.Length);
            Assert.AreEqual(4, diff.PreRemoval[0].Value);
            Assert.IsTrue(diff.Removed[0]);
        }

        [Test]
        public void Compute_Detects_Removal_When_Placer_Is_Also_Destroyed()
        {
            var state = new GameState
            {
                FirstPlayer = PlayerId.One,
                CurrentPlayer = PlayerId.One,
                Phase = TurnPhase.AwaitingPrimaryPlacement,
                PendingDice = new Dice(4, false, PlayerId.One), // 특수 아님 → 자신도 함께 사라짐
            };
            state.Field(PlayerId.Two)[0].Add(new Dice(4, false, PlayerId.Two));
            var before = GameStateSnapshot.Capture(state);

            var game = new DiceGame(new QueueDiceRoller(1), state);
            var result = game.PlacePrimary(0);
            Assert.IsTrue(result.RemovalOccurred);
            Assert.AreEqual(0, game.State.Field(PlayerId.One)[0].Count, "자기 필드에는 아무 흔적도 안 남는다");

            var after = GameStateSnapshot.Capture(game.State);
            var diff = RemoteMoveDiff.Compute(before, after);

            Assert.IsNotNull(diff, "자기 필드 성장이 없어도 상대 필드 축소만으로 설명할 수 있어야 한다");
            Assert.IsTrue(diff.RemovalOccurred);
            Assert.AreEqual(PlayerId.One, diff.PlaceField);
            Assert.AreEqual(0, diff.Line);
            Assert.AreEqual(4, diff.Placed.Value);
            Assert.IsFalse(diff.Placed.IsSpecial);
            Assert.AreEqual(PlayerId.Two, diff.RemovedField);
            Assert.AreEqual(0, diff.Shifted.Length);
            Assert.IsTrue(diff.Removed[0]);
        }

        [Test]
        public void Compute_Detects_Extra_Placement_Onto_Opponent_Field()
        {
            var state = new GameState
            {
                FirstPlayer = PlayerId.One,
                CurrentPlayer = PlayerId.One,
                Phase = TurnPhase.AwaitingExtraPlacement,
                PendingDice = new Dice(6, true, PlayerId.One),
            };
            var before = GameStateSnapshot.Capture(state);

            var game = new DiceGame(new QueueDiceRoller(), state);
            game.PlaceExtra(PlayerId.Two, 2); // 견제: 내 특수 주사위를 상대 필드에 배치

            var after = GameStateSnapshot.Capture(game.State);
            var diff = RemoteMoveDiff.Compute(before, after);

            Assert.IsNotNull(diff);
            Assert.IsFalse(diff.RemovalOccurred);
            Assert.AreEqual(PlayerId.Two, diff.PlaceField, "주사위가 실제로 놓인 필드(상대)여야 한다");
            Assert.AreEqual(2, diff.Line);
            Assert.AreEqual(6, diff.Placed.Value);
            Assert.AreEqual(PlayerId.One, diff.Placed.Owner, "놓은 사람은 여전히 P1이다");
        }

        [Test]
        public void Compute_Returns_Null_When_Nothing_Changed()
        {
            var state = new GameState
            {
                FirstPlayer = PlayerId.One,
                CurrentPlayer = PlayerId.One,
                Phase = TurnPhase.AwaitingPrimaryPlacement,
                PendingDice = new Dice(3, true, PlayerId.One),
            };
            var a = GameStateSnapshot.Capture(state);
            var b = GameStateSnapshot.Capture(state); // 별개 인스턴스, 내용은 동일 — 내가 쓴 값의 메아리 상황

            Assert.IsTrue(GameStateSnapshot.StatesEqual(a, b));
            Assert.IsNull(RemoteMoveDiff.Compute(a, b));
        }

        [Test]
        public void StatesEqual_Ignores_LastReroll()
        {
            var state = new GameState
            {
                FirstPlayer = PlayerId.One,
                CurrentPlayer = PlayerId.One,
                Phase = TurnPhase.AwaitingPrimaryPlacement,
                PendingDice = new Dice(3, true, PlayerId.One),
            };
            var a = GameStateSnapshot.Capture(state);
            var b = GameStateSnapshot.Capture(state);
            b.LastReroll = new RerollData { Value = 5, IsSpecial = false, Picked = true };

            Assert.IsTrue(GameStateSnapshot.StatesEqual(a, b),
                "LastReroll은 연출 재현 전용 부가 정보라 메아리 판별에서 제외해야 한다");
        }
    }
}
