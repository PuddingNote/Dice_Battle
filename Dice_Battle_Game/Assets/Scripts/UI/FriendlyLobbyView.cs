using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiceBattle.UI
{
    /// <summary>
    /// 친선대전 진입 화면. 하나의 GameObject 트리 안에 세 하위 화면을 두고 토글한다:
    ///   진입(방 만들기/코드로 입장하기 선택) → 방 만들기(코드 표시+대기) 또는 코드 입장(입력+에러).
    ///
    /// 방 코드 시스템 자체(Firebase 호출)는 이 화면이 모른다 — GameManager가
    /// <see cref="CreateRequested"/>/<see cref="JoinRequested"/>를 받아 FriendlyRoomService를
    /// 부르고, 결과를 다시 <see cref="ShowCreatedCode"/> 등으로 이 화면에 되돌려준다.
    /// (DifficultySelectView가 점수/해금 판정을 모르는 것과 같은 분리 원칙.)
    /// </summary>
    public sealed class FriendlyLobbyView : MonoBehaviour
    {
        private enum SubScreen { Entry, Create, Join }

        private GameObject _root;
        private SubScreen _sub = SubScreen.Entry;

        private GameObject _entryPanel;
        private GameObject _createPanel;
        private GameObject _joinPanel;

        private TMP_Text _footerBackLabel;

        // 방 만들기 하위 화면
        private TMP_Text _codeText;
        private TMP_Text _createStatusText;
        private GameObject _codeGroup; // 코드+복사 버튼(대기 상태로 넘어가면 그대로 두고 문구만 바뀜)

        // 코드 입장 하위 화면
        private TMP_InputField _joinInput;
        private TMP_Text _joinErrorText;
        private Button _joinButton;

        /// <summary>진입 화면에서 뒤로가기 — 메인 메뉴로 나간다.</summary>
        public event Action ExitRequested;

        /// <summary>방 만들기/코드 입장 하위 화면에서 취소 — 진입 화면으로 돌아간다.
        /// GameManager는 이 시점에 진행 중이던 방 리스너를 정리해야 한다.</summary>
        public event Action BackFromSubScreenRequested;

        public event Action CreateRequested;
        public event Action<string> JoinRequested;

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Build(RectTransform root)
        {
            var bg = UiFactory.CreateStretchPanel("FriendlyLobbyRoot", root, UiTheme.Background);
            UiSkin.Apply(bg, UiSkin.ScreenBackground, UiTheme.Background);
            _root = bg.gameObject;

            var layout = UiFactory.AddVerticalLayout(bg.gameObject, 18, new RectOffset(90, 90, 36, 44));
            layout.childForceExpandHeight = false;

            var title = UiFactory.CreateText("Title", bg.transform, "친선대전",
                UiTheme.DifficultyTitleFontSize, UiTheme.Label);
            UiFactory.SetPreferredHeight(title.gameObject, 96f);

            var body = UiFactory.CreateRect("Body", bg.transform);
            UiFactory.SetFlexibleHeight(body.gameObject, 1f);

            _entryPanel = BuildEntryPanel(body);
            _createPanel = BuildCreatePanel(body);
            _joinPanel = BuildJoinPanel(body);

            BuildFooter(bg.transform);

            SetVisible(false);
        }

        // ---- 진입 화면 ----

        private GameObject BuildEntryPanel(Transform parent)
        {
            var panel = UiFactory.CreateRect("EntryPanel", parent);
            UiFactory.Stretch(panel); // body엔 레이아웃 그룹이 없어 직접 채워야 한다(아래 두 패널도 동일)
            var v = UiFactory.AddVerticalLayout(panel.gameObject, 30, new RectOffset(0, 0, 40, 0));
            v.childForceExpandHeight = false;

            var desc = UiFactory.CreateText("Desc", panel.transform,
                "지인과 방 코드로 1:1 대결합니다.\n점수·코인에는 영향을 주지 않습니다.",
                UiTheme.FriendlyDescFontSize, UiTheme.Label);
            UiFactory.SetWrap(desc, true);
            UiFactory.SetPreferredHeight(desc.gameObject, 140f);

            CreateCenteredButton(panel.transform, "CreateButton", "방 만들기",
                UiTheme.FriendlyEntryButtonWidth, UiTheme.FriendlyEntryButtonHeight,
                UiTheme.MenuStatsFontSize, () => CreateRequested?.Invoke());
            CreateCenteredButton(panel.transform, "JoinButton", "코드로 입장하기",
                UiTheme.FriendlyEntryButtonWidth, UiTheme.FriendlyEntryButtonHeight,
                UiTheme.MenuStatsFontSize,
                () => { _joinErrorText.text = ""; _joinInput.text = ""; SetSub(SubScreen.Join); });

            return panel.gameObject;
        }

        // ---- 방 만들기 화면 ----

        private GameObject BuildCreatePanel(Transform parent)
        {
            var panel = UiFactory.CreateRect("CreatePanel", parent);
            UiFactory.Stretch(panel);
            var v = UiFactory.AddVerticalLayout(panel.gameObject, 24, new RectOffset(0, 0, 40, 0));
            v.childForceExpandHeight = false;

            _codeGroup = UiFactory.CreateRect("CodeGroup", panel.transform).gameObject;
            var codeV = UiFactory.AddVerticalLayout(_codeGroup, 16, new RectOffset(0, 0, 0, 0));
            codeV.childForceExpandHeight = false;
            UiFactory.SetPreferredHeight(_codeGroup, 220f);

            _codeText = UiFactory.CreateText("Code", _codeGroup.transform, "----",
                88, UiTheme.WinText);
            UiFactory.SetPreferredHeight(_codeText.gameObject, 120f);

            CreateCenteredButton(_codeGroup.transform, "CopyButton", "코드 복사", 260f, 90f,
                UiTheme.StatusFontSize, () => GUIUtility.systemCopyBuffer = _codeText.text);

            _createStatusText = UiFactory.CreateText("Status", panel.transform, "",
                UiTheme.FriendlyDescFontSize, UiTheme.Label);
            UiFactory.SetWrap(_createStatusText, true);
            UiFactory.SetPreferredHeight(_createStatusText.gameObject, 130f);

            return panel.gameObject;
        }

        // ---- 코드 입장 화면 ----

        private GameObject BuildJoinPanel(Transform parent)
        {
            var panel = UiFactory.CreateRect("JoinPanel", parent);
            UiFactory.Stretch(panel);
            var v = UiFactory.AddVerticalLayout(panel.gameObject, 24, new RectOffset(0, 0, 40, 0));
            v.childForceExpandHeight = false;

            // 가로 레이아웃 행 하나에 입력칸만 담는다 — childForceExpandWidth=false라
            // 입력칸이 SetSize로 지정한 고정폭 그대로 가운데 정렬된다(다른 고정폭 버튼들과 같은 원리).
            var inputRow = UiFactory.CreateRect("InputRow", panel.transform);
            UiFactory.AddHorizontalLayout(inputRow.gameObject, 0, new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(inputRow.gameObject, 120f);

            _joinInput = UiFactory.CreateInputField("CodeInput", inputRow.transform, 64, "코드 4자리");
            _joinInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            _joinInput.characterLimit = 4;
            UiFactory.SetSize(_joinInput.gameObject, 420f, 120f);
            _joinInput.onSubmit.AddListener(_ => TrySubmitJoin());

            _joinButton = CreateCenteredButton(panel.transform, "JoinSubmitButton", "입장", 320f, 120f,
                UiTheme.StatusFontSize, TrySubmitJoin);

            _joinErrorText = UiFactory.CreateText("Error", panel.transform, "",
                UiTheme.FriendlyDescFontSize, UiTheme.Label);
            UiFactory.SetWrap(_joinErrorText, true);
            UiFactory.SetPreferredHeight(_joinErrorText.gameObject, 130f);

            return panel.gameObject;
        }

        private void TrySubmitJoin()
        {
            string code = _joinInput.text.Trim();
            if (code.Length == 0) return;
            JoinRequested?.Invoke(code);
        }

        // ---- 공통 ----

        private void BuildFooter(Transform parent)
        {
            var footer = UiFactory.CreateRect("Footer", parent);
            UiFactory.AddHorizontalLayout(footer.gameObject, 36, new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(footer.gameObject, UiTheme.DifficultyFooterButtonHeight);

            var back = UiFactory.CreateButton("BackButton", footer.transform, UiTheme.CenterPanel);
            UiFactory.SetSize(back.gameObject,
                UiTheme.DifficultyBackButtonWidth, UiTheme.DifficultyFooterButtonHeight);
            _footerBackLabel = UiFactory.CreateText("Label", back.transform, "뒤로",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(_footerBackLabel.rectTransform);
            back.onClick.AddListener(HandleBackPressed);
        }

        /// <summary>
        /// 세로 레이아웃 부모 안에 고정폭 버튼을 가운데 정렬로 놓는다. 세로 레이아웃은
        /// 기본적으로 자식을 부모 폭 전체로 늘리므로(MenuView의 같은 패턴 참고),
        /// 가로 레이아웃 행 하나로 감싸서(childForceExpandWidth=false) 폭을 고정한다.
        /// </summary>
        private static Button CreateCenteredButton(Transform parent, string name, string text,
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
            return button;
        }

        private void SetSub(SubScreen sub)
        {
            _sub = sub;
            _entryPanel.SetActive(sub == SubScreen.Entry);
            _createPanel.SetActive(sub == SubScreen.Create);
            _joinPanel.SetActive(sub == SubScreen.Join);
            _footerBackLabel.text = sub == SubScreen.Entry ? "메인 메뉴로" : "취소";
        }

        /// <summary>온스크린 뒤로가기 버튼과 하드웨어 뒤로가기가 공유하는 처리.</summary>
        public void HandleBackPressed()
        {
            if (_sub == SubScreen.Entry)
            {
                ExitRequested?.Invoke();
                return;
            }
            SetSub(SubScreen.Entry);
            BackFromSubScreenRequested?.Invoke();
        }

        /// <summary>화면을 진입 하위 화면 상태로 연다.</summary>
        public void OpenEntry()
        {
            SetSub(SubScreen.Entry);
            SetVisible(true);
        }

        /// <summary>방 만들기 시작 — 코드 발급을 기다리는 중.</summary>
        public void ShowCreating()
        {
            SetSub(SubScreen.Create);
            _codeGroup.SetActive(false);
            _createStatusText.color = UiTheme.Label;
            _createStatusText.text = "코드를 만드는 중...";
        }

        /// <summary>코드 발급 성공 — 상대 입장을 기다린다.</summary>
        public void ShowCreatedCode(string code)
        {
            _codeGroup.SetActive(true);
            _codeText.text = code;
            _createStatusText.color = UiTheme.Label;
            _createStatusText.text = "이 코드를 상대에게 알려주세요.\n상대방의 입장을 기다리는 중...";
        }

        public void ShowCreateError(string message)
        {
            _codeGroup.SetActive(false);
            _createStatusText.color = UiTheme.LoseText;
            _createStatusText.text = message;
        }

        /// <summary>참가 확인 중(코드 검증+입장 시도) — 버튼을 잠가 중복 입력을 막는다.</summary>
        public void SetJoinBusy(bool busy)
        {
            if (_joinButton != null) _joinButton.interactable = !busy;
        }

        public void ShowJoinWaiting()
        {
            _joinErrorText.color = UiTheme.Label;
            _joinErrorText.text = "입장했습니다. 대전 시작을 기다리는 중...";
        }

        public void ShowJoinError(string message)
        {
            _joinErrorText.color = UiTheme.LoseText;
            _joinErrorText.text = message;
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
        }
    }
}
