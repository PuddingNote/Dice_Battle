using NUnit.Framework;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    public class TurnOrderTests
    {
        [Test]
        public void Normal_Alternation()
        {
            // 둘 다 안 찼으면 상대에게 넘어간다.
            Assert.AreEqual(PlayerId.Two, TurnOrder.Next(PlayerId.One, false, false));
            Assert.AreEqual(PlayerId.One, TurnOrder.Next(PlayerId.Two, false, false));
        }

        [Test]
        public void Skip_Opponent_When_Opponent_Full()
        {
            // 상대(other)가 가득 찼고 본인은 안 찼으면 본인이 계속 진행.
            Assert.AreEqual(PlayerId.One, TurnOrder.Next(PlayerId.One, currentFieldFull: false, otherFieldFull: true));
        }

        [Test]
        public void Hand_Over_When_Current_Full_But_Other_Not()
        {
            // 본인이 가득 찼고 상대는 안 찼으면 상대에게 넘긴다.
            Assert.AreEqual(PlayerId.Two, TurnOrder.Next(PlayerId.One, currentFieldFull: true, otherFieldFull: false));
        }

        [Test]
        public void Game_Over_When_Both_Full()
        {
            Assert.IsNull(TurnOrder.Next(PlayerId.One, true, true));
            Assert.IsNull(TurnOrder.Next(PlayerId.Two, true, true));
        }
    }
}
