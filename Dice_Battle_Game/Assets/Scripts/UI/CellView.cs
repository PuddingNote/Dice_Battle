using UnityEngine;
using UnityEngine.UI;

namespace DiceBattle.UI
{
    /// <summary>라인의 한 칸(주사위 한 개 슬롯). 값/특수 여부를 시각화한다.</summary>
    public sealed class CellView
    {
        public RectTransform Rect { get; }
        private readonly Image _bg;
        private readonly Text _label;
        private readonly Outline _outline;

        public CellView(Transform parent)
        {
            _bg = UiFactory.CreatePanel("Cell", parent, UiTheme.CellEmpty);
            _bg.raycastTarget = false;
            Rect = _bg.rectTransform;
            UiFactory.SetSize(_bg.gameObject, UiTheme.CellSize, UiTheme.CellSize);

            _outline = _bg.gameObject.AddComponent<Outline>();
            _outline.effectColor = UiTheme.CellSpecialOutline;
            _outline.effectDistance = new Vector2(4f, -4f);
            _outline.enabled = false;

            _label = UiFactory.CreateText("Value", _bg.transform, "", UiTheme.DiceFontSize, UiTheme.DiceText);
            UiFactory.Stretch(_label.rectTransform);
        }

        public void SetEmpty()
        {
            _bg.color = UiTheme.CellEmpty;
            _label.text = "";
            _outline.enabled = false;
            Rect.localScale = Vector3.one;
        }

        public void SetDie(int value, bool isSpecial)
        {
            _bg.color = isSpecial ? UiTheme.CellSpecial : UiTheme.CellFilled;
            _label.text = value.ToString();
            _label.color = isSpecial ? UiTheme.DiceTextOnSpecial : UiTheme.DiceText;
            _outline.enabled = isSpecial;
            Rect.localScale = Vector3.one;
        }
    }
}
