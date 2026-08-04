using System;
using DiceBattle.Core;

namespace DiceBattle.AI
{
    /// <summary>
    /// 난이도(1~10) AI. 플레이어가 해금한 범위에서 직접 고른 레벨로 동작한다.
    /// 이 게임은 운 비중이 커서, 난이도 폭을 넓히기 위해 하위 레벨은 "일부러 나쁘게",
    /// 상위 레벨은 "최선"으로 두는 스펙트럼으로 구성한다:
    ///   Lv1   : 최악 수 40% / 최선 6% / 나머지 무작위 (가장 쉬움, 그래도 상대는 됨)
    ///   Lv2~3 : 최악 수가 빠르게 사라짐
    ///   Lv4~9 : 최선 비중이 꾸준히 늘어남
    ///   Lv10  : 항상 최선                            (가장 어려움)
    ///
    /// <b>가장 쉬운 쪽 끝을 일부러 비워 뒀다.</b> 거의 매 수를 최악으로 두는 AI는
    /// 상대라기보다 구경거리라 이기는 재미가 없다. 그래서 Lv1도 최선 수를 섞는다.
    ///
    /// 위쪽 끝은 "휴리스틱 최선 + 공정한 주사위"다. 그보다 강하게 만들려면 AI에게
    /// 유리한 주사위를 주거나 수읽기를 넣어야 하는데, 둘 다 이 게임의 설계 방향이
    /// 아니라 Lv10을 천장으로 둔다.
    /// </summary>
    public sealed class LeveledAiStrategy : IAiStrategy
    {
        // 난이도 범위는 난이도 표와 하나여야 한다. 따로 두면 한쪽만 바뀌어 조용히 어긋난다.
        public const int MinLevel = DifficultyTable.MinLevel;
        public const int MaxLevel = DifficultyTable.MaxLevel;

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
        //
        // 스펙트럼의 <b>아래쪽 30%는 잘라냈다.</b> 가장 약한 AI가 거의 매 수를 일부러
        // 최악으로 두면 이기는 게 아니라 그냥 지켜보는 게 되어 재미가 없다.
        // Lv1도 최선 수를 섞어 두고, 최악 수는 40%로 낮췄다.
        private static readonly double[] PlayBestByLevel =
        { 0.06, 0.15, 0.25, 0.35, 0.48, 0.60, 0.72, 0.81, 0.91, 1.0 };

        private static readonly double[] PlayWorstByLevel =
        { 0.40, 0.24, 0.09, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };

        private static readonly double[] SmartExtraByLevel =
        { 0.24, 0.30, 0.36, 0.44, 0.53, 0.63, 0.72, 0.81, 0.91, 1.0 };

        public static double DefaultPlayBest(int lvl) => PlayBestByLevel[IndexOf(lvl)];
        public static double DefaultPlayWorst(int lvl) => PlayWorstByLevel[IndexOf(lvl)];
        public static double DefaultSmartExtra(int lvl) => SmartExtraByLevel[IndexOf(lvl)];

        private static int IndexOf(int level) => DifficultyTable.Clamp(level) - MinLevel;

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
