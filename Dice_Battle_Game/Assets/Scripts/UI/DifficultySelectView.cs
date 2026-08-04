using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 난이도 선택 화면. Lv.1~10을 5열 2행 그리드로 보여준다.
    ///
    /// 잠긴 난이도도 <b>자리를 차지한 채 함께 보여준다.</b> 목표가 눈에 보여야
    /// 점수를 모을 이유가 생기기 때문이다. 스크롤을 쓰지 않는 것도 같은 이유다 —
    /// 화면 밖으로 밀려난 목표는 없는 것과 같다.
    /// </summary>
    public sealed class DifficultySelectView : MonoBehaviour
    {
        private GameObject _root;
        private TMP_Text _summary;
        private TMP_Text _startLabel;

        private readonly LevelCard[] _cards = new LevelCard[DifficultyTable.LevelCount];
        private int _selected = DifficultyTable.MinLevel;

        /// <summary>선택한 난이도로 게임 시작.</summary>
        public event Action<int> StartRequested;

        /// <summary>메인 메뉴로 돌아가기.</summary>
        public event Action BackRequested;

        public void Build(RectTransform root)
        {
            var bg = UiFactory.CreateStretchPanel("DifficultySelectRoot", root, UiTheme.Background);
            UiSkin.Apply(bg, UiSkin.ScreenBackground, UiTheme.Background);
            _root = bg.gameObject;

            var layout = UiFactory.AddVerticalLayout(bg.gameObject, 18, new RectOffset(90, 90, 36, 44));
            layout.childForceExpandHeight = false;

            var title = UiFactory.CreateText("Title", bg.transform, "난이도 선택",
                UiTheme.DifficultyTitleFontSize, UiTheme.Label);
            UiFactory.SetPreferredHeight(title.gameObject, 96f);

            _summary = UiFactory.CreateText("Summary", bg.transform, "",
                UiTheme.DifficultySummaryFontSize, UiTheme.Label);
            UiFactory.SetPreferredHeight(_summary.gameObject, 56f);

            BuildGrid(bg.transform);

            // 그리드와 하단 버튼 사이의 남는 공간을 전부 먹어 버튼을 아래로 민다.
            var spacer = UiFactory.CreateRect("Spacer", bg.transform);
            UiFactory.SetFlexibleHeight(spacer.gameObject, 1f);

            BuildFooter(bg.transform);

            SetVisible(false);
        }

        private void BuildGrid(Transform parent)
        {
            var grid = UiFactory.CreateRect("Grid", parent);
            var v = UiFactory.AddVerticalLayout(grid.gameObject,
                UiTheme.DifficultyRowSpacing, new RectOffset(0, 0, 0, 0));
            v.childForceExpandHeight = false;
            UiFactory.SetPreferredHeight(grid.gameObject,
                UiTheme.DifficultyCardHeight * 2f + UiTheme.DifficultyRowSpacing);

            const int perRow = 5;
            for (int row = 0; row < DifficultyTable.LevelCount / perRow; row++)
            {
                var rowRect = UiFactory.CreateRect($"Row{row}", grid);
                UiFactory.AddHorizontalLayout(rowRect.gameObject,
                    UiTheme.DifficultyCardSpacing, new RectOffset(0, 0, 0, 0));
                UiFactory.SetPreferredHeight(rowRect.gameObject, UiTheme.DifficultyCardHeight);

                for (int col = 0; col < perRow; col++)
                {
                    int level = row * perRow + col + DifficultyTable.MinLevel;
                    _cards[level - DifficultyTable.MinLevel] =
                        new LevelCard(rowRect, level, Select);
                }
            }
        }

        private void BuildFooter(Transform parent)
        {
            var footer = UiFactory.CreateRect("Footer", parent);
            UiFactory.AddHorizontalLayout(footer.gameObject, 36, new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(footer.gameObject, UiTheme.DifficultyFooterButtonHeight);

            var back = UiFactory.CreateButton("BackButton", footer.transform, UiTheme.CenterPanel);
            UiFactory.SetSize(back.gameObject,
                UiTheme.DifficultyBackButtonWidth, UiTheme.DifficultyFooterButtonHeight);
            var backLabel = UiFactory.CreateText("Label", back.transform, "뒤로",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(backLabel.rectTransform);
            back.onClick.AddListener(() => BackRequested?.Invoke());

            var start = UiFactory.CreateButton("StartButton", footer.transform, UiTheme.Button);
            UiFactory.SetSize(start.gameObject,
                UiTheme.DifficultyStartButtonWidth, UiTheme.DifficultyFooterButtonHeight);
            _startLabel = UiFactory.CreateText("Label", start.transform, "",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(_startLabel.rectTransform);
            start.onClick.AddListener(() => StartRequested?.Invoke(_selected));
        }

        /// <summary>
        /// 화면을 열면서 카드 상태를 다시 그린다.
        /// 판이 끝날 때마다 해금이 늘 수 있으므로 열 때마다 갱신해야 한다.
        /// </summary>
        /// <param name="selectedLevel">기본 선택. 해금 범위를 벗어나면 접힌다.</param>
        /// <param name="newFrom">이번에 새로 열린 난이도 구간의 시작(없으면 0).</param>
        /// <param name="newTo">이번에 새로 열린 난이도 구간의 끝(없으면 0).</param>
        public void Open(DifficultyTable table, int score, int highestScore, int selectedLevel,
            int newFrom = 0, int newTo = 0)
        {
            int maxUnlocked = table.MaxUnlockedLevel(highestScore);

            for (int i = 0; i < _cards.Length; i++)
            {
                int level = i + DifficultyTable.MinLevel;
                bool isNew = level >= newFrom && level <= newTo;
                _cards[i].Render(table[level], level <= maxUnlocked, isNew, highestScore);
            }

            Select(table.ClampToUnlocked(selectedLevel, highestScore));

            // 다음 해금까지 남은 점수는 잠긴 카드마다 적혀 있으므로 여기서는 빼둔다.
            //
            // 두 점수를 같이 보여주는 건 필요하다. 해금은 최고 점수로만 판정하므로,
            // 둘이 다를 때(=지고 있는 중) 현재 점수만 보이면 "왜 해금이 안 풀리지"가 되고
            // 최고 점수만 보이면 "내 점수가 왜 이래"가 된다.
            _summary.text = $"현재 점수 {score:N0}   ·   최고 점수 {highestScore:N0}";

            SetVisible(true);
        }

        private void Select(int level)
        {
            _selected = level;
            for (int i = 0; i < _cards.Length; i++)
                _cards[i].SetSelected(_cards[i].Level == level);

            // 어떤 난이도로 시작하는지 버튼에 그대로 적는다.
            // 선택은 항상 하나가 잡혀 있으므로 버튼이 비활성인 상태는 존재하지 않는다.
            _startLabel.text = $"Lv.{level} 시작";
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
        }

        /// <summary>난이도 한 칸. 해금 여부에 따라 표시와 클릭 가능 여부가 갈린다.</summary>
        private sealed class LevelCard
        {
            public int Level { get; }

            private readonly Button _button;
            private readonly Image _background;
            private readonly Image _highlight;
            private readonly TMP_Text _title;
            private readonly TMP_Text _line1;
            private readonly TMP_Text _line2;

            private bool _unlocked;

            public LevelCard(Transform parent, int level, Action<int> onClick)
            {
                Level = level;

                _button = UiFactory.CreateButton($"Level{level}", parent, UiTheme.DifficultyCardUnlocked);
                _background = _button.GetComponent<Image>();

                // 카드 색이 곧 상태 표시(잠김/선택)라, 버튼의 자동 색 전환이 그 위를 덮으면 안 된다.
                // 특히 잠긴 카드는 interactable=false 순간 회색 틴트가 씌워져 의도한 색을 잃는다.
                // 누르는 느낌은 선택 하이라이트가 바로 바뀌는 것으로 대신한다.
                _button.transition = Selectable.Transition.None;
                UiFactory.SetSize(_button.gameObject,
                    UiTheme.DifficultyCardWidth, UiTheme.DifficultyCardHeight);

                var v = UiFactory.AddVerticalLayout(_button.gameObject, 8, new RectOffset(12, 12, 16, 16));
                v.childForceExpandHeight = false;

                // 글자보다 먼저 만들어 뒤에 깔린다.
                _highlight = CreateHighlight(_button.transform);

                _title = UiFactory.CreateText("Level", _button.transform, $"Lv.{level}",
                    UiTheme.DifficultyLevelFontSize, UiTheme.Label);
                UiFactory.SetPreferredHeight(_title.gameObject, 84f);

                _line1 = UiFactory.CreateText("Line1", _button.transform, "",
                    UiTheme.DifficultyCardInfoFontSize, UiTheme.Label);
                UiFactory.SetPreferredHeight(_line1.gameObject, 46f);

                _line2 = UiFactory.CreateText("Line2", _button.transform, "",
                    UiTheme.DifficultyCardInfoFontSize, UiTheme.Label);
                UiFactory.SetPreferredHeight(_line2.gameObject, 46f);

                _button.onClick.AddListener(() => onClick(level));
            }

            /// <param name="isNew">이번 판으로 막 열린 난이도인가(금색 강조).</param>
            public void Render(DifficultyTier tier, bool unlocked, bool isNew, int highestScore)
            {
                _unlocked = unlocked;
                _button.interactable = unlocked;

                if (unlocked)
                {
                    // 막 열린 난이도는 특수 주사위와 같은 금색으로 적는다.
                    // 카드 10장 중 어느 게 새 것인지 한눈에 찾을 수 있어야 한다.
                    _title.color = isNew ? UiTheme.CellSpecial : UiTheme.Label;
                    _line1.text = $"승 +{tier.WinPoints}";
                    _line1.color = UiTheme.WinText;
                    _line2.text = $"패 -{tier.LosePoints}";
                    _line2.color = UiTheme.LoseText;
                }
                else
                {
                    // 얼마나 남았는지를 적는다. 필요 점수만 적으면 지금 위치를 따로 계산해야 한다.
                    int remain = tier.UnlockScore - highestScore;
                    if (remain < 0) remain = 0;

                    _title.color = UiTheme.LabelDim;
                    _line1.text = "잠김";
                    _line1.color = UiTheme.LabelDim;
                    _line2.text = $"{remain:N0}점 남음";
                    _line2.color = UiTheme.LabelDim;
                }

                SetSelected(false);
            }

            public void SetSelected(bool on)
            {
                Color color = !_unlocked ? UiTheme.DifficultyCardLocked
                    : on ? UiTheme.DifficultyCardSelected
                    : UiTheme.DifficultyCardUnlocked;

                // 스킨 스프라이트가 있으면 원본색으로 깔리므로, 상태 색은 그 위에 직접 덮는다
                // (라인 박스 강조와 같은 방식).
                UiSkin.Apply(_background, UiSkin.Button, color);
                _background.color = color;

                _highlight.enabled = on && _unlocked;
            }

            /// <summary>
            /// 선택 표시용 초록 테두리.
            /// 라인 박스·리롤 트레이와 <b>같은 스프라이트에 같은 초록</b>을 써서,
            /// "여기가 지금 고른 것"이라는 신호가 게임 안 어디서든 같은 의미로 읽히게 한다.
            /// </summary>
            private static Image CreateHighlight(Transform card)
            {
                var img = UiFactory.CreatePanel("Highlight", card, UiTheme.LineHighlightSolid);
                img.raycastTarget = false;

                // 카드는 세로 레이아웃이라 그냥 두면 글자들 사이에 한 칸을 차지한다.
                UiFactory.IgnoreLayout(img.gameObject);
                UiFactory.Stretch(img.rectTransform);

                if (UiSkin.LineNormal != null)
                {
                    img.sprite = UiSkin.LineNormal;
                    img.type = Image.Type.Sliced;
                    img.color = UiTheme.LineHighlightSolid;
                }
                else
                {
                    // 테두리 이미지가 없을 때의 폴백. 반투명 초록이 카드 전체에 덮인다.
                    img.sprite = UiSprites.RoundedRect;
                    img.type = Image.Type.Sliced;
                    img.color = UiTheme.LineHighlight;
                }

                img.enabled = false;
                return img;
            }
        }
    }
}
