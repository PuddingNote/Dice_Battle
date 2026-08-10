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
        private TMP_Text _missionLabel;

        private const string MissionText = "일일 미션";

        /// <summary>받을 보상이 있을 때 붙는 표시. 색은 코인과 같은 금색이다.</summary>
        private const string MissionBadge = "  <color=#FFDB59>●</color>";

        /// <summary>난이도 선택 화면으로 이동.</summary>
        public event Action StartRequested;

        /// <summary>전적 창 열기.</summary>
        public event Action StatsRequested;

        /// <summary>일일 미션 창 열기.</summary>
        public event Action MissionsRequested;

        /// <summary>게임 설명서 열기.</summary>
        public event Action ManualRequested;

        /// <summary>친선대전 화면으로 이동.</summary>
        public event Action FriendlyRequested;

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

            // 윗줄: 일일 미션 / 게임 설명서. 아랫줄: 전적 / 게임 시작 / 친선대전.
            // 모든 버튼이 같은 폭(UiTheme.MenuButtonWidth)이라 두 줄이 나란한 하나의
            // 덩어리로 보인다.
            var topRow = CreateButtonRow(bg.transform, "TopRow");
            _missionLabel = AddButton(topRow, "MissionButton", MissionText,
                UiTheme.MenuButtonWidth, UiTheme.MenuButtonHeight,
                UiTheme.MenuMissionFontSize, () => MissionsRequested?.Invoke());
            AddButton(topRow, "ManualButton", "게임 설명서",
                UiTheme.MenuButtonWidth, UiTheme.MenuButtonHeight,
                UiTheme.MenuManualFontSize, () => ManualRequested?.Invoke());

            var bottomRow = CreateButtonRow(bg.transform, "BottomRow");
            AddButton(bottomRow, "StatsButton", "전적",
                UiTheme.MenuButtonWidth, UiTheme.MenuButtonHeight,
                UiTheme.MenuStatsFontSize, () => StatsRequested?.Invoke());
            AddButton(bottomRow, "StartButton", "게임 시작",
                UiTheme.MenuButtonWidth, UiTheme.MenuButtonHeight,
                UiTheme.MenuStartFontSize, () => StartRequested?.Invoke());
            AddButton(bottomRow, "FriendlyButton", "친선대전",
                UiTheme.MenuButtonWidth, UiTheme.MenuButtonHeight,
                UiTheme.MenuManualFontSize, () => FriendlyRequested?.Invoke());

            SetVisible(false);
        }

        /// <summary>
        /// 버튼이 나란히 놓일 가로 한 줄. 세로 레이아웃은 자식을 가로로 늘리므로,
        /// 이렇게 감싸야 안의 버튼들이 고정폭 그대로 가운데 정렬된다.
        /// </summary>
        private static RectTransform CreateButtonRow(Transform parent, string name)
        {
            var row = UiFactory.CreateRect(name, parent);
            UiFactory.AddHorizontalLayout(row.gameObject, UiTheme.MenuButtonGap,
                new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(row.gameObject, UiTheme.MenuButtonHeight);
            return row;
        }

        private static TMP_Text AddButton(RectTransform row, string name, string text,
            float width, float height, int fontSize, Action onClick)
        {
            var button = UiFactory.CreateButton(name, row, UiTheme.Button);
            UiFactory.SetSize(button.gameObject, width, height);

            var label = UiFactory.CreateText("Label", button.transform, text, fontSize, Color.white);
            UiFactory.Stretch(label.rectTransform);

            button.onClick.AddListener(() => onClick());
            return label;
        }

        /// <param name="unlockedLevel">해금한 가장 높은 난이도.</param>
        /// <param name="coins">보유 코인. 점수와 섞여 읽히지 않도록 금색으로 뽑는다.</param>
        public void SetScore(int score, int unlockedLevel, int coins)
        {
            if (_scoreText == null) return;

            _scoreText.text = $"점수 {score}   ·   Lv.{unlockedLevel}   ·   " +
                              $"<color={UiTheme.CoinColorHex}>{coins:N0}</color> 코인";
        }

        /// <summary>받을 미션 보상이 있으면 버튼에 표시를 붙인다.</summary>
        public void SetMissionBadge(bool hasClaimable)
        {
            if (_missionLabel == null) return;
            _missionLabel.text = hasClaimable ? MissionText + MissionBadge : MissionText;
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
        }
    }
}
