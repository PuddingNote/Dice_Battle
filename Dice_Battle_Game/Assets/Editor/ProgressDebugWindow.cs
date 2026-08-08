using UnityEditor;
using UnityEngine;
using DiceBattle.Core;
using DiceBattle.UI;

namespace DiceBattle.EditorTools
{
    /// <summary>
    /// 에디터 전용 진행도 조작 창 (<b>DiceBattle → 진행도(디버그)</b>).
    ///
    /// 난이도 해금을 확인하려면 수천 점을 실제로 벌어야 하는데 그건 검증이 불가능하다.
    /// 여기서 점수를 바로 밀어 넣어 각 난이도의 해금 상태를 즉시 확인한다.
    ///
    /// 이 파일은 Editor 폴더에 있어 빌드에 포함되지 않는다.
    /// </summary>
    public sealed class ProgressDebugWindow : EditorWindow
    {
        private int _scoreInput;
        private int _coinInput;
        private Vector2 _scroll;

        [MenuItem("DiceBattle/진행도(디버그)")]
        private static void Open()
        {
            var window = GetWindow<ProgressDebugWindow>("진행도");
            window.minSize = new Vector2(340f, 820f); // 전적·코인 항목까지 들어간 높이
            window._coinInput = PlayerWallet.Coins;
            window._scoreInput = PlayerProgress.Score;
        }

        /// <summary>
        /// 표시할 난이도 표.
        ///
        /// 플레이 중이 아니면 <see cref="PlayerProgress"/>가 아직 에셋의 표를 받지 못해
        /// 코드 기본 곡선을 들고 있다. 그러면 여기 뜨는 해금선이 실제 게임과 달라져
        /// 확인 도구로서 쓸모가 없으므로, 에셋에서 직접 만들어 쓴다.
        /// </summary>
        private static DifficultyTable Table()
        {
            if (Application.isPlaying) return PlayerProgress.Difficulties;

            string[] guids = AssetDatabase.FindAssets("t:DifficultyConfig");
            if (guids.Length > 0)
            {
                var config = AssetDatabase.LoadAssetAtPath<DifficultyConfig>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
                if (config != null) return config.CreateTable();
            }

            return PlayerProgress.Difficulties;
        }

        private void OnGUI()
        {
            // 항목이 점수·난이도·전적·코인까지 늘어 창을 작게 띄우면 아래가 잘린다.
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawBody();
            EditorGUILayout.EndScrollView();
        }

        private void DrawBody()
        {
            DifficultyTable table = Table();
            int score = PlayerProgress.Score;
            int highest = PlayerProgress.HighestScore;
            int unlocked = table.MaxUnlockedLevel(highest);

            EditorGUILayout.LabelField("현재 상태", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("현재 점수", $"{score:N0}");
            EditorGUILayout.LabelField("최고 점수", $"{highest:N0}");
            EditorGUILayout.LabelField("해금 난이도", $"Lv.{unlocked}");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("점수 직접 지정", EditorStyles.boldLabel);
            _scoreInput = EditorGUILayout.IntField("점수", _scoreInput);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("적용 (해금 유지)"))
                    Apply(_scoreInput, alsoLowerHighest: false);

                // 해금을 되돌려 잠긴 카드를 다시 보려면 최고 점수까지 내려야 한다.
                if (GUILayout.Button("적용 (해금도 되돌림)"))
                    Apply(_scoreInput, alsoLowerHighest: true);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("난이도 해금선으로 이동", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "그 난이도가 막 해금되는 점수로 맞춘다. 최고 점수까지 함께 바뀐다.",
                MessageType.None);

            for (int level = DifficultyTable.MinLevel; level <= DifficultyTable.MaxLevel; level++)
            {
                DifficultyTier tier = table[level];
                string label = $"Lv.{level}   해금 {tier.UnlockScore:N0}   " +
                               $"승 +{tier.WinPoints}  패 -{tier.LosePoints}";

                if (GUILayout.Button(label))
                    Apply(tier.UnlockScore, alsoLowerHighest: true);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("진행도 초기화"))
            {
                PlayerProgress.Reset();
                _scoreInput = PlayerProgress.Score;
                Repaint();
            }

            DrawStats();
            DrawWallet();

            if (!Application.isPlaying) return;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "플레이 중에 바꾼 값은 메인 메뉴나 난이도 선택 화면을 다시 열어야 반영된다.",
                MessageType.Info);
        }

        /// <summary>
        /// 전적 확인용. 화면이 네 자리 판수에서도 깨지지 않는지 보려면 수십 판을 둘 수는 없다.
        /// 승/패/무 비율은 실제에 가깝게 채워진다.
        /// </summary>
        private void DrawStats()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("전적", EditorStyles.boldLabel);

            var s = PlayerStats.Data;
            EditorGUILayout.LabelField("전적",
                $"{s.wins}승 {s.losses}패 {s.draws}무 ({s.TotalMatches}판)");
            EditorGUILayout.LabelField("승률", $"{s.WinRate * 100d:F1}%");
            EditorGUILayout.LabelField("최고 연승", $"{s.bestStreak} (현재 {s.currentStreak})");
            EditorGUILayout.LabelField("판당 평균 제거", $"{s.AverageRemoved:F1}개");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("전적 30판 채우기")) SeedStats(30);
                if (GUILayout.Button("300판")) SeedStats(300);
                if (GUILayout.Button("전적 초기화"))
                {
                    PlayerStats.Reset();
                    Repaint();
                }
            }
        }

        /// <summary>
        /// 코인·출석·보호권 확인용.
        /// 출석과 보호권은 하루 1회라 그냥은 하루에 한 번밖에 못 본다.
        /// </summary>
        private void DrawWallet()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("코인", EditorStyles.boldLabel);

            var w = PlayerWallet.Data;
            EditorGUILayout.LabelField("보유", $"{w.coins:N0}");

            int index = PlayerWallet.AttendanceIndex;
            string attendance = index >= CoinRules.AttendanceCycleLength
                ? "이번 주 완료"
                : $"{index + 1}일차 ({CoinRules.AttendanceReward(index)}코인) " +
                  $"{(PlayerWallet.CanClaimAttendance ? "수령 가능" : "오늘 받음")}";
            EditorGUILayout.LabelField("다음 출석", attendance);

            int today = PlayerWallet.Today;
            EditorGUILayout.LabelField("보호권(오늘)",
                $"코인 {(w.CanUseCoinProtection(today) ? "가능" : "사용함")}   ·   " +
                $"광고 {(w.CanUseAdProtection(today) ? "가능" : "사용함")}");
            EditorGUILayout.LabelField("코인 2배(오늘)",
                $"{w.DoubleRewardUsedToday(today)} / {CoinRules.DailyDoubleRewardLimit}");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("일일 미션", EditorStyles.boldLabel);
            for (int slot = 0; slot < MissionRules.DailyCount; slot++)
            {
                string state = PlayerMissions.IsClaimed(slot) ? "받음"
                    : PlayerMissions.CanClaim(slot) ? "수령 가능" : "진행 중";
                EditorGUILayout.LabelField(PlayerMissions.MissionAt(slot).Kind.ToString(),
                    $"{PlayerMissions.Progress(slot)} / {PlayerMissions.Target(slot)}" +
                    $"   ({PlayerMissions.Reward(slot)}코인, {state})");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("미션 전부 달성"))
                {
                    PlayerMissions.EditorCompleteAll();
                    Repaint();
                }
                if (GUILayout.Button("미션 초기화"))
                {
                    PlayerMissions.Reset();
                    Repaint();
                }
            }

            DrawDateTravel();

            _coinInput = EditorGUILayout.IntField("코인 지정", _coinInput);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("적용"))
                {
                    PlayerWallet.EditorSetCoins(_coinInput);
                    Repaint();
                }
                if (GUILayout.Button("+1000"))
                {
                    PlayerWallet.EditorSetCoins(w.coins + 1000);
                    _coinInput = PlayerWallet.Coins;
                    Repaint();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("오늘 제한 해제"))
                {
                    // 출석·보호권·2배를 다시 쓸 수 있게 만든다(날짜를 안 건드리고).
                    PlayerWallet.EditorClearDailyLimits();
                    Repaint();
                }
                if (GUILayout.Button("지갑 초기화"))
                {
                    PlayerWallet.Reset();
                    _coinInput = 0;
                    Repaint();
                }
            }
        }

        /// <summary>
        /// 게임이 보는 "오늘"을 밀어 본다.
        ///
        /// 출석은 하루 한 번뿐이라 2일차 이후를 보려면 날짜가 지나야 한다. 실제로 기다릴
        /// 수도 없고 윈도우 시계를 만지면 다른 프로그램까지 영향을 받으므로, 게임 안에서만
        /// 날짜를 옮긴다. 월요일까지 밀면 주간 초기화도 그대로 확인된다.
        /// </summary>
        private void DrawDateTravel()
        {
            int today = PlayerWallet.Today;
            System.DateTime date = WalletData.DateOf(today);
            int offset = PlayerWallet.EditorDayOffset;

            string label = $"{date:yyyy-MM-dd} ({Weekday(date)})";
            if (offset != 0) label += $"   [{offset:+#;-#;0}일]";
            EditorGUILayout.LabelField("게임이 보는 날짜", label);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("← 하루 전")) Travel(-1);
                if (GUILayout.Button("하루 뒤 →")) Travel(1);
                if (GUILayout.Button("다음 월요일")) TravelToNextMonday(today);
                if (GUILayout.Button("실제 날짜로"))
                {
                    PlayerWallet.EditorDayOffset = 0;
                    Repaint();
                }
            }

            if (offset != 0)
            {
                EditorGUILayout.HelpBox(
                    "날짜를 밀어 둔 상태입니다. 출석·보호권의 하루 제한이 이 날짜를 따릅니다.\n\n" +
                    "미래에서 출석을 받은 뒤 [실제 날짜로] 돌아오면, 시계를 되돌린 것으로 보여 " +
                    "그날이 실제로 올 때까지 출석이 잠깁니다. 그때는 [오늘 제한 해제]로 지우세요.",
                    MessageType.Warning);
            }
        }

        private void Travel(int days)
        {
            PlayerWallet.EditorDayOffset += days;
            Repaint();
        }

        /// <summary>주간 초기화를 보려면 월요일까지 가야 한다. 한 번에 건너뛴다.</summary>
        private void TravelToNextMonday(int today)
        {
            int step = 1;
            while (step < 8 && WalletData.WeekOf(today + step) == WalletData.WeekOf(today))
                step++;

            Travel(step);
        }

        private static string Weekday(System.DateTime date)
        {
            switch (date.DayOfWeek)
            {
                case System.DayOfWeek.Monday: return "월";
                case System.DayOfWeek.Tuesday: return "화";
                case System.DayOfWeek.Wednesday: return "수";
                case System.DayOfWeek.Thursday: return "목";
                case System.DayOfWeek.Friday: return "금";
                case System.DayOfWeek.Saturday: return "토";
                default: return "일";
            }
        }

        private void SeedStats(int matches)
        {
            // 매번 다른 표본이 나오도록 시각을 시드로 쓴다.
            PlayerStats.EditorSeed(matches, System.Environment.TickCount);
            Repaint();
        }

        private void Apply(int score, bool alsoLowerHighest)
        {
            PlayerProgress.EditorSetScore(score, alsoLowerHighest);
            _scoreInput = PlayerProgress.Score;
            Repaint();
        }
    }
}
