using UnityEngine;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 화면 흐름 관리: 메인 메뉴 ↔ 게임.
    /// 등급(점수)에 따라 난이도가 자동 결정되고, 매 판 결과로 점수가 갱신된다.
    /// (테스트용으로 메뉴에서 난이도 직접 선택도 가능)
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        private const PlayerId Human = PlayerId.One;

        private MenuView _menu;
        private BoardView _board;
        private GameController _controller;

        public void Init(RectTransform canvasRoot)
        {
            var boardGo = new GameObject("BoardView");
            boardGo.transform.SetParent(transform, false);
            _board = boardGo.AddComponent<BoardView>();
            _board.Build(canvasRoot, Human);

            var controllerGo = new GameObject("GameController");
            controllerGo.transform.SetParent(transform, false);
            _controller = controllerGo.AddComponent<GameController>();
            _controller.Init(_board);
            _controller.MenuRequested += ShowMenu;
            _controller.MatchFinished += OnMatchFinished;

            // 메뉴는 보드 위에 오도록 나중에 생성.
            var menuGo = new GameObject("MenuView");
            menuGo.transform.SetParent(transform, false);
            _menu = menuGo.AddComponent<MenuView>();
            _menu.Build(canvasRoot);
            _menu.StartRequested += () => StartGame(PlayerProgress.Level); // 등급 자동 난이도
            _menu.LevelSelected += StartGame;                              // 테스트: 직접 선택

            ShowMenu();
        }

        private void ShowMenu()
        {
            _board.SetVisible(false);
            _menu.SetScore(PlayerProgress.Score, PlayerProgress.Level);
            _menu.SetVisible(true);
        }

        private void StartGame(int level)
        {
            _menu.SetVisible(false);
            _board.SetVisible(true);
            _controller.StartMatch(level);
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
