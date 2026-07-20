using UnityEngine;
using UnityEngine.UI;

namespace DiceBattle.UI
{
    /// <summary>코드로 uGUI 요소를 생성하는 헬퍼(런타임 UI 구성용).</summary>
    public static class UiFactory
    {
        private static Font _font;

        /// <summary>Unity 6 내장 런타임 폰트(구 Arial 대체).</summary>
        public static Font GetFont()
        {
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            var rt = CreateRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        /// <summary>부모를 가득 채우는(stretch) 패널.</summary>
        public static Image CreateStretchPanel(string name, Transform parent, Color color)
        {
            var img = CreatePanel(name, parent, color);
            Stretch(img.rectTransform);
            return img;
        }

        public static Text CreateText(string name, Transform parent, string content, int fontSize,
            Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var rt = CreateRect(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = GetFont();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(string name, Transform parent, Color color)
        {
            var img = CreatePanel(name, parent, color);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        /// <summary>RectTransform을 부모에 꽉 채운다.</summary>
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static VerticalLayoutGroup AddVerticalLayout(GameObject go, int spacing, RectOffset padding,
            TextAnchor align = TextAnchor.MiddleCenter)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding;
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = true;
            return v;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(GameObject go, int spacing, RectOffset padding,
            TextAnchor align = TextAnchor.MiddleCenter)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = padding;
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return h;
        }

        public static LayoutElement SetSize(GameObject go, float width, float height)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            le.minWidth = width;
            le.minHeight = height;
            return le;
        }

        public static LayoutElement SetFlexibleHeight(GameObject go, float flex)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.flexibleHeight = flex;
            return le;
        }

        /// <summary>세로 높이는 고정, 가로는 부모를 따라 늘어나게 한다.</summary>
        public static LayoutElement SetPreferredHeight(GameObject go, float height)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;
            return le;
        }

        /// <summary>가로 너비는 고정, 세로는 부모를 따라 늘어나게 한다(가로형 열).</summary>
        public static LayoutElement SetPreferredWidth(GameObject go, float width)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.minWidth = width;
            le.preferredWidth = width;
            le.flexibleHeight = 1f;
            return le;
        }

        /// <summary>가로/세로 모두 부모를 따라 늘어나게 한다(중앙 유연 열).</summary>
        public static LayoutElement SetFlexible(GameObject go)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.flexibleHeight = 1f;
            return le;
        }
    }
}
