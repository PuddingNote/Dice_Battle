using UnityEngine;
using DiceBattle.AI;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 난이도 밸런스 전체. Inspector에서 바로 조정한다.
    ///
    /// 두 종류를 담는다:
    ///   <see cref="levels"/>     Lv1~10의 AI 행동 수치. "체감"은 공식으로 안 나오므로 직접 편집한다.
    ///   <see cref="scoreCurve"/> 해금선·승점·패점을 만드는 상수. 표 30칸을 손으로 적으면
    ///                            한 줄만 어긋나도 알아채기 어려워, 상수 몇 개에서 뽑아낸다.
    ///
    /// Project 창 우클릭 → Create → DiceBattle → Difficulty Config 로 에셋을 만들고
    /// GameBootstrap 의 Difficulty 슬롯에 연결한다. 비우면 코드 기본값을 사용한다.
    ///
    /// 수치를 바꾼 뒤에는 <b>에셋 우클릭(또는 Inspector 우측 상단 ⋮) → "난이도 표 출력"</b>으로
    /// 실제로 어떤 표가 나오는지 Console에서 확인할 수 있다.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyConfig", menuName = "DiceBattle/Difficulty Config")]
    public sealed class DifficultyConfig : ScriptableObject
    {
        [System.Serializable]
        public struct LevelSettings
        {
            [Tooltip("기본 배치를 '최선 수'로 둘 확률")]
            [Range(0f, 1f)] public float playBest;
            [Tooltip("기본 배치를 '일부러 최악 수'로 둘 확률(쉬움용)")]
            [Range(0f, 1f)] public float playWorst;
            [Tooltip("추가(특수) 주사위를 최선 수로 배치할 확률")]
            [Range(0f, 1f)] public float smartExtra;
        }

        [System.Serializable]
        public struct ScoreCurveSettings
        {
            [Tooltip("Lv1의 승리 점수. 곡선 전체의 기준점")]
            public float baseWinPoints;

            [Tooltip("단계마다 승리 점수가 곱해지는 배율. 너무 낮으면 반올림에 먹혀 인접 레벨의 보상이 같아진다")]
            public float growth;

            [Tooltip("패배 차감 = 승리 점수 × 이 값. 전 단계에 같은 비율을 써야 특정 난이도만 파밍 구간이 되지 않는다")]
            [Range(0f, 1f)] public float loseRatio;

            [Tooltip("다음 해금까지의 간격을 '이 난이도에서 몇 판 이긴 만큼'으로 정한다. 전체 플레이타임을 조절하는 손잡이")]
            public float winsPerTier;

            [Tooltip("승/패 점수의 반올림 단위")]
            public int pointRoundTo;

            [Tooltip("해금선의 반올림 단위")]
            public int unlockRoundTo;

            [Tooltip("연승 보너스 = 승점 × 이 값(직전 판도 이겼을 때만). 0이면 보너스 없음")]
            [Range(0f, 2f)] public float streakBonusRatio;
        }

        [Tooltip("Lv1~Lv10 순서. 칸이 모자라면 그 레벨은 코드 기본값을 사용한다.")]
        public LevelSettings[] levels = new LevelSettings[]
        {
            new LevelSettings { playBest = 0.04f, playWorst = 0.46f, smartExtra = 0.04f }, // Lv1 가장 쉬움
            new LevelSettings { playBest = 0.06f, playWorst = 0.34f, smartExtra = 0.08f },
            new LevelSettings { playBest = 0.10f, playWorst = 0.22f, smartExtra = 0.14f },
            new LevelSettings { playBest = 0.15f, playWorst = 0.10f, smartExtra = 0.20f },
            new LevelSettings { playBest = 0.22f, playWorst = 0.00f, smartExtra = 0.28f },
            new LevelSettings { playBest = 0.35f, playWorst = 0.00f, smartExtra = 0.40f },
            new LevelSettings { playBest = 0.49f, playWorst = 0.00f, smartExtra = 0.53f },
            new LevelSettings { playBest = 0.64f, playWorst = 0.00f, smartExtra = 0.68f },
            new LevelSettings { playBest = 0.82f, playWorst = 0.00f, smartExtra = 0.84f },
            new LevelSettings { playBest = 1.00f, playWorst = 0.00f, smartExtra = 1.00f }, // Lv10 가장 어려움
        };

        [Tooltip("해금선과 승/패 점수를 만드는 상수. 실제 표는 '난이도 표 출력'으로 확인한다.")]
        public ScoreCurveSettings scoreCurve = new ScoreCurveSettings
        {
            baseWinPoints = 20f,
            growth = 1.35f,
            loseRatio = 0.45f,
            winsPerTier = 21f, // Lv.10까지 약 451판 ≈ 15시간(연승 보너스 50% 반영, 판당 2분 기준)
            pointRoundTo = 10,
            unlockRoundTo = 100,
            streakBonusRatio = 0.5f,
        };

        public bool TryGet(int level, out LevelSettings settings)
        {
            int idx = level - 1;
            if (levels != null && idx >= 0 && idx < levels.Length)
            {
                settings = levels[idx];
                return true;
            }
            settings = default;
            return false;
        }

        /// <summary>설정에 따라 AI 전략을 생성.</summary>
        public IAiStrategy CreateAi(int level)
        {
            if (TryGet(level, out var s))
                return new LeveledAiStrategy(s.playBest, s.playWorst, s.smartExtra);
            return new LeveledAiStrategy(level);
        }

        /// <summary>
        /// 점수 곡선에서 난이도 표를 만든다.
        /// 상수가 잘못돼 표가 성립하지 않으면 기본 곡선으로 되돌린다 —
        /// 여기서 예외가 밖으로 나가면 게임이 아예 시작되지 않는다.
        /// </summary>
        public DifficultyTable CreateTable()
        {
            try
            {
                DifficultyTable table = ToCurve().Build();
                WarnIfRewardsRepeat(table);
                return table;
            }
            catch (System.ArgumentException e)
            {
                Debug.LogError($"[DifficultyConfig] 점수 곡선이 올바르지 않아 기본값을 사용한다: {e.Message}");
                return DifficultyCurve.Default.Build();
            }
        }

        /// <summary>
        /// 인스펙터 값(float)을 곡선으로 옮긴다.
        /// FromSingle을 거쳐야 인스펙터에 적은 값과 코드 기본 곡선이 같은 표를 만든다
        /// (그냥 double로 넓히면 float 오차가 반올림 경계에서 결과를 갈라놓는다).
        /// </summary>
        private DifficultyCurve ToCurve()
            => DifficultyCurve.FromSingle(scoreCurve.baseWinPoints, scoreCurve.growth,
                scoreCurve.loseRatio, scoreCurve.winsPerTier,
                scoreCurve.pointRoundTo, scoreCurve.unlockRoundTo, scoreCurve.streakBonusRatio);

        /// <summary>
        /// 인접 단계의 승리 점수가 같아지면 경고한다.
        /// 더 어려운 난이도를 같은 보상에 하는 셈이라 그 단계를 고를 이유가 사라지는데,
        /// 표를 눈으로 봐도 잘 드러나지 않는다. growth를 올리거나 pointRoundTo를 낮추면 된다.
        /// </summary>
        private static void WarnIfRewardsRepeat(DifficultyTable table)
        {
            for (int level = DifficultyTable.MinLevel + 1; level <= DifficultyTable.MaxLevel; level++)
            {
                if (table[level].WinPoints != table[level - 1].WinPoints) continue;

                Debug.LogWarning(
                    $"[DifficultyConfig] Lv{level - 1}과 Lv{level}의 승리 점수가 {table[level].WinPoints}로 같다. " +
                    "growth를 올리거나 pointRoundTo를 낮출 것.");
            }
        }

        [ContextMenu("난이도 표 출력")]
        private void LogTable()
        {
            DifficultyTable table = CreateTable();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[DifficultyConfig] 난이도 표");
            sb.AppendLine(" Lv |   해금선 |    승 | 연승보너스 |    패 | 분기승률");

            foreach (DifficultyTier tier in table.Tiers)
            {
                sb.AppendLine($" {tier.Level,2} | {tier.UnlockScore,8} | {tier.WinPoints,5} | " +
                              $"{tier.StreakBonusPoints,10} | {tier.LosePoints,5} | {tier.BreakEvenWinRate,7:P1}");
            }

            Debug.Log(sb.ToString());
        }
    }
}
