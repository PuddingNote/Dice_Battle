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
        private readonly double _lowBias; // 0=공정, 1=항상 두 번 굴려 작은 값(강한 하향)

        public DifficultyDiceRoller(PlayerId weightedPlayer, int level)
            : this(weightedPlayer, DefaultLowBias(level), new Random()) { }

        public DifficultyDiceRoller(PlayerId weightedPlayer, int level, Random rng)
            : this(weightedPlayer, DefaultLowBias(level), rng) { }

        /// <summary>명시적 하향 편향(0~1) 지정.</summary>
        public DifficultyDiceRoller(PlayerId weightedPlayer, double lowBias, Random rng)
        {
            _weightedPlayer = weightedPlayer;
            _lowBias = lowBias < 0 ? 0 : (lowBias > 1 ? 1 : lowBias);
            _rng = rng ?? new Random();
        }

        /// <summary>레벨별 기본 하향 편향(Inspector 미설정 시 사용).</summary>
        public static double DefaultLowBias(int level)
        {
            switch (level)
            {
                case 1: return 1.0;
                case 2: return 0.6;
                case 3: return 0.4;
                case 4: return 0.2;
                default: return 0.0;
            }
        }

        public int Roll(PlayerId player)
        {
            if (player != _weightedPlayer)
                return Uniform();
            // lowBias 확률로 두 번 굴려 작은 값(하향), 아니면 공정.
            return _rng.NextDouble() < _lowBias ? Min2() : Uniform();
        }

        private int Uniform() => _rng.Next(Dice.MinValue, Dice.MaxValue + 1);
        private int Min2() => Math.Min(Uniform(), Uniform());
    }
}
