using UnityEngine;

namespace DiceBattle.UI
{
    /// <summary>
    /// 코드로 생성하는 기본 UI 스프라이트(둥근 사각형 / 원).
    /// 스킨 이미지가 아직 없어도 슬라이더·창 같은 요소가 각진 사각형으로 보이지 않게 한다.
    /// 나중에 UiSkin에 실제 이미지를 넣으면 그쪽이 우선한다.
    /// </summary>
    public static class UiSprites
    {
        private const int RoundedSize = 48;
        private const int RoundedRadius = 14;
        private const int CircleSize = 96;

        private static Sprite _rounded;
        private static Sprite _circle;

        /// <summary>9-슬라이스용 둥근 사각형(모서리가 늘어나지 않는다).</summary>
        public static Sprite RoundedRect
        {
            get
            {
                if (_rounded == null) _rounded = BuildRounded();
                return _rounded;
            }
        }

        /// <summary>슬라이더 손잡이 등에 쓰는 원.</summary>
        public static Sprite Circle
        {
            get
            {
                if (_circle == null) _circle = BuildCircle();
                return _circle;
            }
        }

        private static Sprite BuildRounded()
        {
            const int n = RoundedSize;
            const float r = RoundedRadius;

            var tex = NewTexture(n, n);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    // 모서리 원 중심으로부터의 거리로 알파를 만든다(가장자리 1px는 부드럽게).
                    float dx = Mathf.Max(r - (x + 0.5f), (x + 0.5f) - (n - r), 0f);
                    float dy = Mathf.Max(r - (y + 0.5f), (y + 0.5f) - (n - r), 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f));
                }
            }
            tex.SetPixels(px);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }

        private static Sprite BuildCircle()
        {
            const int n = CircleSize;
            const float r = n * 0.5f;

            var tex = NewTexture(n, n);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - r;
                    float dy = y + 0.5f - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f));
                }
            }
            tex.SetPixels(px);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Texture2D NewTexture(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, mipChain: false)
            {
                name = "UiSprites",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }
    }
}
