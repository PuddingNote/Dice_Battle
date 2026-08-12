using System;
using DiceBattle.Core;

namespace DiceBattle.AI
{
    /// <summary>
    /// 난이도(1~10) AI. 플레이어가 해금한 범위에서 직접 고른 레벨로 동작한다.
    /// <see cref="HeuristicAiStrategy"/>의 최선 수와 최악 수, 그리고 무작위 수를
    /// 레벨별 비율로 섞어 스펙트럼을 만든다:
    ///   Lv1   : 최악 46% / 최선 4% / 나머지 무작위 (가장 쉬움, 그래도 상대는 됨)
    ///   Lv2~4 : 최악 수가 빠르게 사라짐
    ///   Lv5~9 : 최선 비중이 꾸준히 늘어남
    ///   Lv10  : 항상 최선                          (가장 어려움)
    ///
    /// <b>주사위는 전 레벨에서 공정하다.</b> 예전에는 하위 레벨에서 AI에게 낮은 눈을
    /// 몰아주는 방식으로 난이도를 낮췄는데, AI가 작은 눈만 뽑는 게 눈에 띄어
    /// 초반 재미를 깎았다. 지금은 배치 실력만으로 난이도를 만든다 —
    /// 자세한 근거와 측정값은 docs/Difficulty.md 2장에 있다.
    ///
    /// <b>가장 쉬운 쪽 끝을 일부러 비워 뒀다.</b> 거의 매 수를 최악으로 두는 AI는
    /// 상대라기보다 구경거리라 이기는 재미가 없다. 그래서 Lv1도 최선 수를 섞는다.
    ///
    /// 위쪽 끝은 "항상 최선 + 공정한 주사위"다. 그보다 강하게 만들려면 AI에게
    /// 유리한 주사위를 주거나 더 깊은 수읽기를 넣어야 하는데, 앞은 이 게임의 설계
    /// 방향이 아니고 뒤는 계산량을 50배 늘려 승률 2%p를 얻는 거래라 Lv10을 천장으로 둔다.
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
        // 추가(특수) 배치: pSmartExtra 확률로 최선, pWorst 확률로 최악, 나머지는 무작위.
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
        // 스펙트럼의 <b>아래쪽 끝은 잘라냈다.</b> 가장 약한 AI가 거의 매 수를 일부러
        // 최악으로 두면 이기는 게 아니라 그냥 지켜보는 게 되어 재미가 없다.
        // Lv1도 최선 수를 섞어 두고, 최악 수는 과반을 넘기지 않는다.
        //
        // 이 표가 만드는 실제 승률은 docs/Difficulty.md 2-1장에 측정값으로 적어 두었다.
        // 수치를 바꾸면 그 표도 같이 고칠 것 — 안 그러면 무엇이 어긋났는지 알 방법이 없다.
        private static readonly double[] PlayBestByLevel =
        { 0.04, 0.06, 0.10, 0.15, 0.22, 0.35, 0.49, 0.64, 0.82, 1.00 };

        private static readonly double[] PlayWorstByLevel =
        { 0.46, 0.34, 0.22, 0.10, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00 };

        // 2026-08-11: Lv5~9 구간을 큰 폭으로 올렸다(0.28~0.84 → 0.40~0.96). 추가(특수)
        // 배치의 "완전 무작위" 갈래에 걸리면 확률·소유자 무관하게 아무 칸이나 골라, 낮은
        // 눈을 자기 필드에 두고 높은 눈을 상대에게 내주는 것처럼 보이는 수가 나온다.
        // 기본 배치 실수(playWorst)와 달리 이건 "특수 주사위 그 자체를 상대에게 헌납"하는
        // 모양이라 유독 눈에 띄고, 심지어 Lv9(구 16%)에서도 자주 체감됐다. docs/Difficulty.md
        // 2-1장에 재측정한 승률과 근거가 있다.
        private static readonly double[] SmartExtraByLevel =
        { 0.04, 0.08, 0.16, 0.26, 0.40, 0.58, 0.74, 0.88, 0.96, 1.00 };

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
            // 추가 배치도 기본 배치와 같은 세 갈래로 나눈다. 하위 레벨에서 기본 배치만
            // 나쁘게 두면 제거로 얻은 특수 주사위는 제대로 쓰는 셈이라 체감이 어긋난다.
            double roll = _rng.NextDouble();
            if (roll < _pSmartExtra)
                return _heuristic.ChooseExtraMove(state, me);
            if (roll < _pSmartExtra + _pWorst)
                return _heuristic.WorstExtraMove(state, me);
            return _random.ChooseExtraMove(state, me);
        }
    }
}
