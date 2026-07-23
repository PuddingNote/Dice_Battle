using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 보드 전체 뷰(가로형).
    /// 상단: 난이도/상태 / 중앙: 3줄(내 라인 [점수 ◀▶ 점수] 상대 라인) / 하단: 가로 주사위 트레이.
    /// 런타임에 코드로 UI를 구성한다.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        private PlayerId _humanId;
        private PlayerId _aiId;

        private readonly MatchRowView[] _rows = new MatchRowView[Field.LineCount];
        private TrayView _tray;
        private Text _levelText;
        private Text _statusText;

        private GameObject _root;
        private GameObject _resultOverlay;
        private Text _resultText;
        private Button _restartButton;
        private Button _menuButton;

        public event Action<PlayerId, int> LineClicked;
        public event Action RestartClicked;
        public event Action MenuClicked;

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
        }

        public void Build(RectTransform root, PlayerId humanId)
        {
            _humanId = humanId;
            _aiId = humanId.Other();

            var bg = UiFactory.CreateStretchPanel("BoardRoot", root, UiTheme.Background);
            UiSkin.Apply(bg, UiSkin.ScreenBackground, UiTheme.Background);
            _root = bg.gameObject;
            UiFactory.AddVerticalLayout(bg.gameObject, 12, new RectOffset(24, 24, 12, 12));

            BuildTopBar(bg.transform);
            BuildRows(bg.transform);
            BuildTrayArea(bg.transform);
            BuildResultOverlay(bg.rectTransform);
        }

        private void BuildTopBar(Transform parent)
        {
            var top = UiFactory.CreateRect("TopBar", parent);
            UiFactory.AddHorizontalLayout(top.gameObject, 16, new RectOffset(12, 12, 0, 0));
            UiFactory.SetPreferredHeight(top.gameObject, 96f);

            _levelText = UiFactory.CreateText("Level", top.transform, "",
                UiTheme.ScoreFontSize, UiTheme.WinText, TextAnchor.MiddleLeft);
            UiFactory.SetSize(_levelText.gameObject, 320f, 92f);

            _statusText = UiFactory.CreateText("Status", top.transform, "",
                36, UiTheme.Label, TextAnchor.MiddleRight);
            _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.SetFlexible(_statusText.gameObject);
        }

        private void BuildRows(Transform parent)
        {
            var rows = UiFactory.CreateRect("Rows", parent);
            var v = UiFactory.AddVerticalLayout(rows.gameObject, 14, new RectOffset(0, 0, 0, 0));
            v.childForceExpandHeight = true;
            UiFactory.SetFlexible(rows.gameObject);

            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i] = new MatchRowView(rows.transform, i, _humanId, _aiId);
                _rows[i].Clicked += (f, idx) => LineClicked?.Invoke(f, idx);
            }
        }

        private void BuildTrayArea(Transform parent)
        {
            var trayRow = UiFactory.CreateRect("TrayRow", parent);
            UiFactory.AddHorizontalLayout(trayRow.gameObject, 0, new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(trayRow.gameObject, UiTheme.TrayHeight + 16f);
            _tray = new TrayView(trayRow.transform, this);
        }

        private void BuildResultOverlay(RectTransform root)
        {
            var overlay = UiFactory.CreateStretchPanel("ResultOverlay", root, UiTheme.Overlay);
            UiFactory.IgnoreLayout(overlay.gameObject);
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.AddVerticalLayout(overlay.gameObject, 40, new RectOffset(60, 60, 120, 120));
            _resultOverlay = overlay.gameObject;

            _resultText = UiFactory.CreateText("ResultText", overlay.transform, "",
                56, UiTheme.Label, TextAnchor.MiddleCenter);
            _resultText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.SetSize(_resultText.gameObject, 900f, 640f);

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
            for (int i = 0; i < _rows.Length; i++)
                _rows[i].Render(state);

            _tray.ShowPending(state.PendingDice, state.CurrentPlayer == _humanId);
        }

        public void SetStatus(string message) => _statusText.text = message;

        public void SetLevelInfo(int level) => _levelText.text = $"AI 난이도  Lv{level}";

        // ---- 하이라이트 ----

        public void ClearHighlights()
        {
            for (int i = 0; i < _rows.Length; i++)
                _rows[i].ClearHighlights();
        }

        public void HighlightPrimary(GameState state)
        {
            ClearHighlights();
            for (int i = 0; i < _rows.Length; i++)
                if (state.Field(_humanId)[i].HasSpace)
                    _rows[i].SetSelectable(_humanId, true);
        }

        public void HighlightExtra(GameState state)
        {
            ClearHighlights();
            for (int i = 0; i < _rows.Length; i++)
            {
                if (state.Field(_humanId)[i].HasSpace) _rows[i].SetSelectable(_humanId, true);
                if (state.Field(_aiId)[i].HasSpace) _rows[i].SetSelectable(_aiId, true);
            }
        }

        // ---- 연출 ----

        public void PlayPlace(PlayerId field, int line, int cellIndex)
        {
            StartCoroutine(PopCell(_rows[line].Cell(field, cellIndex)));
        }

        public void PlayRemoval(PlayerId field, int line)
        {
            StartCoroutine(FlashImage(_rows[line].Background(field), _rows[line].BaseColor(field)));
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

        private IEnumerator FlashImage(Image img, Color to)
        {
            Color from = new Color(0.9f, 0.3f, 0.3f, 0.7f);
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
