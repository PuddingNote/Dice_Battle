using System;
using UnityEngine;
using UnityEngine.UI;
using DiceBattle.AI;

namespace DiceBattle.UI
{
    /// <summary>
    /// 최소 placeholder 메인 메뉴: 제목 + 현재 점수/등급 + "게임 시작"(등급 자동 난이도).
    /// 아래에 테스트용 난이도 직접 선택(1~5)도 제공.
    /// (추후 스프라이트 기반 실제 메뉴로 교체)
    /// </summary>
    public sealed class MenuView : MonoBehaviour
    {
        private GameObject _root;
        private Text _scoreText;

        /// <summary>등급 자동 난이도로 게임 시작.</summary>
        public event Action StartRequested;
        /// <summary>테스트: 난이도(1~5) 직접 선택.</summary>
        public event Action<int> LevelSelected;

        public void Build(RectTransform root)
        {
            var bg = UiFactory.CreateStretchPanel("MenuRoot", root, UiTheme.Background);
            UiSkin.Apply(bg, UiSkin.ScreenBackground, UiTheme.Background);
            _root = bg.gameObject;
            UiFactory.AddVerticalLayout(bg.gameObject, 32, new RectOffset(80, 80, 70, 70));

            var title = UiFactory.CreateText("Title", bg.transform, "다이스 배틀", 92, UiTheme.Label);
            UiFactory.SetPreferredHeight(title.gameObject, 160f);

            _scoreText = UiFactory.CreateText("Score", bg.transform, "", UiTheme.StatusFontSize, UiTheme.Label);
            UiFactory.SetPreferredHeight(_scoreText.gameObject, 70f);

            var startBtn = UiFactory.CreateButton("StartButton", bg.transform, UiTheme.Button);
            UiFactory.SetPreferredHeight(startBtn.gameObject, 150f);
            var startText = UiFactory.CreateText("Label", startBtn.transform, "게임 시작", 52, Color.white);
            UiFactory.Stretch(startText.rectTransform);
            startBtn.onClick.AddListener(() => StartRequested?.Invoke());

            var testLabel = UiFactory.CreateText("TestLabel", bg.transform, "(테스트) 난이도 직접 선택",
                UiTheme.ScoreFontSize, UiTheme.LabelDim);
            UiFactory.SetPreferredHeight(testLabel.gameObject, 60f);

            var row = UiFactory.CreateRect("LevelButtons", bg.transform);
            UiFactory.AddHorizontalLayout(row.gameObject, 20, new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(row.gameObject, 140f);

            for (int level = LeveledAiStrategy.MinLevel; level <= LeveledAiStrategy.MaxLevel; level++)
            {
                int captured = level;
                var btn = UiFactory.CreateButton($"Lv{level}", row.transform, UiTheme.CenterPanel);
                UiFactory.SetSize(btn.gameObject, 180f, 130f);
                var label = UiFactory.CreateText("Label", btn.transform, $"Lv{level}",
                    UiTheme.StatusFontSize, Color.white);
                UiFactory.Stretch(label.rectTransform);
                btn.onClick.AddListener(() => LevelSelected?.Invoke(captured));
            }

            SetVisible(false);
        }

        public void SetScore(int score, int level)
        {
            if (_scoreText != null)
                _scoreText.text = $"점수 {score}   ·   등급 Lv{level}   (선공: 랜덤)";
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
        }
    }
}
