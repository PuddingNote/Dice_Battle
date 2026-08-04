namespace DiceBattle.Core
{
    /// <summary>
    /// 한 판 결과를 반영한 뒤의 진행 상태와, 그 판에서 무엇이 바뀌었는지.
    ///
    /// 결과 화면은 "몇 점 움직였는지"와 "새 난이도가 열렸는지"를 둘 다 알아야 한다.
    /// 이 둘을 각자 다시 계산하면 판정 기준이 어긋날 수 있으므로 한 번에 묶어 돌려준다.
    /// </summary>
    public readonly struct ProgressUpdate
    {
        /// <summary>갱신된 현재 점수.</summary>
        public int Score { get; }

        /// <summary>갱신된 최고 달성 점수. 해금 판정의 기준이며 내려가지 않는다.</summary>
        public int HighestScore { get; }

        /// <summary>
        /// 이번 판의 실제 점수 변화.
        /// 하한에 걸리면 난이도의 차감량보다 작을 수 있으므로, 결과 화면은 이 값을 써야 한다.
        /// </summary>
        public int Delta { get; }

        public int UnlockedBefore { get; }
        public int UnlockedAfter { get; }

        public ProgressUpdate(int score, int highestScore, int delta,
            int unlockedBefore, int unlockedAfter)
        {
            Score = score;
            HighestScore = highestScore;
            Delta = delta;
            UnlockedBefore = unlockedBefore;
            UnlockedAfter = unlockedAfter;
        }

        /// <summary>이번 판으로 새 난이도가 열렸는가. 화면 흐름의 분기 조건이다.</summary>
        public bool HasNewUnlock => UnlockedAfter > UnlockedBefore;
    }
}
