using UnityEngine;
using UnityEngine.InputSystem;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 화면 흐름 관리: 메인 메뉴 ↔ 게임.
    /// 등급(점수)에 따라 난이도가 자동 결정되고, 매 판 결과로 점수가 갱신된다.
    /// (테스트용으로 메뉴에서 난이도 직접 선택도 가능)
    /// 모바일 뒤로가기 버튼(=Escape)도 여기서 처리한다.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        private const PlayerId Human = PlayerId.One;

        private MenuView _menu;
        private BoardView _board;
        private GameController _controller;
        private ConfirmDialogView _dialog;

        private bool _inMenu = true;

        public void Init(RectTransform canvasRoot, DifficultyConfig difficulty)
        {
            var boardGo = new GameObject("BoardView");
            boardGo.transform.SetParent(transform, false);
            _board = boardGo.AddComponent<BoardView>();
            _board.Build(canvasRoot, Human);

            var controllerGo = new GameObject("GameController");
            controllerGo.transform.SetParent(transform, false);
            _controller = controllerGo.AddComponent<GameController>();
            _controller.Init(_board, difficulty);
            _controller.MenuRequested += ShowMenu;
            _controller.MatchFinished += OnMatchFinished;

            // 메뉴는 보드 위에 오도록 나중에 생성.
            var menuGo = new GameObject("MenuView");
            menuGo.transform.SetParent(transform, false);
            _menu = menuGo.AddComponent<MenuView>();
            _menu.Build(canvasRoot);
            _menu.StartRequested += () => StartGame(PlayerProgress.Level); // 등급 자동 난이도
            _menu.LevelSelected += StartGame;                              // 테스트: 직접 선택

            // 뒤로가기 다이얼로그는 항상 최상단에 오도록 마지막에 생성.
            _dialog = new ConfirmDialogView(canvasRoot);

            ShowMenu();
        }

        private void ShowMenu()
        {
            _controller.AbortMatch(); // 진행 중이던 판/연출 중단
            _inMenu = true;
            _board.SetVisible(false);
            _menu.SetScore(PlayerProgress.Score, PlayerProgress.Level);
            _menu.SetVisible(true);
        }

        private void StartGame(int level)
        {
            _inMenu = false;
            _menu.SetVisible(false);
            _board.SetVisible(true);
            _controller.StartMatch(level);
        }

        // ---- 모바일 뒤로가기 ----

        private void Update()
        {
            if (BackPressed()) OnBackPressed();
        }

        /// <summary>안드로이드 뒤로가기 버튼은 Escape 키로 전달된다.</summary>
        private static bool BackPressed()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        }

        private void OnBackPressed()
        {
            // 이미 열려 있으면 뒤로가기는 '닫기'로 동작.
            if (_dialog.IsOpen)
            {
                _dialog.Close();
                return;
            }

            if (_inMenu)
            {
                _dialog.Open("게임을 종료할까요?", "뒤로가기", "게임 종료", QuitGame);
                return;
            }

            // 승부가 끝난 뒤(결과 화면)에는 "사라집니다" 경고가 맞지 않으므로 문구를 나눈다.
            string message = _controller.IsMatchActive
                ? "메인 메뉴로 돌아갈까요?\n진행 중인 판은 사라집니다."
                : "메인 메뉴로 돌아갈까요?";
            _dialog.Open(message, "뒤로가기", "메인 메뉴로", ShowMenu);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnMatchFinished(MatchOutcome outcome)
        {
            PlayerMatchResult result = RankSystem.ResultFor(outcome, Human);
            int delta = PlayerProgress.ApplyResult(result);

            string sign = delta > 0 ? "+" : "";
            string scoreLine =
                $"점수 {sign}{delta}  →  {PlayerProgress.Score}  (등급 Lv{PlayerProgress.Level})";
            _board.ShowResult(outcome, scoreLine);
        }
    }
}
