using System;
using DiceBattle.Core;

namespace DiceBattle.AI
{
    /// <summary>
    /// 등급 연동 난이도(1~5) AI. 향후 플레이어 점수/등급에 따라 레벨이 선택된다.
    /// 이 게임은 운 비중이 커서, 난이도 폭을 넓히기 위해 하위 레벨은 "일부러 나쁘게",
    /// 상위 레벨은 "최선"으로 두는 스펙트럼으로 구성한다:
    ///   Lv1: 항상 최악 수      (가장 쉬움)
    ///   Lv2: 절반 최악 / 절반 무작위
    ///   Lv3: 무작위            (중립)
    ///   Lv4: 대체로 최선       (어려움)
    ///   Lv5: 항상 최선         (가장 어려움)
    /// </summary>
    public sealed class LeveledAiStrategy : IAiStrategy
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 5;

        public int Level { get; }

        private readonly Random _rng;
        private readonly HeuristicAiStrategy _heuristic = new HeuristicAiStrategy();
        private readonly RandomAiStrategy _random;

        // 기본 배치: pBest 확률로 최선, pWorst 확률로 최악, 나머지는 무작위.
        private readonly double _pBest;
        private readonly double _pWorst;
        // 추가(특수) 배치: pSmartExtra 확률로 휴리스틱 최선, 나머지는 무작위.
        private readonly double _pSmartExtra;

        public LeveledAiStrategy(int level) : this(level, new Random()) { }

        public LeveledAiStrategy(int level, Random rng)
        {
            Level = level < MinLevel ? MinLevel : (level > MaxLevel ? MaxLevel : level);
            _rng = rng ?? new Random();
            _random = new RandomAiStrategy(_rng);
            _pBest = DefaultPlayBest(Level);
            _pWorst = DefaultPlayWorst(Level);
            _pSmartExtra = DefaultSmartExtra(Level);
        }

        /// <summary>명시적 확률 지정(난이도 설정 에셋에서 값 주입용).</summary>
        public LeveledAiStrategy(double playBest, double playWorst, double smartExtra)
            : this(playBest, playWorst, smartExtra, new Random()) { }

        public LeveledAiStrategy(double playBest, double playWorst, double smartExtra, Random rng)
        {
            Level = 0;
            _rng = rng ?? new Random();
            _random = new RandomAiStrategy(_rng);
            _pBest = Clamp01(playBest);
            _pWorst = Clamp01(playWorst);
            _pSmartExtra = Clamp01(smartExtra);
        }

        // 레벨별 기본값(Inspector 미설정 시 사용).
        public static double DefaultPlayBest(int lvl)
        {
            switch (lvl) { case 1: return 0.0; case 2: return 0.0; case 3: return 0.3; case 4: return 0.7; default: return 1.0; }
        }
        public static double DefaultPlayWorst(int lvl)
        {
            switch (lvl) { case 1: return 1.0; case 2: return 0.5; default: return 0.0; }
        }
        public static double DefaultSmartExtra(int lvl)
        {
            switch (lvl) { case 1: return 0.0; case 2: return 0.2; case 3: return 0.4; case 4: return 0.7; default: return 1.0; }
        }

        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

        public int ChoosePrimaryLine(GameState state, PlayerId me)
        {
            double roll = _rng.NextDouble();
            if (roll < _pBest)
                return _heuristic.ChoosePrimaryLine(state, me);
            if (roll < _pBest + _pWorst)
                return _heuristic.WorstPrimaryLine(state, me);
            return _random.ChoosePrimaryLine(state, me);
        }

        public ExtraMove ChooseExtraMove(GameState state, PlayerId me)
        {
            if (_rng.NextDouble() < _pSmartExtra)
                return _heuristic.ChooseExtraMove(state, me);
            return _random.ChooseExtraMove(state, me);
        }
    }
}
