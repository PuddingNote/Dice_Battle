using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        private readonly TMP_Text _label;   // 숫자(스프라이트 없을 때 폴백)
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

        /// <summary>이 칸이 클릭을 받도록 할지(리롤 선택 시 트레이 주사위용).</summary>
        public void SetRaycast(bool on)
        {
            _bg.raycastTarget = on;
        }

        public void SetEmpty()
        {
            // 빈 칸은 슬롯 UI 없이 완전히 투명(주사위만 보이도록).
            UiSkin.Apply(_bg, null, Color.clear);
            _face.enabled = false;
            _label.text = "";
            _outline.enabled = false;
            Rect.localScale = Vector3.one;
        }

        /// <param name="side">주사위 세트(놓은 사람 기준: 플레이어=초록 / AI=빨강).</param>
        public void SetDie(int value, bool isSpecial, DiceSide side)
        {
            Sprite face = UiSkin.Face(side, value);
            if (face != null)
            {
                // 주사위 눈 스프라이트가 있으면: 칸 배경은 투명(라인 박스가 비침) + 주사위 스프라이트 표시.
                UiSkin.Apply(_bg, null, Color.clear);
                _face.sprite = face;
                _face.enabled = true;
                _face.color = isSpecial ? UiTheme.CellSpecial : Color.white; // 특수는 금색 틴트
                _label.text = "";
                _outline.enabled = false;
            }
            else
            {
                // 폴백: 단색 칸 + 숫자.
                Sprite cellSprite = isSpecial ? UiSkin.CellSpecial : UiSkin.CellFilled;
                Color cellColor = isSpecial ? UiTheme.CellSpecial : UiTheme.CellFilled;
                UiSkin.Apply(_bg, cellSprite, cellColor);
                _face.enabled = false;
                _label.text = value.ToString();
                _label.color = isSpecial ? UiTheme.DiceTextOnSpecial : UiTheme.DiceText;
                _outline.enabled = isSpecial && UiSkin.CellSpecial == null;
            }

            Rect.localScale = Vector3.one;
        }
    }
}
