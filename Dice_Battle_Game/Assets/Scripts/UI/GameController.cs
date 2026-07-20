using System.Collections;
using UnityEngine;
using DiceBattle.Core;
using DiceBattle.AI;

namespace DiceBattle.UI
{
    /// <summary>
    /// Core 규칙 엔진 + AI + BoardView 를 연결해 사람(P1) vs AI(P2) 대전을 진행한다.
    /// (Phase 3: 최소 연결/플레이 루프. Phase 4에서 메뉴/난이도/선공선택 등으로 확장 예정)
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        private enum InputMode { None, Primary, Extra }

        private BoardView _board;
        private bool _useHeuristic = true;

        private DiceGame _game;
        private IAiStrategy _ai;
        private InputMode _mode = InputMode.None;

        private readonly PlayerId _human = PlayerId.One;
        private PlayerId Ai => _human.Other();

        private const float AiStepDelay = 0.6f;

        public void Init(BoardView board, bool useHeuristic)
        {
            _board = board;
            _useHeuristic = useHeuristic;
            _board.LineClicked += OnLineClicked;
            _board.RestartClicked += OnRestart;
            StartMatch();
        }

        private void StartMatch()
        {
            StopAllCoroutines();
            _mode = InputMode.None;
            _board.HideResult();

            _game = new DiceGame(new RandomDiceRoller());
            _ai = _useHeuristic ? new HeuristicAiStrategy() : (IAiStrategy)new RandomAiStrategy();

            // 사람이 선공 → 첫 주사위는 특수(기획서 9번).
            _game.Start(_human);
            _board.Render(_game.State);
            BeginTurn();
        }

        private void OnRestart() => StartMatch();

        private void BeginTurn()
        {
            var s = _game.State;
            if (s.IsGameOver)
            {
                _mode = InputMode.None;
                _board.ClearHighlights();
                _board.SetStatus("게임 종료");
                _board.ShowResult(_game.Outcome.Value);
                return;
            }

            if (s.CurrentPlayer == _human)
                SetHumanInput();
            else
                StartCoroutine(AiTurn());
        }

        private void SetHumanInput()
        {
            var s = _game.State;
            if (s.Phase == TurnPhase.AwaitingPrimaryPlacement)
            {
                _mode = InputMode.Primary;
                _board.SetStatus("당신 차례\n라인을 선택해 주사위를 놓으세요");
                _board.HighlightPrimary(s);
            }
            else if (s.Phase == TurnPhase.AwaitingExtraPlacement)
            {
                _mode = InputMode.Extra;
                _board.SetStatus("제거 성공! 추가 특수 주사위\n본인/상대 라인에 배치하세요");
                _board.HighlightExtra(s);
            }
        }

        private void OnLineClicked(PlayerId field, int line)
        {
            var s = _game.State;

            if (_mode == InputMode.Primary)
            {
                if (field != _human || !s.Field(_human)[line].HasSpace) return;

                bool wasSpecial = s.PendingDice.IsSpecial;
                var res = _game.PlacePrimary(line);
                _board.Render(s);
                AnimatePrimary(_human, line, wasSpecial, res);

                AfterHumanAction();
            }
            else if (_mode == InputMode.Extra)
            {
                if (!s.Field(field)[line].HasSpace) return;

                _game.PlaceExtra(field, line);
                _board.Render(s);
                AnimateExtra(field, line);

                AfterHumanAction();
            }
        }

        /// <summary>기본 배치 연출. 상호 소멸이면 양쪽 라인 플래시(특수는 배치 팝).</summary>
        private void AnimatePrimary(PlayerId placer, int line, bool wasSpecial, PlaceResult res)
        {
            if (!res.RemovalOccurred)
            {
                PopLastCell(placer, line);
                return;
            }

            _board.PlayRemoval(placer.Other(), line); // 상대 주사위 소멸
            if (wasSpecial)
                PopLastCell(placer, line);            // 특수는 생존
            else
                _board.PlayRemoval(placer, line);     // 배치 주사위도 소멸
        }

        private void AnimateExtra(PlayerId field, int line) => PopLastCell(field, line);

        private void PopLastCell(PlayerId field, int line)
        {
            int idx = _game.State.Field(field)[line].Count - 1;
            if (idx >= 0)
                _board.PlayPlace(field, line, idx);
        }

        private void AfterHumanAction()
        {
            _mode = InputMode.None;
            _board.ClearHighlights();
            BeginTurn();
        }

        private IEnumerator AiTurn()
        {
            _mode = InputMode.None;
            _board.ClearHighlights();
            _board.SetStatus("상대(AI) 차례...");
            yield return new WaitForSeconds(AiStepDelay);

            var s = _game.State;
            while (!s.IsGameOver && s.CurrentPlayer == Ai)
            {
                if (s.Phase == TurnPhase.AwaitingPrimaryPlacement)
                {
                    bool wasSpecial = s.PendingDice.IsSpecial;
                    int line = _ai.ChoosePrimaryLine(s, Ai);
                    var res = _game.PlacePrimary(line);
                    _board.Render(s);
                    AnimatePrimary(Ai, line, wasSpecial, res);
                }
                else if (s.Phase == TurnPhase.AwaitingExtraPlacement)
                {
                    var mv = _ai.ChooseExtraMove(s, Ai);
                    _game.PlaceExtra(mv.Field, mv.Line);
                    _board.Render(s);
                    AnimateExtra(mv.Field, mv.Line);
                }

                yield return new WaitForSeconds(AiStepDelay);
            }

            BeginTurn();
        }
    }
}
