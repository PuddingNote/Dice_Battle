using System;
using System.Collections;
using UnityEngine;
using DiceBattle.Core;
using DiceBattle.AI;

namespace DiceBattle.UI
{
    /// <summary>
    /// Core 규칙 엔진 + 난이도 AI + BoardView 를 연결해 사람(P1) vs AI(P2) 대전을 진행한다.
    /// 선공은 매 판 랜덤, AI는 난이도 레벨(1~5)로 동작한다.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        private enum InputMode { None, Primary, Extra }

        private BoardView _board;
        private DiceGame _game;
        private IAiStrategy _ai;
        private InputMode _mode = InputMode.None;
        private int _level = 3;

        private readonly PlayerId _human = PlayerId.One;
        private readonly System.Random _rng = new System.Random();
        private PlayerId Ai => _human.Other();

        // 주사위를 뽑고 무엇을 뽑았는지 확인할 시간(초) → 그 뒤 AI가 행동.
        private const float AiThinkDelay = 1.5f;
        // 배치 결과를 잠깐 보여주는 시간(초).
        private const float AiActDelay = 0.8f;

        /// <summary>결과 화면에서 "메뉴로" 선택 시 발생.</summary>
        public event Action MenuRequested;

        /// <summary>한 판 종료 시 결과를 전달(점수/등급 갱신용).</summary>
        public event Action<MatchOutcome> MatchFinished;

        public void Init(BoardView board)
        {
            _board = board;
            _board.LineClicked += OnLineClicked;
            _board.RestartClicked += OnRestart;
            _board.MenuClicked += () => MenuRequested?.Invoke();
        }

        /// <summary>지정 난이도로 새 대전 시작(선공 랜덤).</summary>
        public void StartMatch(int level)
        {
            _level = level;
            StopAllCoroutines();
            _mode = InputMode.None;
            _board.HideResult();

            // AI(P2)만 난이도 가중 주사위, 사람은 공정.
            _game = new DiceGame(new DifficultyDiceRoller(Ai, level));
            _ai = new LeveledAiStrategy(level);

            _board.SetLevelInfo(level);

            // 선공 랜덤 → 선공의 첫 주사위는 특수(기획서 9번).
            PlayerId first = _rng.Next(2) == 0 ? PlayerId.One : PlayerId.Two;
            _game.Start(first);
            _board.Render(_game.State);
            BeginTurn();
        }

        private void OnRestart() => StartMatch(_level);

        private void BeginTurn()
        {
            var s = _game.State;
            if (s.IsGameOver)
            {
                _mode = InputMode.None;
                _board.ClearHighlights();
                _board.SetStatus("게임 종료");
                MatchFinished?.Invoke(_game.Outcome.Value); // 점수 갱신 후 GameManager가 결과창 표시
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

            var s = _game.State;
            while (!s.IsGameOver && s.CurrentPlayer == Ai)
            {
                // 이번 손패(주사위)는 이미 트레이에 굴려져 표시된 상태.
                // 무엇을 뽑았는지 플레이어가 인지할 시간을 준 뒤 행동한다.
                var die = s.PendingDice;
                if (s.Phase == TurnPhase.AwaitingExtraPlacement)
                    _board.SetStatus($"상대(AI) 추가 주사위 {die.Value}\n배치 준비 중...");
                else
                    _board.SetStatus($"상대(AI) 차례\n주사위 {die.Value} 획득 — 배치 준비 중...");
                yield return new WaitForSeconds(AiThinkDelay);

                if (s.Phase == TurnPhase.AwaitingPrimaryPlacement)
                {
                    bool wasSpecial = die.IsSpecial;
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

                // 배치 결과를 잠시 보여준 뒤 다음 행동으로.
                yield return new WaitForSeconds(AiActDelay);
            }

            BeginTurn();
        }
    }
}
