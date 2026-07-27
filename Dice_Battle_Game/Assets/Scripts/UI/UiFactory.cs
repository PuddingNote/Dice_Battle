using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TextCore.LowLevel;
using TMPro;

namespace DiceBattle.UI
{
    /// <summary>코드로 uGUI 요소를 생성하는 헬퍼(런타임 UI 구성용).</summary>
    public static class UiFactory
    {
        private static TMP_FontAsset _fontAsset;

        /// <summary>
        /// 모든 텍스트가 쓰는 TMP 폰트 에셋.
        /// 1) 스킨의 TMP 폰트 에셋(권장) → 2) 스킨의 TTF로 런타임 동적 생성 → 3) TMP 기본 폰트.
        /// 동적(Dynamic) 모드라 한글 글리프를 필요할 때 아틀라스에 구워서 □□□이 나오지 않는다.
        /// </summary>
        public static TMP_FontAsset GetFontAsset()
        {
            if (_fontAsset != null) return _fontAsset;

            var skinAsset = UiSkin.ActiveFontAsset;
            if (skinAsset != null) return _fontAsset = skinAsset;

            var ttf = UiSkin.ActiveFont;
            if (ttf != null)
            {
                _fontAsset = TMP_FontAsset.CreateFontAsset(
                    ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
                if (_fontAsset != null)
                {
                    _fontAsset.name = ttf.name + " (Runtime SDF)";
                    return _fontAsset;
                }
            }

            return _fontAsset = TMP_Settings.defaultFontAsset;
        }

        /// <summary>TextAnchor(구 Text) → TMP 정렬로 변환.</summary>
        public static TextAlignmentOptions ToTmpAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        /// <summary>줄바꿈 허용 여부(구 horizontalOverflow 대체).</summary>
        public static void SetWrap(TMP_Text text, bool wrap)
        {
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
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

        public static TextMeshProUGUI CreateText(string name, Transform parent, string content, int fontSize,
            Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var rt = CreateRect(name, parent);
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var fontAsset = GetFontAsset();
            if (fontAsset != null) text.font = fontAsset;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = ToTmpAlignment(anchor);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(string name, Transform parent, Color color)
        {
            var img = CreatePanel(name, parent, color);
            UiSkin.Apply(img, UiSkin.Button, color); // 스킨 버튼 스프라이트(있으면)
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

        /// <summary>부모의 레이아웃 그룹 제어에서 제외한다(전체 화면 오버레이 등).</summary>
        public static LayoutElement IgnoreLayout(GameObject go)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
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
