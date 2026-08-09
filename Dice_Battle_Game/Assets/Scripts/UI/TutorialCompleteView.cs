using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiceBattle.UI
{
    /// <summary>
    /// 튜토리얼 전용 완료 화면.
    ///
    /// 평소 결과 화면을 쓰지 않는 이유: 튜토리얼은 점수도 전적도 건드리지 않으므로
    /// "점수 +0 → 0" 같은 빈 줄만 남고, 해금 안내가 들어갈 자리도 비어 버린다.
    /// 첫 화면에서 의미 없는 0을 보여 주는 것보다 무엇을 얻었는지만 적는 편이 낫다.
    /// </summary>
    public sealed class TutorialCompleteView
    {
        private readonly GameObject _root;
        private readonly TMP_Text _bodyText;

        /// <summary>확인 버튼을 눌렀을 때.</summary>
        public event Action Closed;

        public bool IsOpen => _root.activeSelf;

        public TutorialCompleteView(RectTransform parent)
        {
            var backdrop = UiFactory.CreateStretchPanel("TutorialDonePanel", parent, UiTheme.Overlay);
            UiFactory.IgnoreLayout(backdrop.gameObject);
            UiFactory.Stretch(backdrop.rectTransform);
            _root = backdrop.gameObject;

            var window = UiFactory.CreateWindow("Window", backdrop.transform,
                UiTheme.TutorialDoneWindowWidth, UiTheme.TutorialDoneWindowHeight);
            var layout = UiFactory.AddVerticalLayout(window.gameObject, 24, new RectOffset(60, 60, 40, 40));
            layout.childForceExpandHeight = false;

            // 제목은 승패로 갈리지 않는다. 뒷구간이 자유 배치라 뒤집힐 수도, 끝까지 두지
            // 않고 마칠 수도 있는데, 어느 쪽이든 배울 것을 다 배웠으면 완료다.
            var title = UiFactory.CreateText("Title", window, "튜토리얼 완료!",
                UiTheme.TutorialDoneTitleFontSize, UiTheme.Label);
            UiFactory.SetPreferredHeight(title.gameObject, 100f);

            _bodyText = UiFactory.CreateText("Body", window, "",
                UiTheme.TutorialDoneBodyFontSize, UiTheme.Label);
            UiFactory.SetWrap(_bodyText, true);
            UiFactory.SetFlexibleHeight(_bodyText.gameObject, 1f);

            var buttonRow = UiFactory.CreateRect("Buttons", window);
            UiFactory.AddHorizontalLayout(buttonRow.gameObject, 0, new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(buttonRow.gameObject, UiTheme.TutorialDoneButtonHeight);

            var button = UiFactory.CreateButton("StartButton", buttonRow.transform, UiTheme.Button);
            UiFactory.SetSize(button.gameObject,
                UiTheme.TutorialDoneButtonWidth, UiTheme.TutorialDoneButtonHeight);
            var label = UiFactory.CreateText("Label", button.transform, "난이도 고르기",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(label.rectTransform);
            button.onClick.AddListener(() =>
            {
                Close();
                Closed?.Invoke();
            });

            _root.SetActive(false);
        }

        /// <summary>
        /// 완료 화면을 연다.
        /// </summary>
        /// <param name="coins">지급한 코인. 0이면 보상 줄을 적지 않는다(이미 받은 경우).</param>
        /// <param name="ahead">
        /// 상대보다 앞선 채로 끝났는가. 뒷구간은 자유 배치라 뒤집힐 수 있고,
        /// 판을 끝까지 두지 않고 마칠 수도 있어 "승리"라고 단정하지 않는다.
        /// 뒤졌다고 튜토리얼이 실패한 것은 아니므로 보상과 다음 걸음은 그대로 준다.
        /// </param>
        public void Open(int coins, bool ahead)
        {
            string head = "배치 · 제거 · 특수 주사위 · 리롤까지 모두 익혔습니다.\n" +
                          (ahead
                              ? "상대보다 앞선 채로 마쳤네요."
                              : "이번 판은 아쉬웠지만, 실전에서 갚아 주면 됩니다.");

            string reward = coins > 0
                ? $"\n\n<color={UiTheme.CoinColorHex}>+{coins} 코인</color>"
                : "";

            _bodyText.text = head + reward +
                "\n\n이제 난이도를 골라 실전을 시작해 보세요.\n" +
                "<color=#B0B4BC>점수 계산 방법은 설정 → 게임 설명서에 있습니다.</color>";

            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        public void Close() => _root.SetActive(false);
    }
}
