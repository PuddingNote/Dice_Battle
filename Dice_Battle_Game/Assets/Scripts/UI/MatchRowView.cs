using System;
using UnityEngine;
using UnityEngine.UI;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 한 라인을 좌우로 마주보게 표시하는 행:
    ///   [내 3칸] [내 점수] [◀/▶] [상대 점수] [상대 3칸]
    /// 내/상대 칸 묶음이 각각 탭 버튼이며, 클릭 시 (필드, 라인인덱스)를 알린다.
    /// </summary>
    public sealed class MatchRowView
    {
        public int Index { get; }
        public event Action<PlayerId, int> Clicked;

        private readonly PlayerId _me;
        private readonly PlayerId _opp;

        private readonly CellView[] _myCells = new CellView[Line.Capacity];
        private readonly CellView[] _oppCells = new CellView[Line.Capacity];
        private readonly Button _myButton;
        private readonly Button _oppButton;
        private readonly Image _myBg;
        private readonly Image _oppBg;
        private readonly Text _myScore;
        private readonly Text _oppScore;
        private readonly Text _arrow;

        public MatchRowView(Transform parent, int index, PlayerId me, PlayerId opp)
        {
            Index = index;
            _me = me;
            _opp = opp;

            var row = UiFactory.CreateRect($"Row_{index}", parent);
            UiFactory.AddHorizontalLayout(row.gameObject, 12, new RectOffset(20, 20, 6, 6));

            // 내 칸 묶음(좌측): 안쪽(오른쪽=중앙)부터 index 0 이 되도록 역순 생성.
            _myButton = UiFactory.CreateButton($"MyGroup_{index}", row.transform, UiTheme.LineNormal);
            _myBg = _myButton.GetComponent<Image>();
            UiFactory.SetSize(_myButton.gameObject, UiTheme.LineBoxWidth, UiTheme.LineBoxHeight);
            UiFactory.AddHorizontalLayout(_myButton.gameObject, (int)UiTheme.CellSpacing, new RectOffset(0, 0, 0, 0));
            for (int i = Line.Capacity - 1; i >= 0; i--) _myCells[i] = new CellView(_myButton.transform);
            for (int i = 0; i < _myCells.Length; i++) _myCells[i].SetSide(DiceSide.Player);
            _myButton.onClick.AddListener(() => Clicked?.Invoke(_me, Index));

            // 중앙: 내 점수 ◀/▶ 상대 점수
            _myScore = UiFactory.CreateText($"MyScore_{index}", row.transform, "0",
                UiTheme.ScoreFontSize, UiTheme.Label);
            UiFactory.SetSize(_myScore.gameObject, 90f, UiTheme.CellSize);

            _arrow = UiFactory.CreateText($"Arrow_{index}", row.transform, "=",
                UiTheme.StatusFontSize, UiTheme.LabelDim);
            UiFactory.SetSize(_arrow.gameObject, 70f, UiTheme.CellSize);

            _oppScore = UiFactory.CreateText($"OppScore_{index}", row.transform, "0",
                UiTheme.ScoreFontSize, UiTheme.Label);
            UiFactory.SetSize(_oppScore.gameObject, 90f, UiTheme.CellSize);

            // 상대 칸 묶음(우측): 안쪽(왼쪽=중앙)부터 index 0.
            _oppButton = UiFactory.CreateButton($"OppGroup_{index}", row.transform, UiTheme.LineNormal);
            _oppBg = _oppButton.GetComponent<Image>();
            UiFactory.SetSize(_oppButton.gameObject, UiTheme.LineBoxWidth, UiTheme.LineBoxHeight);
            UiFactory.AddHorizontalLayout(_oppButton.gameObject, (int)UiTheme.CellSpacing, new RectOffset(0, 0, 0, 0));
            for (int i = 0; i < Line.Capacity; i++) _oppCells[i] = new CellView(_oppButton.transform);
            for (int i = 0; i < _oppCells.Length; i++) _oppCells[i].SetSide(DiceSide.Ai);
            _oppButton.onClick.AddListener(() => Clicked?.Invoke(_opp, Index));

            ClearHighlights();
        }

        public void Render(GameState state)
        {
            RenderSide(_myCells, state.Field(_me)[Index], _myScore);
            RenderSide(_oppCells, state.Field(_opp)[Index], _oppScore);
            UpdateArrow(state.Field(_me)[Index].Score(), state.Field(_opp)[Index].Score());
        }

        private static void RenderSide(CellView[] cells, Line line, Text scoreText)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (i < line.Count)
                {
                    var d = line.Dice[i];
                    cells[i].SetDie(d.Value, d.IsSpecial);
                }
                else
                {
                    cells[i].SetEmpty();
                }
            }
            scoreText.text = line.Score().ToString();
        }

        private void UpdateArrow(int myScore, int oppScore)
        {
            if (myScore > oppScore)
            {
                _arrow.text = "◀"; // ◀
                _arrow.color = UiTheme.WinText;
                _myScore.color = UiTheme.WinText;
                _oppScore.color = UiTheme.LabelDim;
            }
            else if (oppScore > myScore)
            {
                _arrow.text = "▶"; // ▶
                _arrow.color = UiTheme.LoseText;
                _myScore.color = UiTheme.LabelDim;
                _oppScore.color = UiTheme.LoseText;
            }
            else
            {
                _arrow.text = "=";
                _arrow.color = UiTheme.LabelDim;
                _myScore.color = UiTheme.Label;
                _oppScore.color = UiTheme.Label;
            }
        }

        public CellView Cell(PlayerId field, int i) => field == _me ? _myCells[i] : _oppCells[i];
        public Image Background(PlayerId field) => field == _me ? _myBg : _oppBg;

        /// <summary>
        /// 라인 박스 기본 틴트. 박스 스프라이트가 있으면 흰색(원본색 유지),
        /// 없으면(프로토타입) 편 구분 색(내=푸름, 상대=붉음).
        /// </summary>
        public Color BaseColor(PlayerId field)
            => UiSkin.LineNormal != null ? Color.white : (field == _me ? UiTheme.MyLine : UiTheme.OppLine);

        public void SetSelectable(PlayerId field, bool on)
        {
            var btn = field == _me ? _myButton : _oppButton;
            var bg = field == _me ? _myBg : _oppBg;
            btn.interactable = on;
            // 라인 박스 스프라이트는 두 상태 동일하게 쓰고 색(틴트)만 변경.
            Color highlight = UiSkin.LineNormal != null ? UiTheme.LineHighlightSolid : UiTheme.LineHighlight;
            UiSkin.Apply(bg, UiSkin.LineNormal, on ? highlight : BaseColor(field));
        }

        public void ClearHighlights()
        {
            SetSelectable(_me, false);
            SetSelectable(_opp, false);
        }
    }
}
