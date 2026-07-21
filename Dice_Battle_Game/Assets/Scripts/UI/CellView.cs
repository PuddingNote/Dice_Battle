using UnityEngine;
using UnityEngine.UI;

namespace DiceBattle.UI
{
    /// <summary>
    /// 라인의 한 칸(주사위 한 개 슬롯). 값/특수 여부를 시각화한다.
    /// 스킨에 스프라이트가 있으면 셀 배경/주사위 눈을 스프라이트로, 없으면 단색+숫자로 표시.
    /// </summary>
    public sealed class CellView
    {
        public RectTransform Rect { get; }
        private readonly Image _bg;
        private readonly Image _face;   // 주사위 눈 스프라이트(스킨 지정 시)
        private readonly Text _label;   // 숫자(스프라이트 없을 때 폴백)
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

            _face = UiFactory.CreatePanel("Face", _bg.transform, Color.white);
            _face.raycastTarget = false;
            UiFactory.Stretch(_face.rectTransform);
            _face.enabled = false;

            _label = UiFactory.CreateText("Value", _bg.transform, "", UiTheme.DiceFontSize, UiTheme.DiceText);
            UiFactory.Stretch(_label.rectTransform);
        }

        public void SetEmpty()
        {
            UiSkin.Apply(_bg, UiSkin.CellEmpty, UiTheme.CellEmpty);
            _face.enabled = false;
            _label.text = "";
            _outline.enabled = false;
            Rect.localScale = Vector3.one;
        }

        public void SetDie(int value, bool isSpecial)
        {
            Sprite cellSprite = isSpecial ? UiSkin.CellSpecial : UiSkin.CellFilled;
            Color cellColor = isSpecial ? UiTheme.CellSpecial : UiTheme.CellFilled;
            UiSkin.Apply(_bg, cellSprite, cellColor);

            Sprite face = UiSkin.Face(value);
            if (face != null)
            {
                _face.sprite = face;
                _face.enabled = true;
                _label.text = "";
            }
            else
            {
                _face.enabled = false;
                _label.text = value.ToString();
                _label.color = isSpecial ? UiTheme.DiceTextOnSpecial : UiTheme.DiceText;
            }

            // 특수 표시: 전용 스프라이트가 없을 때만 테두리로 강조.
            _outline.enabled = isSpecial && UiSkin.CellSpecial == null;
            Rect.localScale = Vector3.one;
        }
    }
}
