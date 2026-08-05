using UnityEngine;
using TMPro;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 전적 창(모달). 메인 메뉴의 "전적" 버튼으로 연다.
    ///
    /// <b>난이도별 전적은 저장하지만 여기 보여주지 않는다.</b> 화면을 단순하게 두기로 한
    /// 결정이고, 저장된 값은 나중에 연승 보너스·배팅 수치를 정할 근거 데이터로 쓴다.
    ///
    /// 값은 <see cref="Open"/>할 때마다 다시 읽는다. 매 판 갱신되므로 만들 때 한 번
    /// 채워 두면 두 번째로 열었을 때 낡은 값이 보인다.
    /// </summary>
    public sealed class StatsView
    {
        private readonly GameObject _root;

        private readonly TMP_Text _record;
        private readonly TMP_Text _winRate;
        private readonly TMP_Text _bestStreak;
        private readonly TMP_Text _highestScore;
        private readonly TMP_Text _unlocked;
        private readonly TMP_Text _averageRemoved;

        /// <summary>한 판도 두지 않았을 때 0/0.0%가 늘어서는 것보다 낫다.</summary>
        private const string Empty = "-";

        public bool IsOpen => _root.activeSelf;

        public StatsView(RectTransform parent)
        {
            var backdrop = UiFactory.CreateStretchPanel("StatsPanel", parent, UiTheme.Backdrop);
            UiFactory.IgnoreLayout(backdrop.gameObject);
            UiFactory.Stretch(backdrop.rectTransform);
            _root = backdrop.gameObject;

            var window = UiFactory.CreateWindow("Window", backdrop.transform,
                UiTheme.StatsWindowWidth, UiTheme.StatsWindowHeight);
            var layout = UiFactory.AddVerticalLayout(window.gameObject,
                UiTheme.StatsRowSpacing, new RectOffset(90, 90, 36, 36));
            layout.childForceExpandHeight = false;

            var title = UiFactory.CreateText("Title", window, "전적",
                UiTheme.StatsTitleFontSize, UiTheme.Label);
            UiFactory.SetPreferredHeight(title.gameObject, 90f);

            _record = CreateRow(window, "전적");
            _winRate = CreateRow(window, "승률");
            _bestStreak = CreateRow(window, "최고 연승");
            _averageRemoved = CreateRow(window, "판당 평균 제거");
            _highestScore = CreateRow(window, "최고 점수");
            _unlocked = CreateRow(window, "해금 난이도");

            // 남는 세로 공간을 전부 먹어 버튼을 창 아래에 붙인다.
            var spacer = UiFactory.CreateRect("Spacer", window);
            UiFactory.SetFlexibleHeight(spacer.gameObject, 1f);

            var buttonRow = UiFactory.CreateRect("ButtonRow", window);
            UiFactory.AddHorizontalLayout(buttonRow.gameObject, 0, new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(buttonRow.gameObject, 120f);

            var closeButton = UiFactory.CreateButton("CloseButton", buttonRow.transform, UiTheme.Button);
            UiFactory.SetSize(closeButton.gameObject, 320f, 110f);
            var closeLabel = UiFactory.CreateText("Label", closeButton.transform, "닫기",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(closeLabel.rectTransform);
            closeButton.onClick.AddListener(Close);

            _root.SetActive(false);
        }

        /// <summary>
        /// "라벨 ....... 값" 한 줄. 라벨은 왼쪽, 값은 오른쪽 끝에 붙는다.
        /// 세로 레이아웃이 자식을 가로로 늘리므로 가로 줄로 감싸야 두 칸이 나뉜다.
        /// </summary>
        private static TMP_Text CreateRow(Transform parent, string label)
        {
            var row = UiFactory.CreateRect($"{label}Row", parent);
            UiFactory.AddHorizontalLayout(row.gameObject, 0, new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(row.gameObject, UiTheme.StatsRowHeight);

            var name = UiFactory.CreateText("Label", row, label,
                UiTheme.StatsLabelFontSize, UiTheme.LabelDim, TextAnchor.MiddleLeft);
            UiFactory.SetFlexible(name.gameObject);

            var value = UiFactory.CreateText("Value", row, Empty,
                UiTheme.StatsValueFontSize, UiTheme.Label, TextAnchor.MiddleRight);
            UiFactory.SetFlexible(value.gameObject);

            return value;
        }

        public void Open()
        {
            Refresh();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        public void Close() => _root.SetActive(false);

        private void Refresh()
        {
            StatsData s = PlayerStats.Data;
            bool played = s.TotalMatches > 0;

            _record.text = played
                ? $"{s.wins}승 {s.losses}패 {s.draws}무   ({s.TotalMatches}판)"
                : Empty;

            // 무승부도 분모에 넣는다. 전적 줄에 무승부를 따로 보여주므로 오해할 여지가 없다.
            _winRate.text = played ? $"{s.WinRate * 100d:F1}%" : Empty;

            _bestStreak.text = played ? $"{s.bestStreak}연승" : Empty;
            _averageRemoved.text = played ? $"{s.AverageRemoved:F1}개" : Empty;

            // 점수와 해금은 전적과 별개로 항상 값이 있다(첫 실행도 0점 / Lv.1).
            _highestScore.text = $"{PlayerProgress.HighestScore:N0}";
            _unlocked.text = $"Lv.{PlayerProgress.MaxUnlockedLevel}";
        }
    }
}
