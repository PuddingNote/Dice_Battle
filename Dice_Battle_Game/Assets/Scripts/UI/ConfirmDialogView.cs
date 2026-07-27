using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiceBattle.UI
{
    /// <summary>
    /// 확인 다이얼로그(모바일 뒤로가기 버튼용).
    /// 결과 화면과 같은 전체 화면 오버레이 위에 문구 + [뒤로가기] [실행] 두 버튼을 표시한다.
    /// 오버레이가 화면 전체의 클릭을 막으므로 열려 있는 동안 뒤쪽 UI는 눌리지 않는다.
    /// </summary>
    public sealed class ConfirmDialogView
    {
        private readonly GameObject _root;
        private readonly TMP_Text _message;
        private readonly TMP_Text _closeLabel;
        private readonly TMP_Text _confirmLabel;

        private Action _onConfirm;

        public bool IsOpen => _root.activeSelf;

        public ConfirmDialogView(RectTransform parent)
        {
            var overlay = UiFactory.CreateStretchPanel("ConfirmDialog", parent, UiTheme.Overlay);
            UiFactory.IgnoreLayout(overlay.gameObject);
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.AddVerticalLayout(overlay.gameObject, 60, new RectOffset(60, 60, 180, 180));
            _root = overlay.gameObject;

            _message = UiFactory.CreateText("Message", overlay.transform, "", 56, UiTheme.Label);
            UiFactory.SetWrap(_message, true);
            UiFactory.SetSize(_message.gameObject, 1100f, 320f);

            var buttonRow = UiFactory.CreateRect("Buttons", overlay.transform);
            UiFactory.AddHorizontalLayout(buttonRow.gameObject, 40, new RectOffset(0, 0, 0, 0));
            UiFactory.SetSize(buttonRow.gameObject, 900f, 150f);

            // 왼쪽: 안전한 기본 동작(그냥 닫기).
            var closeButton = UiFactory.CreateButton("CloseButton", buttonRow.transform, UiTheme.Button);
            UiFactory.SetSize(closeButton.gameObject, 400f, 130f);
            _closeLabel = UiFactory.CreateText("Label", closeButton.transform, "뒤로가기",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(_closeLabel.rectTransform);
            closeButton.onClick.AddListener(Close);

            // 오른쪽: 실제 실행(메뉴로 이동 / 게임 종료).
            var confirmButton = UiFactory.CreateButton("ConfirmButton", buttonRow.transform, UiTheme.CenterPanel);
            UiFactory.SetSize(confirmButton.gameObject, 400f, 130f);
            _confirmLabel = UiFactory.CreateText("Label", confirmButton.transform, "",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(_confirmLabel.rectTransform);
            confirmButton.onClick.AddListener(Confirm);

            _root.SetActive(false);
        }

        /// <summary>다이얼로그를 연다. onConfirm은 오른쪽 버튼을 눌렀을 때 실행된다.</summary>
        public void Open(string message, string closeText, string confirmText, Action onConfirm)
        {
            _message.text = message;
            _closeLabel.text = closeText;
            _confirmLabel.text = confirmText;
            _onConfirm = onConfirm;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling(); // 항상 최상단
        }

        public void Close()
        {
            _root.SetActive(false);
            _onConfirm = null;
        }

        private void Confirm()
        {
            Action action = _onConfirm;
            Close();
            action?.Invoke();
        }
    }
}
