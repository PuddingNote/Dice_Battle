using System;

namespace DiceBattle.Core
{
    /// <summary>
    /// 튜닝 상수 몇 개에서 난이도 표 10줄을 통째로 만든다.
    ///
    /// 10줄을 손으로 적으면 한 줄만 고쳐도 곡선이 어긋나고, 그게 어긋났다는 걸
    /// 눈으로 알아채기 어렵다. 공식에서 뽑으면 상수 하나만 바꿔 전체를 다시 그린다.
    ///
    /// <b>여기 기본값은 전부 임시 수치다.</b> 실제 밸런스는 마지막 단계에서
    /// 확정한다. 지금 이 값들은 "구조가 말이 되는 표를 뽑아내는가"를 보기 위한 것이다.
    /// </summary>
    public readonly struct DifficultyCurve
    {
        /// <summary>Lv1의 승리 점수. 곡선 전체의 기준점.</summary>
        public double BaseWinPoints { get; }

        /// <summary>단계마다 승리 점수가 곱해지는 배율. 1보다 커야 뒤로 갈수록 커진다.</summary>
        public double Growth { get; }

        /// <summary>
        /// 패배 차감 = 승리 점수 × 이 값.
        /// 전 단계에 같은 비율을 쓰면 손익분기 승률이 단계와 무관하게 일정해진다.
        /// 그래야 "이길 수 있는 가장 높은 난이도"를 고르는 게 항상 이득이 되고,
        /// 하위 난이도 파밍이 저절로 손해가 된다.
        /// </summary>
        public double LoseRatio { get; }

        /// <summary>
        /// 다음 단계 해금선까지 벌어지는 간격을 "이 난이도에서 몇 판을 이긴 만큼"으로 정한다.
        /// <b>전체 플레이타임을 조절하는 사실상 유일한 손잡이다</b> — 이 값을 키우면
        /// 모든 구간이 비례해서 길어진다.
        /// </summary>
        public double WinsPerTier { get; }

        /// <summary>
        /// 승/패 점수의 반올림 단위. 매 판 결과 화면에 뜨는 숫자라 잘게 떨어지면 지저분하다.
        /// <b>단위를 키울수록 <see cref="Growth"/>도 같이 키워야 한다</b> —
        /// 단계 간 차이가 단위보다 작으면 반올림에 먹혀 인접 레벨의 보상이 같아진다.
        /// </summary>
        public int PointRoundTo { get; }

        /// <summary>
        /// 해금선의 반올림 단위. 목표로 보이는 숫자라 점수보다 굵게 떨어뜨린다
        /// (200 / 500 / 900처럼).
        /// </summary>
        public int UnlockRoundTo { get; }

        public DifficultyCurve(double baseWinPoints, double growth, double loseRatio,
            double winsPerTier, int pointRoundTo, int unlockRoundTo)
        {
            BaseWinPoints = baseWinPoints;
            Growth = growth;
            LoseRatio = loseRatio;
            WinsPerTier = winsPerTier;
            PointRoundTo = pointRoundTo;
            UnlockRoundTo = unlockRoundTo;
        }

        /// <summary>
        /// <b>임시 수치.</b> 인스펙터 에셋이 비어 있거나 깨졌을 때 쓰이는 폴백이자,
        /// 밸런스를 확정할 때의 출발점이다. 확정 전까지 이 값을 근거로 삼지 말 것.
        /// </summary>
        public static DifficultyCurve Placeholder
            => new DifficultyCurve(baseWinPoints: 20d, growth: 1.35d, loseRatio: 0.45d,
                winsPerTier: 10d, pointRoundTo: 10, unlockRoundTo: 100);

        public DifficultyTable Build()
        {
            var tiers = new DifficultyTier[DifficultyTable.LevelCount];

            int unlock = 0;
            double win = BaseWinPoints;

            for (int i = 0; i < tiers.Length; i++)
            {
                int level = DifficultyTable.MinLevel + i;
                int winPoints = Round(win, PointRoundTo);

                // 차감은 반올림된 승점에서 뽑는다. 원본 값에서 뽑으면 화면에 보이는
                // 두 숫자의 비율이 의도한 LoseRatio와 어긋나 보인다.
                int losePoints = Round(winPoints * LoseRatio, PointRoundTo);

                tiers[i] = new DifficultyTier(level, unlock, winPoints, losePoints);

                // 다음 단계 해금선은 "이 단계에서 WinsPerTier판을 이긴 만큼" 더 간다.
                // 승리 점수가 커지는 만큼 간격도 같이 커져서 누진 증가가 된다.
                unlock += Round(winPoints * WinsPerTier, UnlockRoundTo);
                win *= Growth;
            }

            return new DifficultyTable(tiers);
        }

        /// <summary>지정 단위로 반올림하되 0으로 내려가지는 않게 한다.</summary>
        private static int Round(double value, int roundTo)
        {
            int step = roundTo < 1 ? 1 : roundTo;
            int rounded = (int)Math.Round(value / step, MidpointRounding.AwayFromZero) * step;
            return rounded < step ? step : rounded;
        }
    }
}
