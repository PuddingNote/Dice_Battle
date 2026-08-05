using System;

namespace DiceBattle.Core
{
    /// <summary>
    /// 누적 전적. JSON 한 덩어리로 저장되므로 <b>필드는 전부 public</b>이어야 한다
    /// (JsonUtility는 프로퍼티를 저장하지 않는다).
    ///
    /// 난이도별 전적은 <b>화면에 보이지 않지만 기록은 한다.</b> 나중에 연승 보너스나
    /// 배팅 수치를 정할 때 감이 아니라 실제 승률 분포로 정하기 위한 근거 데이터다
    /// (docs/Difficulty.md 6장이 요구하는 데이터가 바로 이것이다).
    /// </summary>
    [Serializable]
    public sealed class StatsData
    {
        /// <summary>
        /// 저장 스키마 버전. 항목을 추가할 때 올린다.
        /// 낡은 저장본을 읽어도 없는 필드는 0으로 남을 뿐 깨지지 않는다.
        /// </summary>
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        public int wins;
        public int losses;
        public int draws;

        /// <summary>지금 이어지고 있는 연승. 패배와 <b>무승부</b> 모두에서 끊긴다.</summary>
        public int currentStreak;
        public int bestStreak;

        /// <summary>내가 제거한 상대 주사위의 누적 <b>개수</b>(제거가 일어난 횟수가 아니다).</summary>
        public int removedDice;

        // 난이도별. 길이는 항상 DifficultyTable.LevelCount다. Repair()가 보장한다.
        public int[] winsByLevel = new int[DifficultyTable.LevelCount];
        public int[] lossesByLevel = new int[DifficultyTable.LevelCount];
        public int[] drawsByLevel = new int[DifficultyTable.LevelCount];

        public int TotalMatches => wins + losses + draws;

        /// <summary>무승부도 분모에 넣는다. 화면에는 전적을 "N승 N패 N무"로 따로 보여준다.</summary>
        public double WinRate => TotalMatches == 0 ? 0d : (double)wins / TotalMatches;

        /// <summary>판당 평균 제거 개수.</summary>
        public double AverageRemoved
            => TotalMatches == 0 ? 0d : (double)removedDice / TotalMatches;

        /// <summary>한 판의 결과를 누적한다.</summary>
        /// <param name="level">그 판을 <b>시작한</b> 난이도. 지금 해금된 난이도가 아니다.</param>
        /// <param name="removed">그 판에서 내가 제거한 주사위 개수.</param>
        public void Apply(PlayerMatchResult result, int level, int removed)
        {
            Repair();

            int index = DifficultyTable.Clamp(level) - DifficultyTable.MinLevel;
            if (removed > 0) removedDice += removed;

            switch (result)
            {
                case PlayerMatchResult.Win:
                    wins++;
                    winsByLevel[index]++;
                    currentStreak++;
                    if (currentStreak > bestStreak) bestStreak = currentStreak;
                    break;

                case PlayerMatchResult.Lose:
                    losses++;
                    lossesByLevel[index]++;
                    currentStreak = 0;
                    break;

                default:
                    draws++;
                    drawsByLevel[index]++;
                    // 무승부는 연승을 끊는다. "연승"을 글자 그대로 연속 승리로 본 결정이다.
                    currentStreak = 0;
                    break;
            }
        }

        /// <summary>난이도별 승/패/무를 한 번에 읽는다. 화면에는 쓰지 않고 디버그·분석용이다.</summary>
        public void LevelRecord(int level, out int w, out int l, out int d)
        {
            Repair();
            int index = DifficultyTable.Clamp(level) - DifficultyTable.MinLevel;
            w = winsByLevel[index];
            l = lossesByLevel[index];
            d = drawsByLevel[index];
        }

        /// <summary>
        /// 저장본이 낡았거나 손상됐을 때를 보정한다.
        /// JsonUtility는 JSON에 배열이 없으면 null을 그대로 두고, 길이가 달라도 맞춰주지 않는다.
        /// 읽은 직후와 쓰기 전에 반드시 부른다.
        /// </summary>
        public void Repair()
        {
            winsByLevel = Resize(winsByLevel);
            lossesByLevel = Resize(lossesByLevel);
            drawsByLevel = Resize(drawsByLevel);

            if (wins < 0) wins = 0;
            if (losses < 0) losses = 0;
            if (draws < 0) draws = 0;
            if (removedDice < 0) removedDice = 0;
            if (currentStreak < 0) currentStreak = 0;
            if (bestStreak < currentStreak) bestStreak = currentStreak;

            version = CurrentVersion;
        }

        private static int[] Resize(int[] source)
        {
            if (source != null && source.Length == DifficultyTable.LevelCount) return source;

            var fixedArray = new int[DifficultyTable.LevelCount];
            if (source != null)
            {
                int copy = source.Length < fixedArray.Length ? source.Length : fixedArray.Length;
                for (int i = 0; i < copy; i++)
                    fixedArray[i] = source[i] < 0 ? 0 : source[i];
            }
            return fixedArray;
        }
    }
}
