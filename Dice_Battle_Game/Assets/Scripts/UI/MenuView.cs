using System;
using UnityEngine;
using TMPro;

namespace DiceBattle.UI
{
    /// <summary>
    /// 메인 메뉴: 제목 + 현재 점수/해금 난이도 + "게임 시작"(난이도 선택 화면으로) + "게임 설명서".
    /// 제목과 버튼 사이에 빈 공간을 둬서 버튼들이 화면 아래쪽에 오도록 배치한다.
    /// 글자 크기/버튼 크기는 UiTheme의 Menu* 상수로 조절한다.
    /// </summary>
    public sealed class MenuView : MonoBehaviour
    {
        private GameObject _root;
        private TMP_Text _scoreText;

        /// <summary>난이도 선택 화면으로 이동.</summary>
        public event Action StartRequested;

        /// <summary>전적 창 열기.</summary>
        public event Action StatsRequested;

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

            CreateButtonPair(bg.transform,
                "StatsButton", "전적", UiTheme.MenuStatsFontSize,
                () => StatsRequested?.Invoke(),
                "ManualButton", "게임 설명서", UiTheme.MenuManualFontSize,
                () => ManualRequested?.Invoke());

            // "게임 시작"이 맨 아래다. 가장 자주 누르는 버튼이라 엄지에서 제일 가깝다.
            CreateCenteredButton(bg.transform, "StartButton", "게임 시작",
                UiTheme.MenuStartButtonWidth, UiTheme.MenuStartButtonHeight,
                UiTheme.MenuStartFontSize, () => StartRequested?.Invoke());

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

            AddButton(row, name, text, width, height, fontSize, onClick);
        }

        /// <summary>버튼 두 개를 한 줄에 나란히 놓는다(가로 레이아웃이 가운데로 모아 준다).</summary>
        private static void CreateButtonPair(Transform parent,
            string leftName, string leftText, int leftFontSize, Action onLeft,
            string rightName, string rightText, int rightFontSize, Action onRight)
        {
            var row = UiFactory.CreateRect("MenuPairRow", parent);
            UiFactory.AddHorizontalLayout(row.gameObject, UiTheme.MenuPairGap,
                new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(row.gameObject, UiTheme.MenuPairButtonHeight);

            AddButton(row, leftName, leftText, UiTheme.MenuPairButtonWidth,
                UiTheme.MenuPairButtonHeight, leftFontSize, onLeft);
            AddButton(row, rightName, rightText, UiTheme.MenuPairButtonWidth,
                UiTheme.MenuPairButtonHeight, rightFontSize, onRight);
        }

        private static void AddButton(RectTransform row, string name, string text,
            float width, float height, int fontSize, Action onClick)
        {
            var button = UiFactory.CreateButton(name, row, UiTheme.Button);
            UiFactory.SetSize(button.gameObject, width, height);

            var label = UiFactory.CreateText("Label", button.transform, text, fontSize, Color.white);
            UiFactory.Stretch(label.rectTransform);

            button.onClick.AddListener(() => onClick());
        }

        /// <param name="unlockedLevel">해금한 가장 높은 난이도.</param>
        public void SetScore(int score, int unlockedLevel)
        {
            if (_scoreText != null)
                _scoreText.text = $"점수 {score}   ·   Lv.{unlockedLevel}";
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
        }
    }
}
