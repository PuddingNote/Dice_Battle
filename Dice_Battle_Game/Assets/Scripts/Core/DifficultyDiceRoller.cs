using System;

namespace DiceBattle.Core
{
    /// <summary>
    /// 난이도(1~10) 가중 주사위 롤러.
    /// 지정한 "가중 플레이어"(보통 AI)만 레벨에 따라 분포가 달라지고,
    /// 그 외 플레이어(사람)는 항상 공정한 1~6 균등 분포로 굴린다.
    ///
    /// <b>AI가 유리해지는 구간은 두지 않는다.</b> 최고 난이도가 공정한 주사위이고,
    /// 그 아래로는 AI를 불리하게 만들어 난이도를 낮춘다. 운이 크게 작용하는 게임에서
    /// "상대 주사위가 더 잘 나와서 졌다"는 건 가장 나쁜 패배감이기 때문이다.
    ///   Lv1  : 낮은 값에 편향(0.7) → AI 불리(가장 쉬움)
    ///   Lv2~9: 편향이 고르게 옅어짐
    ///   Lv10 : 공정(균등)          → 배치 실력으로만 상대함
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

        /// <summary>
        /// 레벨별 기본 하향 편향(Inspector 미설정 시 사용).
        ///
        /// Lv1의 0.7에서 <b>Lv10의 0까지</b> 고르게 줄어든다.
        /// 시작을 0.7로 낮춘 것은, 더 세게 주면 AI가 낮은 눈만 뽑는 게 눈에 띄어
        /// 게임이 시시해지기 때문이다.
        ///
        /// 중간에 0으로 떨어뜨리지 않고 끝까지 끌고 가는 이유: 상위 몇 단계가 모두
        /// 공정한 주사위가 되면 그 구간은 배치 실력만 남는데, 운이 크게 작용하는
        /// 게임이라 한 판의 분산이 그 차이를 덮어 서로 구분되지 않는다.
        /// </summary>
        private static readonly double[] LowBiasByLevel =
        {
            0.7, 0.62, 0.54, 0.47, 0.39, 0.31, 0.23, 0.16, 0.08, 0.0
        };

        public static double DefaultLowBias(int level)
        {
            int index = DifficultyTable.Clamp(level) - DifficultyTable.MinLevel;
            return LowBiasByLevel[index];
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
