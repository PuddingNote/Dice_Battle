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

        [MenuItem("DiceBattle/진행도(디버그)")]
        private static void Open()
        {
            var window = GetWindow<ProgressDebugWindow>("진행도");
            window.minSize = new Vector2(320f, 420f);
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

            if (!Application.isPlaying) return;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "플레이 중에 바꾼 값은 메인 메뉴나 난이도 선택 화면을 다시 열어야 반영된다.",
                MessageType.Info);
        }

        private void Apply(int score, bool alsoLowerHighest)
        {
            PlayerProgress.EditorSetScore(score, alsoLowerHighest);
            _scoreInput = PlayerProgress.Score;
            Repaint();
        }
    }
}
