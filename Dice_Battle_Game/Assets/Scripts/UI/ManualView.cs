using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiceBattle.UI
{
    /// <summary>
    /// 게임 설명서 창(모달). 한 화면에 다 담기지 않으므로 좌우 화살표로 페이지를 넘긴다.
    /// 화살표는 스킨의 <see cref="UiSkin.pageArrowIcon"/>(오른쪽 방향 1장)을 쓰고,
    /// 없으면 임시로 ◀ ▶ 문자를 표시한다.
    /// </summary>
    public sealed class ManualView
    {
        private readonly GameObject _root;
        private readonly TMP_Text _titleText;
        private readonly TMP_Text _bodyText;
        private readonly TMP_Text _pageText;
        private readonly IconButton _prevButton;
        private readonly IconButton _nextButton;

        private int _page;

        public bool IsOpen => _root.activeSelf;

        private static readonly Page[] Pages =
        {
            new Page("게임 목표",
                "· 3개의 라인에서 상대와 점수를 겨룹니다.\n\n" +
                "· 각 라인에서 점수가 높은 쪽이 승리합니다.\n\n" +
                "· 더 많은 라인에서 승리하면 최종 승리합니다."),

            new Page("턴 진행",
                "· 내 차례가 되면 주사위가 자동으로 굴려집니다.\n\n" +
                "· 내 필드(왼쪽)의 라인 하나를 골라 주사위를 놓습니다.\n\n" +
                "· 라인은 총 3개고, 가득 찬 라인에는 놓을 수 없습니다.\n\n" +
                "· 양쪽 필드가 모두 채워지면 게임이 끝납니다.\n\n" +
                "· 선공은 매 판 무작위로 정해지며, 선공의 첫 주사위는 특수 주사위입니다."),

            new Page("특수 주사위",
                "· 금색으로 표시되는 주사위입니다.\n\n" +
                "· 주사위 제거가 불가능한 주사위입니다.\n\n" +
                "· 획득하는 경우는 두 가지입니다.\n" +
                "  - 선공 플레이어의 첫 주사위\n" +
                "  - 주사위 제거 후 받는 추가 주사위"),

            new Page("주사위 제거",
                "· 상대의 라인에 플레이어가 배치할 주사위와 같은 숫자가 있으면,\n" +
                "  그 주사위를 제거할 수 있습니다.\n\n" +
                "· 제거를 하면 특수 주사위를 하나 더 받아,\n" +
                "  내 라인이든 상대 라인이든 원하는 곳에 놓을 수 있습니다.\n\n" +
                "· 이 추가 주사위로는 주사위 제거를 할 수 없습니다.\n" +
                "  (한 턴에 주사위 제거는 한 번만)"),

            new Page("점수 & 리롤",
                "· 라인 점수 = 주사위 눈 합계\n\n" +
                "· 같은 숫자가 2개면 그 숫자 x 1 만큼 추가 (5, 5, 3 → 합 13 + 보너스 5 = 18점)\n\n" +
                "· 같은 숫자가 3개면 그 숫자 x 2 만큼 추가 (4, 4, 4 → 합 12 + 보너스 8 = 20점)\n\n" +
                "· 리롤은 한 판에 단 1번 사용 가능합니다.\n\n" + 
                "· 주사위를 한번 더 굴려 두개의 주사위 중 하나를 선택합니다.\n\n" +
                "· 다시 굴린 주사위는 기존과 다른 숫자가 나옵니다.\n\n" +
                "· 리롤을 모두 쓴 뒤에는 광고를 보고 한 번 더 굴릴 수 있습니다."),
        };

        public ManualView(RectTransform parent)
        {
            var backdrop = UiFactory.CreateStretchPanel("ManualPanel", parent, UiTheme.Backdrop);
            UiFactory.IgnoreLayout(backdrop.gameObject);
            UiFactory.Stretch(backdrop.rectTransform);
            _root = backdrop.gameObject;

            var window = UiFactory.CreateWindow("Window", backdrop.transform,
                UiTheme.ManualWindowWidth, UiTheme.ManualWindowHeight);
            var layout = UiFactory.AddVerticalLayout(window.gameObject, 20, new RectOffset(70, 70, 36, 36));
            layout.childForceExpandHeight = false;

            _titleText = UiFactory.CreateText("Title", window, "",
                UiTheme.ManualTitleFontSize, UiTheme.Label);
            UiFactory.SetPreferredHeight(_titleText.gameObject, 90f);

            _bodyText = UiFactory.CreateText("Body", window, "",
                UiTheme.ManualBodyFontSize, UiTheme.Label, TextAnchor.UpperLeft);
            UiFactory.SetWrap(_bodyText, true);
            UiFactory.SetFlexibleHeight(_bodyText.gameObject, 1f);

            // 아래 줄: [◀] [ 현재 / 전체 ] [▶]  + 닫기
            var footer = UiFactory.CreateRect("Footer", window);
            UiFactory.AddHorizontalLayout(footer.gameObject, 40, new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(footer.gameObject, UiTheme.ManualArrowSize + 10f);

            _prevButton = CreateArrow("PrevButton", footer, "◀");
            _prevButton.FlipIcon(); // 오른쪽 화살표 이미지를 좌우 반전해 왼쪽으로 사용
            _prevButton.Button.onClick.AddListener(() => Turn(-1));

            _pageText = UiFactory.CreateText("PageNo", footer, "",
                UiTheme.StatusFontSize, UiTheme.LabelDim);
            UiFactory.SetSize(_pageText.gameObject, 220f, UiTheme.ManualArrowSize);

            _nextButton = CreateArrow("NextButton", footer, "▶");
            _nextButton.Button.onClick.AddListener(() => Turn(1));

            var closeButton = UiFactory.CreateButton("CloseButton", footer, UiTheme.Button);
            UiFactory.SetSize(closeButton.gameObject, 280f, UiTheme.ManualArrowSize);
            var closeLabel = UiFactory.CreateText("Label", closeButton.transform, "닫기",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(closeLabel.rectTransform);
            closeButton.onClick.AddListener(Close);

            _root.SetActive(false);
        }

        private static IconButton CreateArrow(string name, RectTransform parent, string fallback)
        {
            var arrow = UiFactory.CreateIconButton(name, parent, UiSkin.PageArrowIcon, fallback,
                UiTheme.ManualArrowSize, 18f, 52);
            UiFactory.SetSize(arrow.Button.gameObject, UiTheme.ManualArrowSize, UiTheme.ManualArrowSize);
            return arrow;
        }

        public void Open()
        {
            _page = 0;
            Render();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        public void Close() => _root.SetActive(false);

        private void Turn(int delta)
        {
            _page = Mathf.Clamp(_page + delta, 0, Pages.Length - 1);
            Render();
        }

        private void Render()
        {
            _titleText.text = Pages[_page].Title;
            _bodyText.text = Pages[_page].Body;
            _pageText.text = $"{_page + 1} / {Pages.Length}";
            _prevButton.SetInteractable(_page > 0);
            _nextButton.SetInteractable(_page < Pages.Length - 1);
        }

        private readonly struct Page
        {
            public string Title { get; }
            public string Body { get; }

            public Page(string title, string body)
            {
                Title = title;
                Body = body;
            }
        }
    }
}
