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

        private void Start()
        {
            UiSkin.Active = skin; // 스킨 지정 시 스프라이트 적용, null이면 단색 폴백

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
            gm.Init(content);
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
