using NUnit.Framework;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    public class ScoreCalculatorTests
    {
        private static Line LineOf(params int[] values)
        {
            var line = new Line();
            foreach (int v in values)
                line.Add(new Dice(v, isSpecial: false, owner: PlayerId.One));
            return line;
        }

        [Test]
        public void Empty_Is_Zero()
        {
            Assert.AreEqual(0, LineOf().Score());
        }

        [Test]
        public void Single_Is_Value()
        {
            Assert.AreEqual(5, LineOf(5).Score());
        }

        [Test]
        public void NoBonus_Sum_Only()
        {
            // 1,2,3 → 6 (보너스 없음)
            Assert.AreEqual(6, LineOf(1, 2, 3).Score());
        }

        [Test]
        public void Double_Adds_Value_Once()
        {
            // 기획서 예시: 4,4,2 → 4+4+2+(4*1) = 14
            Assert.AreEqual(14, LineOf(4, 4, 2).Score());
            // 기획서 예시: 2,2,1 → 2+2+1+(2*1) = 7
            Assert.AreEqual(7, LineOf(2, 2, 1).Score());
        }

        [Test]
        public void Double_Of_Two_Dice_Only()
        {
            // 3,3 → 3+3+(3*1) = 9
            Assert.AreEqual(9, LineOf(3, 3).Score());
        }

        [Test]
        public void Triple_Adds_Value_Twice()
        {
            // 기획서 예시: 4,4,4 → 4+4+4+(4*2) = 20
            Assert.AreEqual(20, LineOf(4, 4, 4).Score());
            // 기획서 예시: 2,2,2 → 2+2+2+(2*2) = 10
            Assert.AreEqual(10, LineOf(2, 2, 2).Score());
        }

        [Test]
        public void Special_Dice_Also_Counts_For_Score()
        {
            // 특수 주사위는 "제거"만 면제될 뿐 점수 계산에는 정상 포함된다.
            var line = new Line();
            line.Add(new Dice(6, isSpecial: true, owner: PlayerId.One));
            line.Add(new Dice(6, isSpecial: false, owner: PlayerId.One));
            // 6,6 → 6+6+(6*1) = 18
            Assert.AreEqual(18, line.Score());
        }
    }
}
