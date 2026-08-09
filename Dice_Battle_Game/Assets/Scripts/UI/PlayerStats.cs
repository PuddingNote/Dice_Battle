using UnityEngine;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 누적 전적을 PlayerPrefs에 저장/로드한다. <see cref="PlayerProgress"/>와 짝을 이룬다.
    ///
    /// 점수와 달리 항목이 계속 늘어날 자리라, 키를 하나씩 늘리지 않고
    /// <b>JSON 한 덩어리</b>로 저장한다. 항목을 추가해도 키가 늘지 않고,
    /// 낡은 저장본은 없는 필드가 0으로 남을 뿐 깨지지 않는다.
    /// </summary>
    public static class PlayerStats
    {
        private const string StatsKey = "dicebattle.stats";

        private static StatsData _data;

        /// <summary>읽기 전용으로 쓸 것. 값을 바꾸려면 <see cref="ApplyMatch"/>를 쓴다.</summary>
        public static StatsData Data => _data ??= Load();

        /// <summary>전적이 저장된 적이 있는가(튜토리얼 재노출 방지용).</summary>
        public static bool HasRecord => PlayerPrefs.HasKey(StatsKey);

        /// <summary>한 판 결과를 누적하고 저장한다.</summary>
        /// <param name="playedLevel">그 판을 시작할 때 고정된 난이도.</param>
        /// <param name="removedDice">그 판에서 내가 제거한 주사위 개수.</param>
        public static void ApplyMatch(PlayerMatchResult result, int playedLevel, int removedDice)
        {
            Data.Apply(result, playedLevel, removedDice);
            Save();
        }

        private static StatsData Load()
        {
            string json = PlayerPrefs.GetString(StatsKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return new StatsData();

            StatsData loaded = null;
            try
            {
                loaded = JsonUtility.FromJson<StatsData>(json);
            }
            catch
            {
                // 손상된 저장본 하나 때문에 게임이 멈추면 안 된다. 전적은 잃어도 진행도는 멀쩡하다.
                Debug.LogWarning("[PlayerStats] 전적 데이터를 읽지 못해 새로 시작합니다.");
            }

            if (loaded == null) return new StatsData();

            loaded.Repair(); // 배열 길이·음수 보정. 낡은 스키마도 여기서 흡수된다
            return loaded;
        }

        private static void Save()
        {
            Data.Repair();
            PlayerPrefs.SetString(StatsKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        /// <summary>디버그/테스트용 초기화.</summary>
        public static void Reset()
        {
            _data = new StatsData();
            Save();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 전용: 임의의 전적을 밀어 넣는다.
        /// 화면 레이아웃을 네 자리 판수 같은 실제 크기로 확인하려면 수십 판을 둘 수는 없다.
        /// </summary>
        public static void EditorSeed(int matches, int seed = 0)
        {
            _data = new StatsData();

            var rng = new System.Random(seed);
            for (int i = 0; i < matches; i++)
            {
                int level = rng.Next(DifficultyTable.MinLevel, DifficultyTable.MaxLevel + 1);
                int roll = rng.Next(100);
                // 실제 분포에 가깝게: 승 40 / 패 55 / 무 5.
                PlayerMatchResult result =
                    roll < 40 ? PlayerMatchResult.Win :
                    roll < 95 ? PlayerMatchResult.Lose : PlayerMatchResult.Draw;

                _data.Apply(result, level, rng.Next(0, 5));
            }

            Save();
        }
#endif
    }
}
