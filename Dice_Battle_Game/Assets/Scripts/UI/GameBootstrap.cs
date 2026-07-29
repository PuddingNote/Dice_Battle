using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace DiceBattle.UI
{
    /// <summary>
    /// 런타임 진입점. 빈 씬에 이 컴포넌트를 가진 GameObject 하나만 두고 Play 하면
    /// Canvas/EventSystem/GameManager(메뉴↔게임)를 코드로 생성한다.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Tooltip("선택: UI 스킨 에셋. 비우면 기본 단색 UI로 동작한다.")]
        [SerializeField] private UiSkin skin;

        [Tooltip("선택: 난이도(등급별 AI 수치) 에셋. 비우면 코드 기본값 사용.")]
        [SerializeField] private DifficultyConfig difficulty;

        [Tooltip("선택: 사운드 모음 에셋. 비우면 소리 없이 동작한다.")]
        [SerializeField] private SoundBank sounds;

        private void Start()
        {
            UiSkin.Active = skin; // 스킨 지정 시 스프라이트 적용, null이면 단색 폴백

            // UI보다 먼저 만들어야 버튼 생성 시점부터 클릭음이 붙는다.
            AudioManager.Create(transform, sounds);

            Canvas canvas = CreateCanvas();
            EnsureEventSystem();
            var canvasRect = canvas.GetComponent<RectTransform>();

            // 비율이 다른 화면에서 남는 영역을 채울 검은 배경(레터박스/필러박스).
            var bars = UiFactory.CreateStretchPanel("Letterbox", canvasRect, Color.black);
            bars.raycastTarget = false;

            // 고정 1920x1080 컨텐츠(가운데). LetterboxScaler가 화면에 맞춰 균등 스케일.
            RectTransform content = CreateContent(canvasRect);

            var gmGo = new GameObject("GameManager");
            gmGo.transform.SetParent(transform, false);
            var gm = gmGo.AddComponent<GameManager>();
            gm.Init(content, difficulty);

            // 게임 UI를 만든 뒤 맨 위에 덮어, 1920x1080 밖으로 나간 연출(주사위 낙하/알까기 등)이
            // 레터박스 영역에 보이지 않게 가린다.
            CreateEdgeMasks(content);
        }

        /// <summary>
        /// 컨텐츠(1920x1080) 바깥 상/하/좌/우를 덮는 검은 판.
        /// Content의 자식이라 LetterboxScaler와 같은 배율로 스케일되고,
        /// 마지막 자식이라 게임 UI보다 위에 그려진다.
        /// </summary>
        private static void CreateEdgeMasks(RectTransform content)
        {
            const float thickness = 3000f; // 어떤 화면 비율에서도 남는 영역을 덮을 만큼 크게

            // 아래: 컨텐츠 하단선에 위쪽 끝을 붙이고 아래로 뻗는다.
            CreateEdgeMask("MaskBottom", content,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f), thickness);
            // 위
            CreateEdgeMask("MaskTop", content,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0f), thickness);
            // 좌
            CreateEdgeMask("MaskLeft", content,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 0.5f), thickness);
            // 우
            CreateEdgeMask("MaskRight", content,
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), thickness);
        }

        private static void CreateEdgeMask(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, float thickness)
        {
            var img = UiFactory.CreatePanel(name, parent, Color.black);
            img.raycastTarget = false;

            var rt = img.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            // 늘어나는 축에서는 컨텐츠 너비/높이에 더해지는 여유분, 고정 축에서는 두께가 된다.
            rt.sizeDelta = new Vector2(thickness, thickness);
            rt.anchoredPosition = Vector2.zero;
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // 실제 화면 픽셀 기준. 스케일은 LetterboxScaler가 컨텐츠 단위로 직접 처리.
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>화면 중앙에 고정 1920x1080 컨텐츠 루트를 만든다(균등 스케일용).</summary>
        private static RectTransform CreateContent(RectTransform parent)
        {
            var go = new GameObject("Content", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(UiTheme.ReferenceWidth, UiTheme.ReferenceHeight);
            rt.anchoredPosition = Vector2.zero;
            go.AddComponent<LetterboxScaler>();
            return rt;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            // 새 Input System 기반 UI 입력 모듈(기본 액션 자동 할당).
            var module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }
    }
}
