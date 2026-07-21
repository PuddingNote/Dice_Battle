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

            var gmGo = new GameObject("GameManager");
            gmGo.transform.SetParent(transform, false);
            var gm = gmGo.AddComponent<GameManager>();
            gm.Init(canvas.GetComponent<RectTransform>());
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(UiTheme.ReferenceWidth, UiTheme.ReferenceHeight);
            // 가로형: 세로(높이) 기준으로 스케일해 3행 필드가 항상 들어오게 한다.
            scaler.matchWidthOrHeight = 1f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
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
