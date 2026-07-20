using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 런타임 진입점. 빈 씬에 이 컴포넌트를 가진 GameObject 하나만 두고 Play 하면
    /// Canvas/EventSystem/보드/컨트롤러를 코드로 생성해 대전이 시작된다.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Tooltip("체크 시 규칙기반(보통), 해제 시 랜덤(낮음) AI")]
        public bool useHeuristicAi = true;

        private void Start()
        {
            Canvas canvas = CreateCanvas();
            EnsureEventSystem();

            var boardGo = new GameObject("BoardView");
            boardGo.transform.SetParent(canvas.transform, false);
            var board = boardGo.AddComponent<BoardView>();
            board.Build(canvas.GetComponent<RectTransform>(), PlayerId.One);

            var controllerGo = new GameObject("GameController");
            controllerGo.transform.SetParent(transform, false);
            var controller = controllerGo.AddComponent<GameController>();
            controller.Init(board, useHeuristicAi);
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
