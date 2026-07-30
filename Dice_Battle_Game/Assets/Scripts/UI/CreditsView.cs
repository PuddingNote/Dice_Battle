using UnityEngine;

namespace DiceBattle.UI
{
    /// <summary>
    /// 크레딧 창(모달). 게임에 사용한 외부 리소스의 출처를 표기한다.
    /// CC BY 라이선스는 저작자 표시가 <b>의무</b>이므로 이름·라이선스·링크를 원문 그대로 둔다.
    /// 리소스를 추가하면 <see cref="Body"/>에 줄을 덧붙이면 된다.
    /// </summary>
    public sealed class CreditsView
    {
        private readonly GameObject _root;

        public bool IsOpen => _root.activeSelf;

        /// <summary>표기 문구. 저작자가 요구한 형식이므로 임의로 줄이거나 번역하지 않는다.</summary>
        private const string Body =
            "Some sounds by\n\n" +
            "Pierre-Clément KERNEIS (CC BY 4.0) - https://creativecommons.org/licenses/by/4.0/\n\n" +
            "JDSherbert - https://jdsherbert.itch.io\n\n" +
            "Kenney Vleugels (CC0) - www.kenney.nl\n\n" +
            "Jordan Irwin (CC0)";

        public CreditsView(RectTransform parent)
        {
            var backdrop = UiFactory.CreateStretchPanel("CreditsPanel", parent, UiTheme.Backdrop);
            UiFactory.IgnoreLayout(backdrop.gameObject);
            UiFactory.Stretch(backdrop.rectTransform);
            _root = backdrop.gameObject;

            var window = UiFactory.CreateWindow("Window", backdrop.transform,
                UiTheme.CreditsWindowWidth, UiTheme.CreditsWindowHeight);
            var layout = UiFactory.AddVerticalLayout(window.gameObject, 20, new RectOffset(70, 70, 36, 36));
            layout.childForceExpandHeight = false;

            var title = UiFactory.CreateText("Title", window, "크레딧",
                UiTheme.CreditsTitleFontSize, UiTheme.Label);
            UiFactory.SetPreferredHeight(title.gameObject, 90f);

            var body = UiFactory.CreateText("Body", window, Body,
                UiTheme.CreditsBodyFontSize, UiTheme.Label, TextAnchor.UpperLeft);
            UiFactory.SetWrap(body, true); // 링크가 창 폭을 넘으면 줄바꿈되도록
            UiFactory.SetFlexibleHeight(body.gameObject, 1f);

            // 세로 레이아웃은 자식을 가로로 늘리므로, 가로 줄로 한 번 감싸야 버튼 폭이 유지된다.
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

        public void Open()
        {
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        public void Close() => _root.SetActive(false);
    }
}
