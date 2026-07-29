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

        /// <summary>
        /// 버튼 생성. 기본적으로 클릭음이 자동으로 붙는다.
        /// 라인 박스처럼 다른 소리를 내야 하는 버튼은 clickSound=false 로 만든다.
        /// </summary>
        public static Button CreateButton(string name, Transform parent, Color color, bool clickSound = true)
        {
            var img = CreatePanel(name, parent, color);
            UiSkin.Apply(img, UiSkin.Button, color); // 스킨 버튼 스프라이트(있으면)
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (clickSound) btn.onClick.AddListener(AudioManager.PlayButton);
            return btn;
        }

        /// <summary>
        /// 아이콘이 들어가는 정사각형 버튼. 스킨 아이콘이 없으면 대체 문구를 대신 보여준다.
        /// (리롤/설정/페이지 넘김 버튼이 같은 모양을 쓴다.)
        /// </summary>
        public static IconButton CreateIconButton(string name, Transform parent, Sprite icon,
            string fallbackText, float size, float inset, int fallbackFontSize)
        {
            var button = CreateButton(name, parent, UiTheme.Button);
            var rt = button.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            var iconImage = CreatePanel("Icon", button.transform, Color.white);
            iconImage.raycastTarget = false;
            Stretch(iconImage.rectTransform);
            iconImage.rectTransform.offsetMin = new Vector2(inset, inset);
            iconImage.rectTransform.offsetMax = new Vector2(-inset, -inset);

            var label = CreateText("Label", button.transform, fallbackText, fallbackFontSize, Color.white);
            Stretch(label.rectTransform);

            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true;
                label.gameObject.SetActive(false);
            }
            else
            {
                iconImage.enabled = false; // 아이콘이 없으면 문구만 보인다
            }

            return new IconButton(button, iconImage, label);
        }

        /// <summary>
        /// 화면 가운데에 뜨는 창(모달) 패널을 만들고, 내용을 넣을 본체를 돌려준다.
        /// 스킨에 창 이미지가 있으면 그걸 쓰고, 없으면 코드로 만든 둥근 사각형 2겹(테두리+본체)을 쓴다.
        /// </summary>
        public static RectTransform CreateWindow(string name, Transform parent, float width, float height)
        {
            var outer = CreatePanel(name, parent, UiTheme.WindowBorder);
            var ort = outer.rectTransform;
            ort.anchorMin = new Vector2(0.5f, 0.5f);
            ort.anchorMax = new Vector2(0.5f, 0.5f);
            ort.pivot = new Vector2(0.5f, 0.5f);
            ort.sizeDelta = new Vector2(width, height);
            ort.anchoredPosition = Vector2.zero;

            if (UiSkin.WindowPanel != null)
            {
                UiSkin.Apply(outer, UiSkin.WindowPanel, UiTheme.WindowPanel);
                return ort;
            }

            ApplyRounded(outer, null, UiTheme.WindowBorder);

            var body = CreatePanel("Body", outer.transform, UiTheme.WindowPanel);
            ApplyRounded(body, null, UiTheme.WindowPanel);
            Stretch(body.rectTransform);
            body.rectTransform.offsetMin = new Vector2(10f, 10f);
            body.rectTransform.offsetMax = new Vector2(-10f, -10f);
            return body.rectTransform;
        }

        /// <summary>
        /// 가로 볼륨 슬라이더. 스킨 이미지가 없으면 코드로 만든 둥근 트랙 + 원형 손잡이를 쓴다.
        /// 값 범위는 0~1이다.
        /// </summary>
        public static Slider CreateSlider(string name, Transform parent)
        {
            float track = UiTheme.SliderTrackHeight;
            float handle = UiTheme.SliderHandleSize;

            var root = CreateRect(name, parent);
            var slider = root.gameObject.AddComponent<Slider>();

            // 트랙이 얇아 손가락으로 집기 어려우므로, 슬라이더 영역 전체를 잡을 수 있게 투명 판을 깐다.
            var hitArea = root.gameObject.AddComponent<Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0f);

            var background = CreatePanel("Background", root, UiTheme.SliderTrack);
            ApplyRounded(background, UiSkin.SliderTrack, UiTheme.SliderTrack);
            StretchHorizontalBand(background.rectTransform, 0f, track);

            // 채워지는 구간. 손잡이가 양 끝에서 트랙 밖으로 나가지 않도록 좌우를 손잡이 반지름만큼 줄인다.
            var fillArea = CreateRect("Fill Area", root);
            StretchHorizontalBand(fillArea, -handle, track);
            var fill = CreatePanel("Fill", fillArea, UiTheme.SliderFill);
            ApplyRounded(fill, UiSkin.SliderFill, UiTheme.SliderFill);
            fill.raycastTarget = false;
            Stretch(fill.rectTransform);

            var handleArea = CreateRect("Handle Slide Area", root);
            StretchHorizontalBand(handleArea, -handle, 0f);
            var handleImage = CreatePanel("Handle", handleArea, UiTheme.SliderHandle);
            if (UiSkin.SliderHandle != null)
            {
                UiSkin.Apply(handleImage, UiSkin.SliderHandle, UiTheme.SliderHandle);
            }
            else
            {
                handleImage.sprite = UiSprites.Circle;
                handleImage.type = Image.Type.Simple;
                handleImage.color = UiTheme.SliderHandle;
            }
            var hrt = handleImage.rectTransform;
            hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.sizeDelta = new Vector2(handle, handle);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = hrt;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            return slider;
        }

        /// <summary>부모 폭을 따라 늘어나되 세로로는 가운데 정렬된 띠 모양으로 배치한다.</summary>
        private static void StretchHorizontalBand(RectTransform rt, float widthDelta, float height)
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(widthDelta, height);
            rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 이미지에 둥근 모서리를 입힌다.
        /// 스킨 스프라이트가 있으면 그걸 쓰고(원본색), 없으면 코드 생성 스프라이트에 지정 색을 입힌다.
        /// </summary>
        public static void ApplyRounded(Image img, Sprite skin, Color color)
        {
            if (img == null) return;
            if (skin != null)
            {
                img.sprite = skin;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                return;
            }
            img.sprite = UiSprites.RoundedRect;
            img.type = Image.Type.Sliced;
            img.color = color;
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
