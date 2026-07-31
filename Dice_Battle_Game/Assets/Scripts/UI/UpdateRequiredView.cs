using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiceBattle.UI
{
    /// <summary>
    /// 강제 업데이트 안내 창. <b>닫을 수 없다.</b>
    /// 구버전을 들고 있는 사용자는 이 창을 넘어 게임에 들어갈 수단이 없어야 하므로
    /// 닫기 버튼도, 바깥을 눌러 닫는 동작도 두지 않는다. 선택지는 [업데이트]와 [종료] 뿐이다.
    ///
    /// 배경은 결과 화면과 같은 거의 불투명한 검정(<see cref="UiTheme.Overlay"/>)이라
    /// 뒤쪽 UI가 비쳐 "조금만 더 하면 될 것 같은" 인상을 주지 않는다.
    /// </summary>
    public sealed class UpdateRequiredView
    {
        private readonly GameObject _root;
        private readonly TMP_Text _message;

        private string _storeUrl;

        public bool IsOpen => _root.activeSelf;

        public UpdateRequiredView(RectTransform parent)
        {
            var overlay = UiFactory.CreateStretchPanel("UpdateRequired", parent, UiTheme.Overlay);
            UiFactory.IgnoreLayout(overlay.gameObject);
            UiFactory.Stretch(overlay.rectTransform);
            _root = overlay.gameObject;

            var window = UiFactory.CreateWindow("Window", overlay.transform,
                UiTheme.UpdateWindowWidth, UiTheme.UpdateWindowHeight);
            var layout = UiFactory.AddVerticalLayout(window.gameObject, 24, new RectOffset(70, 70, 40, 40));
            layout.childForceExpandHeight = false;

            var title = UiFactory.CreateText("Title", window, "업데이트가 필요합니다",
                UiTheme.UpdateTitleFontSize, UiTheme.Label);
            UiFactory.SetPreferredHeight(title.gameObject, 100f);

            _message = UiFactory.CreateText("Message", window, "",
                UiTheme.UpdateBodyFontSize, UiTheme.Label);
            UiFactory.SetWrap(_message, true);
            UiFactory.SetFlexibleHeight(_message.gameObject, 1f);

            // 세로 레이아웃은 자식 폭을 강제로 늘리므로, 고정 폭 버튼은 가로 줄로 감싼다.
            var buttonRow = UiFactory.CreateRect("ButtonRow", window);
            UiFactory.AddHorizontalLayout(buttonRow.gameObject, 40, new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(buttonRow.gameObject, UiTheme.UpdateButtonHeight + 10f);

            var quitButton = CreateButton(buttonRow, "QuitButton", "종료", UiTheme.CenterPanel);
            quitButton.onClick.AddListener(AppExit.Quit);

            // 오른쪽이 권장 동작.
            var updateButton = CreateButton(buttonRow, "UpdateButton", "업데이트", UiTheme.Button);
            updateButton.onClick.AddListener(OpenStore);

            _root.SetActive(false);
        }

        private static Button CreateButton(RectTransform row, string name, string text, Color color)
        {
            var button = UiFactory.CreateButton(name, row.transform, color);
            UiFactory.SetSize(button.gameObject, UiTheme.UpdateButtonWidth, UiTheme.UpdateButtonHeight);
            var label = UiFactory.CreateText("Label", button.transform, text,
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(label.rectTransform);
            return button;
        }

        /// <summary>창을 연다. 한 번 열리면 코드로도 닫지 않는다.</summary>
        public void Open(string message, string storeUrl)
        {
            _message.text = message;
            _storeUrl = storeUrl;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling(); // 무엇보다 위에
        }

        /// <summary>
        /// 스토어로 보낸다. 창은 닫지 않는다 —
        /// 사용자가 업데이트하지 않고 앱으로 돌아와도 여전히 막혀 있어야 하기 때문이다.
        /// </summary>
        private void OpenStore()
        {
            if (string.IsNullOrWhiteSpace(_storeUrl)) return;
            Application.OpenURL(_storeUrl);
        }
    }
}
