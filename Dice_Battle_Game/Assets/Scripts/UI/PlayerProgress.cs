using UnityEngine;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 플레이어 진행도를 PlayerPrefs에 저장/로드한다.
    ///
    /// 두 점수를 구분해서 들고 있다:
    ///   <see cref="Score"/>        승패로 오르내리는 현재 점수. 화면에 보이는 값.
    ///   <see cref="HighestScore"/> 지금까지 도달한 최고 점수. 내려가지 않는다.
    ///
    /// <b>난이도 해금은 오직 HighestScore로 판정한다.</b> 현재 점수로 판정하면
    /// 패배로 점수가 내려갈 때 이미 해금한 난이도가 다시 잠긴다.
    /// </summary>
    public static class PlayerProgress
    {
        private const string ScoreKey = "dicebattle.score";
        private const string HighestKey = "dicebattle.highest";

        private static DifficultyTable _table;

        /// <summary>
        /// 난이도 표. <see cref="Configure"/>로 인스펙터 에셋의 표가 주입되고,
        /// 주입 전이거나 에셋이 없으면 코드 기본 곡선으로 돌아간다.
        /// </summary>
        private static DifficultyTable Table => _table ??= DifficultyCurve.Default.Build();

        /// <summary>
        /// 난이도 표를 지정한다. 부트스트랩에서 한 번 호출한다.
        /// null이면 아무것도 하지 않고 기본 곡선을 그대로 쓴다.
        /// </summary>
        public static void Configure(DifficultyTable table)
        {
            if (table != null) _table = table;
        }

        public static int Score
        {
            get => Mathf.Max(RankSystem.MinScore, PlayerPrefs.GetInt(ScoreKey, RankSystem.StartScore));
            private set
            {
                PlayerPrefs.SetInt(ScoreKey, Mathf.Max(RankSystem.MinScore, value));
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// 최고 달성 점수.
        ///
        /// 키가 없으면 <b>현재 점수를 기본값으로 쓴다</b> — 이 한 줄이 구버전 데이터 이관이다.
        /// 최고 점수 개념이 없던 시절의 저장 데이터는 현재 점수가 곧 도달 기록이므로,
        /// 별도 변환 절차 없이 처음 읽는 순간 자연스럽게 넘어온다.
        /// </summary>
        public static int HighestScore
        {
            get
            {
                int stored = PlayerPrefs.GetInt(HighestKey, Score);
                return stored < Score ? Score : stored; // 어떤 경우에도 현재 점수보다 낮을 수 없다
            }
            private set
            {
                PlayerPrefs.SetInt(HighestKey, Mathf.Max(RankSystem.MinScore, value));
                PlayerPrefs.Save();
            }
        }

        /// <summary>난이도 표(읽기 전용). 화면이 해금 상태와 점수 폭을 표시할 때 쓴다.</summary>
        public static DifficultyTable Difficulties => Table;

        /// <summary>
        /// 지금까지 해금한 가장 높은 난이도.
        /// 난이도 선택 화면의 기본 선택이기도 하다 — 마지막에 고른 값을 기억하지 않는다.
        /// 기억해 두면 낮은 난이도로 한 번 논 뒤에는 해금이 늘어도 계속 그 값으로 열려,
        /// 정작 새로 얻은 난이도가 눈에 띄지 않는다.
        /// </summary>
        public static int MaxUnlockedLevel => Table.MaxUnlockedLevel(HighestScore);

        /// <summary>다음 해금까지 남은 점수(전부 해금했으면 0).</summary>
        public static int PointsToNextUnlock => Table.PointsToNextUnlock(HighestScore);

        /// <summary>해당 난이도의 점수 규칙(해금선/승점/패점).</summary>
        public static DifficultyTier Tier(int level) => Table[level];

        public static bool IsUnlocked(int level) => Table.IsUnlocked(level, HighestScore);

        /// <summary>한 판 결과를 반영하고 무엇이 바뀌었는지 돌려준다.</summary>
        /// <param name="playedLevel">그 판을 시작할 때 고정된 난이도.</param>
        public static ProgressUpdate ApplyResult(PlayerMatchResult result, int playedLevel)
        {
            ProgressUpdate update = Table.ApplyMatch(Score, HighestScore, playedLevel, result);
            Score = update.Score;
            HighestScore = update.HighestScore;
            return update;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 전용: 점수를 직접 지정한다(난이도 해금 확인용).
        ///
        /// 최고 점수는 현재 점수보다 낮을 수 없으므로 같이 올려 준다.
        /// <paramref name="alsoLowerHighest"/>를 켜면 최고 점수까지 같이 내려서
        /// 해금을 되돌릴 수 있다 — 실제 플레이에서는 절대 일어나지 않는 동작이라
        /// 에디터 밖으로 나가지 않게 막아 둔다.
        /// </summary>
        public static void EditorSetScore(int score, bool alsoLowerHighest)
        {
            int clamped = Mathf.Max(RankSystem.MinScore, score);

            // 새 최고 점수를 Score를 바꾸기 전에 정한다.
            // HighestScore 게터가 현재 점수를 참고하므로 순서를 바꾸면 값이 꼬인다.
            int keep = alsoLowerHighest ? clamped : Mathf.Max(HighestScore, clamped);

            Score = clamped;
            HighestScore = keep; // 반드시 직접 저장한다. 게터의 보정에 기대면 저장값이 낡은 채로 남는다
        }
#endif

        /// <summary>디버그/테스트용 초기화.</summary>
        public static void Reset()
        {
            Score = RankSystem.StartScore;
            HighestScore = RankSystem.StartScore;
        }
    }
}
