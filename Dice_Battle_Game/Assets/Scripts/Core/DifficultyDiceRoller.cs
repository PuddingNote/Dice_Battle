using System;

namespace DiceBattle.Core
{
    /// <summary>
    /// 난이도(1~5) 가중 주사위 롤러.
    /// 지정한 "가중 플레이어"(보통 AI)만 레벨에 따라 분포가 달라지고,
    /// 그 외 플레이어(사람)는 항상 공정한 1~6 균등 분포로 굴린다.
    /// AI가 유리해지는 구간은 두지 않는다(최고 난이도도 공정한 주사위).
    ///   Lv1: 낮은 값에 강하게 편향(두 번 굴려 작은 값)   → AI 크게 불리(쉬움)
    ///   Lv2: 낮은 값에 편향
    ///   Lv3: 낮은 값에 약하게 편향
    ///   Lv4~5: 공정(균등)                               → 배치 실력으로만 강해짐
    /// </summary>
    public sealed class DifficultyDiceRoller : IDiceRoller
    {
        private readonly Random _rng;
        private readonly PlayerId _weightedPlayer;
        private readonly int _level;

        public DifficultyDiceRoller(PlayerId weightedPlayer, int level)
            : this(weightedPlayer, level, new Random()) { }

        public DifficultyDiceRoller(PlayerId weightedPlayer, int level, Random rng)
        {
            _weightedPlayer = weightedPlayer;
            _level = level < 1 ? 1 : (level > 5 ? 5 : level);
            _rng = rng ?? new Random();
        }

        public int Roll(PlayerId player)
        {
            if (player != _weightedPlayer)
                return Uniform();

            switch (_level)
            {
                case 1: return Min2();                                        // 강한 하향
                case 2: return _rng.NextDouble() < 0.6 ? Min2() : Uniform();  // 하향
                case 3: return _rng.NextDouble() < 0.4 ? Min2() : Uniform();  // 중간 하향
                case 4: return _rng.NextDouble() < 0.2 ? Min2() : Uniform();  // 약한 하향
                default: return Uniform();                                    // Lv5 공정
            }
        }

        private int Uniform() => _rng.Next(Dice.MinValue, Dice.MaxValue + 1);
        private int Min2() => Math.Min(Uniform(), Uniform());
    }
}
