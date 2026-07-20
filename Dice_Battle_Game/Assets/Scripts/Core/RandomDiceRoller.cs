using System;

namespace DiceBattle.Core
{
    /// <summary>System.Random 기반 기본 주사위 굴림(1~6). 시드로 재현 가능.</summary>
    public sealed class RandomDiceRoller : IDiceRoller
    {
        private readonly Random _rng;

        public RandomDiceRoller() : this(new Random()) { }
        public RandomDiceRoller(int seed) : this(new Random(seed)) { }
        public RandomDiceRoller(Random rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        public int Roll() => _rng.Next(Dice.MinValue, Dice.MaxValue + 1);
    }
}
