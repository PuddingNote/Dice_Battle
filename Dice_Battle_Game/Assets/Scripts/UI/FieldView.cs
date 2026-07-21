using System;
using UnityEngine;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>한 플레이어의 필드(3라인).</summary>
    public sealed class FieldView
    {
        public PlayerId Owner { get; }
        private readonly LineView[] _lines = new LineView[Field.LineCount];

        public event Action<PlayerId, int> LineClicked;

        /// <param name="scoreInnerLeft">true면 각 라인의 점수를 왼쪽(내측)에 둔다(우측 필드용).</param>
        public FieldView(Transform parent, PlayerId owner, Color panelColor, string title, bool scoreInnerLeft)
        {
            Owner = owner;

            var panel = UiFactory.CreatePanel($"Field_{owner}", parent, panelColor);
            UiSkin.Apply(panel, UiSkin.FieldPanel, panelColor); // 스킨 필드 프레임(있으면), 색은 틴트
            var layout = UiFactory.AddVerticalLayout(panel.gameObject, 12, new RectOffset(16, 16, 20, 20),
                TextAnchor.UpperCenter);
            layout.childForceExpandHeight = false; // 행은 자연 높이로 상단 정렬
            UiFactory.SetPreferredWidth(panel.gameObject, UiTheme.FieldWidth);

            var header = UiFactory.CreateText("Title", panel.transform, title, UiTheme.ScoreFontSize, UiTheme.LabelDim);
            UiFactory.SetSize(header.gameObject, 200f, 56f);

            for (int i = 0; i < _lines.Length; i++)
            {
                _lines[i] = new LineView(panel.transform, owner, i, scoreFirst: scoreInnerLeft);
                _lines[i].Clicked += (f, idx) => LineClicked?.Invoke(f, idx);
            }
        }

        public LineView Line(int i) => _lines[i];

        public void Render(GameState state)
        {
            var field = state.Field(Owner);
            for (int i = 0; i < _lines.Length; i++)
                _lines[i].Render(field[i]);
        }

        public void ClearHighlights()
        {
            for (int i = 0; i < _lines.Length; i++)
                _lines[i].ClearHighlight();
        }
    }
}
