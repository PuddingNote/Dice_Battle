using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 보드 전체 뷰(마주보는 3x3 필드 2개 + 획득 주사위 + 상태 텍스트 + 결과 오버레이).
    /// 런타임에 코드로 UI를 구성한다.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        private FieldView _leftField;   // 플레이어(사람) — 좌측
        private FieldView _rightField;  // 상대(AI) — 우측
        private PlayerId _humanId;
        private PlayerId _aiId;

        private GameObject _root;
        private CellView _handCell;
        private Text _handLabel;
        private Text _statusText;
        private Text _levelText;

        private GameObject _resultOverlay;
        private Text _resultText;
        private Button _restartButton;
        private Button _menuButton;

        /// <summary>라인 탭: (필드, 라인인덱스).</summary>
        public event Action<PlayerId, int> LineClicked;
        public event Action RestartClicked;
        public event Action MenuClicked;

        /// <summary>보드 전체 표시/숨김.</summary>
        public void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
        }

        public void Build(RectTransform root, PlayerId humanId)
        {
            _humanId = humanId;
            _aiId = humanId.Other();

            var bg = UiFactory.CreateStretchPanel("BoardRoot", root, UiTheme.Background);
            _root = bg.gameObject;
            // 가로형: [내 필드] [중앙(상태/획득 주사위)] [AI 필드]
            UiFactory.AddHorizontalLayout(bg.gameObject, 12, new RectOffset(28, 28, 24, 24));

            // 좌측: 내 필드 (점수는 우측=내측)
            _leftField = new FieldView(bg.transform, _humanId, UiTheme.PlayerPanel, "나", scoreInnerLeft: false);
            _leftField.LineClicked += (f, i) => LineClicked?.Invoke(f, i);

            // 중앙: 상태 + 획득 주사위
            BuildCenter(bg.transform);

            // 우측: AI 필드 (점수는 좌측=내측)
            _rightField = new FieldView(bg.transform, _aiId, UiTheme.OpponentPanel, "상대 (AI)", scoreInnerLeft: true);
            _rightField.LineClicked += (f, i) => LineClicked?.Invoke(f, i);

            BuildResultOverlay(bg.rectTransform);
        }

        private void BuildCenter(Transform parent)
        {
            var center = UiFactory.CreatePanel("Center", parent, UiTheme.CenterPanel);
            UiFactory.AddVerticalLayout(center.gameObject, 20, new RectOffset(20, 20, 40, 40));
            UiFactory.SetFlexible(center.gameObject);

            _levelText = UiFactory.CreateText("Level", center.transform, "",
                UiTheme.ScoreFontSize, UiTheme.WinText);
            UiFactory.SetSize(_levelText.gameObject, 260f, 56f);

            _statusText = UiFactory.CreateText("Status", center.transform, "",
                UiTheme.StatusFontSize, UiTheme.Label, TextAnchor.UpperCenter);
            UiFactory.SetFlexibleHeight(_statusText.gameObject, 1f);
            _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;

            _handLabel = UiFactory.CreateText("HandLabel", center.transform, "획득 주사위",
                UiTheme.ScoreFontSize, UiTheme.LabelDim);
            UiFactory.SetSize(_handLabel.gameObject, 260f, 48f);

            _handCell = new CellView(center.transform);
        }

        private void BuildResultOverlay(RectTransform root)
        {
            var overlay = UiFactory.CreateStretchPanel("ResultOverlay", root, UiTheme.Overlay);
            // 부모(BoardRoot)의 가로 레이아웃에 눌려 잘리지 않도록 레이아웃 제외 + 전체 화면.
            UiFactory.IgnoreLayout(overlay.gameObject);
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.AddVerticalLayout(overlay.gameObject, 40, new RectOffset(60, 60, 120, 120));
            _resultOverlay = overlay.gameObject;

            _resultText = UiFactory.CreateText("ResultText", overlay.transform, "",
                56, UiTheme.Label, TextAnchor.MiddleCenter);
            _resultText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.SetSize(_resultText.gameObject, 900f, 700f);

            var buttonRow = UiFactory.CreateRect("Buttons", overlay.transform);
            UiFactory.AddHorizontalLayout(buttonRow.gameObject, 40, new RectOffset(0, 0, 0, 0));
            UiFactory.SetSize(buttonRow.gameObject, 900f, 150f);

            _restartButton = UiFactory.CreateButton("RestartButton", buttonRow.transform, UiTheme.Button);
            UiFactory.SetSize(_restartButton.gameObject, 400f, 130f);
            var restartText = UiFactory.CreateText("Label", _restartButton.transform, "다시 하기",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(restartText.rectTransform);
            _restartButton.onClick.AddListener(() => RestartClicked?.Invoke());

            _menuButton = UiFactory.CreateButton("MenuButton", buttonRow.transform, UiTheme.CenterPanel);
            UiFactory.SetSize(_menuButton.gameObject, 400f, 130f);
            var menuText = UiFactory.CreateText("Label", _menuButton.transform, "메뉴로",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(menuText.rectTransform);
            _menuButton.onClick.AddListener(() => MenuClicked?.Invoke());

            _resultOverlay.SetActive(false);
        }

        // ---- 렌더링 ----

        public void Render(GameState state)
        {
            _leftField.Render(state);
            _rightField.Render(state);
            RenderHand(state);
        }

        private void RenderHand(GameState state)
        {
            var die = state.PendingDice;
            if (die == null)
                _handCell.SetEmpty();
            else
                _handCell.SetDie(die.Value, die.IsSpecial);
        }

        public void SetStatus(string message) => _statusText.text = message;

        public void SetLevelInfo(int level) => _levelText.text = $"AI 난이도  Lv{level}";

        // ---- 하이라이트 ----

        public void ClearHighlights()
        {
            _leftField.ClearHighlights();
            _rightField.ClearHighlights();
        }

        /// <summary>기본 배치: 사람 필드에서 빈칸 있는 라인만 선택 가능.</summary>
        public void HighlightPrimary(GameState state)
        {
            ClearHighlights();
            HighlightFieldSpaces(state, ViewOf(_humanId));
        }

        /// <summary>추가 배치: 양쪽 필드에서 빈칸 있는 라인 모두 선택 가능.</summary>
        public void HighlightExtra(GameState state)
        {
            ClearHighlights();
            HighlightFieldSpaces(state, _leftField);
            HighlightFieldSpaces(state, _rightField);
        }

        private void HighlightFieldSpaces(GameState state, FieldView view)
        {
            var field = state.Field(view.Owner);
            for (int i = 0; i < Field.LineCount; i++)
                if (field[i].HasSpace) view.Line(i).SetSelectable(true);
        }

        private FieldView ViewOf(PlayerId owner)
            => owner == _leftField.Owner ? _leftField : _rightField;

        // ---- 연출 ----

        public void PlayPlace(PlayerId field, int line, int cellIndex)
        {
            var cell = ViewOf(field).Line(line).Cell(cellIndex);
            StartCoroutine(PopCell(cell));
        }

        public void PlayRemoval(PlayerId field, int line)
        {
            StartCoroutine(FlashLine(ViewOf(field).Line(line)));
        }

        private IEnumerator PopCell(CellView cell)
        {
            float t = 0f;
            const float dur = 0.16f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float s = Mathf.Lerp(1.35f, 1f, t / dur);
                cell.Rect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            cell.Rect.localScale = Vector3.one;
        }

        private IEnumerator FlashLine(LineView lineView)
        {
            var img = lineView.Background;
            Color from = new Color(0.9f, 0.3f, 0.3f, 0.7f);
            Color to = UiTheme.LineNormal;
            float t = 0f;
            const float dur = 0.35f;
            while (t < dur)
            {
                t += Time.deltaTime;
                img.color = Color.Lerp(from, to, t / dur);
                yield return null;
            }
            img.color = to;
        }

        // ---- 결과 ----

        public void ShowResult(MatchOutcome outcome, string scoreLine)
        {
            string headline;
            if (outcome.IsDraw) headline = "무승부";
            else if (outcome.Winner == _humanId) headline = "승리!";
            else headline = "패배";

            string lines = "";
            for (int i = 0; i < outcome.Lines.Count; i++)
            {
                string r = outcome.Lines[i] == LineResult.Draw ? "무"
                    : (LineResultToPlayer(outcome.Lines[i]) == _humanId ? "승" : "패");
                lines += $"라인 {i + 1}: {r}\n";
            }

            _resultText.text = $"{headline}\n\n{lines}\n{scoreLine}";
            _resultOverlay.SetActive(true);
        }

        private static PlayerId LineResultToPlayer(LineResult r)
            => r == LineResult.PlayerOne ? PlayerId.One : PlayerId.Two;

        public void HideResult() => _resultOverlay.SetActive(false);
    }
}
