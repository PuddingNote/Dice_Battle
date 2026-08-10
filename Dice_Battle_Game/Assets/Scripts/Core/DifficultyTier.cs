namespace DiceBattle.Core
{
    /// <summary>
    /// 난이도 한 단계의 <b>점수 규칙</b>(불변).
    ///
    /// AI 행동 수치(playBest / playWorst 등)는 여기 들어가지 않는다.
    /// 그쪽은 Unity 인스펙터에서 조정하는 DifficultyConfig 에셋이 들고 있고,
    /// 이 구조체는 UnityEngine에 의존하지 않는 순수 점수 규칙만 담당한다.
    /// 하나로 합치면 Core가 엔진에 묶여 테스트에서 못 쓴다.
    /// </summary>
    public readonly struct DifficultyTier
    {
        public int Level { get; }

        /// <summary>이 난이도를 해금하는 데 필요한 <b>최고 달성 점수</b>.</summary>
        public int UnlockScore { get; }

        /// <summary>승리 시 획득 점수(연승 보너스 제외).</summary>
        public int WinPoints { get; }

        /// <summary>패배 시 차감량. <b>양수로 보관</b>하고 적용할 때 빼낸다.</summary>
        public int LosePoints { get; }

        /// <summary>
        /// 직전 판도 이겼을 때(연승 2연째부터) 추가로 얹는 점수.
        /// <see cref="WinPoints"/>에 비례해 미리 반올림해 둔 값이다 — docs/Difficulty.md 6장.
        /// </summary>
        public int StreakBonusPoints { get; }

        public DifficultyTier(int level, int unlockScore, int winPoints, int losePoints,
            int streakBonusPoints = 0)
        {
            Level = level;
            UnlockScore = unlockScore < 0 ? 0 : unlockScore;
            WinPoints = winPoints < 0 ? 0 : winPoints;
            LosePoints = losePoints < 0 ? 0 : losePoints;
            StreakBonusPoints = streakBonusPoints < 0 ? 0 : streakBonusPoints;
        }

        /// <summary>
        /// 점수가 늘지도 줄지도 않는 승률. 이보다 잘 이기면 앞으로 가고, 못 이기면 뒤로 밀린다.
        ///
        /// 이 값이 단계마다 크게 달라지면 특정 난이도만 파밍 구간이 되므로,
        /// 밸런스를 볼 때 전 단계가 비슷한 값인지 확인하는 용도다.
        /// 게임 로직에서는 쓰지 않는다. <b>연승 보너스는 넣지 않는다</b> — 보너스는
        /// 연승 중에만 붙어 손익분기 자체를 흔들지 않고, 넣으면 오히려 오해를 부른다.
        /// </summary>
        public double BreakEvenWinRate
            => WinPoints + LosePoints == 0 ? 0d : (double)LosePoints / (WinPoints + LosePoints);

        public override string ToString()
            => $"Lv{Level} 해금 {UnlockScore} / 승 +{WinPoints} / 연승 +{StreakBonusPoints} / 패 -{LosePoints}";
    }
}
