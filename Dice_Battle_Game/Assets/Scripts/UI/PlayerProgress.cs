using UnityEngine;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>플레이어 점수를 PlayerPrefs에 저장/로드하고 등급을 제공한다.</summary>
    public static class PlayerProgress
    {
        private const string ScoreKey = "dicebattle.score";

        public static int Score
        {
            get => Mathf.Max(RankSystem.MinScore, PlayerPrefs.GetInt(ScoreKey, RankSystem.StartScore));
            private set
            {
                PlayerPrefs.SetInt(ScoreKey, Mathf.Max(RankSystem.MinScore, value));
                PlayerPrefs.Save();
            }
        }

        public static int Level => RankSystem.LevelForScore(Score);

        /// <summary>한 판 결과를 반영해 점수를 갱신하고, 변화량(delta)을 반환한다.</summary>
        public static int ApplyResult(PlayerMatchResult result)
        {
            int before = Score;
            int after = RankSystem.ApplyResult(before, result);
            Score = after;
            return after - before;
        }

        /// <summary>디버그/테스트용 초기화.</summary>
        public static void Reset() => Score = RankSystem.StartScore;
    }
}
