using System;
using UnityEngine;
using TMPro;

namespace DiceBattle.UI
{
    /// <summary>
    /// 메인 메뉴: 제목 + 현재 점수/등급 + "게임 시작"(등급 자동 난이도) + "게임 설명서".
    /// 제목과 버튼 사이에 빈 공간을 둬서 버튼들이 화면 아래쪽에 오도록 배치한다.
    /// 글자 크기/버튼 크기는 UiTheme의 Menu* 상수로 조절한다.
    /// </summary>
    public sealed class MenuView : MonoBehaviour
    {
        private GameObject _root;
        private TMP_Text _scoreText;

        /// <summary>등급 자동 난이도로 게임 시작.</summary>
        public event Action StartRequested;

        /// <summary>게임 설명서 열기.</summary>
        public event Action ManualRequested;

        public void Build(RectTransform root)
        {
            var bg = UiFactory.CreateStretchPanel("MenuRoot", root, UiTheme.Background);
            UiSkin.Apply(bg, UiSkin.ScreenBackground, UiTheme.Background);
            _root = bg.gameObject;

            var layout = UiFactory.AddVerticalLayout(bg.gameObject, 26,
                new RectOffset(80, 80, 70, UiTheme.MenuBottomPadding));
            // 남는 세로 공간을 아래 Spacer 하나가 전부 가져가게 한다(버튼을 하단으로 밀기 위해).
            layout.childForceExpandHeight = false;

            // 남는 공간을 제목 위/아래가 나눠 갖는다. 아래쪽(TopSpacer) 비중이 커질수록
            // 제목과 점수 사이가 벌어지고, 버튼은 그대로 화면 아래에 붙어 있다.
            var headSpacer = UiFactory.CreateRect("HeadSpacer", bg.transform);
            UiFactory.SetFlexibleHeight(headSpacer.gameObject, UiTheme.MenuHeadSpacerWeight);

            var title = UiFactory.CreateText("Title", bg.transform, "다이스 배틀",
                UiTheme.MenuTitleFontSize, UiTheme.Label);
            UiFactory.SetPreferredHeight(title.gameObject, 170f);

            var spacer = UiFactory.CreateRect("Spacer", bg.transform);
            UiFactory.SetFlexibleHeight(spacer.gameObject, UiTheme.MenuTitleSpacerWeight);

            _scoreText = UiFactory.CreateText("Score", bg.transform, "",
                UiTheme.MenuScoreFontSize, UiTheme.Label);
            UiFactory.SetPreferredHeight(_scoreText.gameObject, 90f);

            CreateCenteredButton(bg.transform, "StartButton", "게임 시작",
                UiTheme.MenuStartButtonWidth, UiTheme.MenuStartButtonHeight,
                UiTheme.MenuStartFontSize, () => StartRequested?.Invoke());

            CreateCenteredButton(bg.transform, "ManualButton", "게임 설명서",
                UiTheme.MenuManualButtonWidth, UiTheme.MenuManualButtonHeight,
                UiTheme.MenuManualFontSize, () => ManualRequested?.Invoke());

            SetVisible(false);
        }

        /// <summary>
        /// 세로 레이아웃은 자식을 가로로 늘리므로, 가로 레이아웃 한 줄로 감싸 폭을 고정한다.
        /// </summary>
        private static void CreateCenteredButton(Transform parent, string name, string text,
            float width, float height, int fontSize, Action onClick)
        {
            var row = UiFactory.CreateRect($"{name}Row", parent);
            UiFactory.AddHorizontalLayout(row.gameObject, 0, new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(row.gameObject, height);

            var button = UiFactory.CreateButton(name, row.transform, UiTheme.Button);
            UiFactory.SetSize(button.gameObject, width, height);

            var label = UiFactory.CreateText("Label", button.transform, text, fontSize, Color.white);
            UiFactory.Stretch(label.rectTransform);

            button.onClick.AddListener(() => onClick());
        }

        public void SetScore(int score, int level)
        {
            if (_scoreText != null)
                _scoreText.text = $"점수 {score}   ·   등급 Lv{level}";
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
        }
    }
}
