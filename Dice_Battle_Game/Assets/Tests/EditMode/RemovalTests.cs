using NUnit.Framework;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    public class RemovalTests
    {
        [Test]
        public void Mutual_Removal_Placing_Normal_Die_Is_Also_Removed()
        {
            // 상호 소멸: 일반 주사위로 제거 시 배치 주사위도 사라진다.
            // 롤: Start(P1)=1, P2=5, P1=5
            var game = new DiceGame(new QueueDiceRoller(1, 5, 5));
            game.Start(PlayerId.One);
            game.PlacePrimary(0); // P1 특수1 → line0
            game.PlacePrimary(1); // P2 5 → line1
            var r = game.PlacePrimary(1); // P1 5 → line1, 상대 5 제거 + 내 5 소멸
            Assert.IsTrue(r.RemovalOccurred);
            Assert.AreEqual(0, game.State.Field(PlayerId.One)[1].Count, "배치 주사위도 소멸해야 한다.");
            Assert.AreEqual(0, game.State.Field(PlayerId.Two)[1].Count, "상대 주사위 제거.");
        }

        [Test]
        public void Special_Placing_Die_Survives_Mutual_Removal()
        {
            // 배치 주사위가 특수면 상대를 제거하되 자신은 남는다(기획서 9번).
            // 선공 첫 주사위는 특수이므로, 상대 라인에 미리 일반 주사위를 두고 검증.
            var g = new DiceGame(new QueueDiceRoller(3, 2));
            g.Start(PlayerId.One); // P1 첫 손패=특수3
            g.State.Field(PlayerId.Two)[0].Add(new Dice(3, false, PlayerId.Two)); // 상대 일반 3
            var r = g.PlacePrimary(0); // 특수3 배치 → 상대 3 제거, 특수 생존
            Assert.IsTrue(r.RemovalOccurred);
            Assert.AreEqual(1, g.State.Field(PlayerId.One)[0].Count, "특수 배치 주사위는 생존.");
            Assert.AreEqual(0, g.State.Field(PlayerId.Two)[0].Count, "상대 3 제거.");
        }

        [Test]
        public void Placing_Matching_Value_Removes_Opponent_And_Grants_Special_Extra()
        {
            // P1 선공 첫 주사위=3(특수). P1이 line0에 배치(제거 대상 없음) → 턴 종료.
            // P2 주사위=3. P2가 line0에 배치 → P1 line0의 3은 "특수"라 제거되지 않음(별도 테스트).
            // 여기서는 일반 주사위 간 제거를 검증하기 위해 시퀀스를 구성한다.
            //
            // 롤 순서: Start(P1)=3, P2턴=5, P1턴=5, ...
            var roller = new QueueDiceRoller(3, 5, 5, /*extra*/ 2);
            var game = new DiceGame(roller);
            game.Start(PlayerId.One);

            // P1: 특수 3을 line0
            var r1 = game.PlacePrimary(0);
            Assert.IsFalse(r1.RemovalOccurred);
            Assert.AreEqual(TurnPhase.AwaitingPrimaryPlacement, game.State.Phase);
            Assert.AreEqual(PlayerId.Two, game.State.CurrentPlayer);

            // P2: 일반 5를 line1 (제거 없음)
            var r2 = game.PlacePrimary(1);
            Assert.IsFalse(r2.RemovalOccurred);
            Assert.AreEqual(PlayerId.One, game.State.CurrentPlayer);

            // P1: 일반 5를 line1 → 상대(P2) line1의 5(일반) 제거 발생
            var r3 = game.PlacePrimary(1);
            Assert.IsTrue(r3.RemovalOccurred);
            Assert.AreEqual(1, r3.RemovedCount);
            Assert.IsTrue(r3.ExtraDicePending);
            Assert.AreEqual(TurnPhase.AwaitingExtraPlacement, game.State.Phase);
            Assert.AreEqual(0, game.State.Field(PlayerId.Two)[1].Count); // 제거되어 비었음

            // 추가 주사위는 특수 주사위여야 한다.
            Assert.IsNotNull(game.State.PendingDice);
            Assert.IsTrue(game.State.PendingDice.IsSpecial);
        }

        [Test]
        public void Removes_All_Same_Value_Dice_In_Line()
        {
            // P2 line0 에 일반 4가 2개 있는 상태에서 P1이 4를 배치하면 둘 다 제거.
            // 롤: Start(P1)=1, P2=4, P1=6, P2=4, P1=4, extra=2
            var roller = new QueueDiceRoller(1, 4, 6, 4, 4, 2);
            var game = new DiceGame(roller);
            game.Start(PlayerId.One);

            game.PlacePrimary(2); // P1 특수1 → line2 (관계없음)
            game.PlacePrimary(0); // P2 4 → line0
            game.PlacePrimary(2); // P1 6 → line2 (제거 없음)
            game.PlacePrimary(0); // P2 4 → line0 (이제 P2 line0 = 4,4)
            Assert.AreEqual(2, game.State.Field(PlayerId.Two)[0].Count);

            var r = game.PlacePrimary(0); // P1 4 → line0, 상대 4 두 개 모두 제거
            Assert.IsTrue(r.RemovalOccurred);
            Assert.AreEqual(2, r.RemovedCount);
            Assert.AreEqual(0, game.State.Field(PlayerId.Two)[0].Count);
        }

        [Test]
        public void Only_Same_Line_Is_Affected()
        {
            // 상대의 "다른 라인"에 같은 숫자가 있어도 제거되지 않는다.
            // 롤: Start(P1)=1, P2=5, P1=5
            var roller = new QueueDiceRoller(1, 5, 5);
            var game = new DiceGame(roller);
            game.Start(PlayerId.One);

            game.PlacePrimary(0); // P1 특수1 → line0
            game.PlacePrimary(2); // P2 5 → line2
            var r = game.PlacePrimary(0); // P1 5 → line0. 상대 line0엔 5 없음 → 제거 없음
            Assert.IsFalse(r.RemovalOccurred);
            Assert.AreEqual(1, game.State.Field(PlayerId.Two)[2].Count); // line2의 5는 그대로
        }

        [Test]
        public void Extra_Dice_Does_Not_Trigger_Second_Removal()
        {
            // 턴당 제거 1회 제한: 추가 주사위가 상대의 동일 숫자 위에 놓여도 제거 안 됨.
            // 구성:
            //  Start(P1)=1 → line1
            //  P2=6 → line0  (P1이 나중에 제거할 대상)
            //  P1=2 → line2  (관계없음)
            //  P2=2 → line0  (추가 주사위가 나중에 겹칠 값)
            //  P1=6 → line0  → 상대 line0의 6 제거 발생, 추가 특수 주사위=2 획득
            //  추가 2를 "상대 line0"에 배치 → 상대 line0엔 일반 2가 있지만 제거되지 않아야 함
            var roller = new QueueDiceRoller(1, 6, 2, 2, 6, /*extra*/ 2);
            var game = new DiceGame(roller);
            game.Start(PlayerId.One);

            game.PlacePrimary(1); // P1 특수1 → line1
            game.PlacePrimary(0); // P2 6 → line0
            game.PlacePrimary(2); // P1 2 → line2
            game.PlacePrimary(0); // P2 2 → line0 (P2 line0 = 6,2)
            var r = game.PlacePrimary(0); // P1 6 → line0 → 6 제거
            Assert.IsTrue(r.RemovalOccurred);
            Assert.AreEqual(1, r.RemovedCount);
            // 남은 P2 line0 = [2]
            Assert.AreEqual(1, game.State.Field(PlayerId.Two)[0].Count);

            // 추가 특수 2를 상대 line0에 배치
            game.PlaceExtra(PlayerId.Two, 0);
            // 제거되지 않아야 함: P2 line0 = [2(일반), 2(특수)]
            Assert.AreEqual(2, game.State.Field(PlayerId.Two)[0].Count);
            Assert.AreEqual(TurnPhase.AwaitingPrimaryPlacement, game.State.Phase); // 턴 종료 후 다음 턴
        }
    }
}
