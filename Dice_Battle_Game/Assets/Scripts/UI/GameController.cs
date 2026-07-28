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
        private enum InputMode { None, Primary, Extra, RerollPick }

        private BoardView _board;
        private DifficultyConfig _difficulty;
        private DiceGame _game;
        private IAiStrategy _ai;
        private InputMode _mode = InputMode.None;
        private int _level = 3;

        // 리롤: 판당 1회. 사용하면 그 판이 끝날 때까지 다시 못 쓴다.
        private bool _rerollAvailable;
        private Dice _rerollCandidate;

        private readonly PlayerId _human = PlayerId.One;
        private readonly System.Random _rng = new System.Random();
        private PlayerId Ai => _human.Other();

        // 주사위를 뽑고 무엇을 뽑았는지 확인할 시간(초) → 그 뒤 AI가 행동.
        private const float AiThinkDelay = 1.5f;
        // 배치 결과를 잠깐 보여주는 시간(초).
        private const float AiActDelay = 0.8f;
        // 게임 종료 후 결과 화면을 띄우기 전 대기(승자 도장 확인 시간).
        private const float EndGameDelay = 2f;

        /// <summary>결과 화면에서 "메뉴로" 선택 시 발생.</summary>
        public event Action MenuRequested;

        /// <summary>한 판 종료 시 결과를 전달(점수/등급 갱신용).</summary>
        public event Action<MatchOutcome> MatchFinished;

        public void Init(BoardView board, DifficultyConfig difficulty)
        {
            _board = board;
            _difficulty = difficulty;
            _board.LineClicked += OnLineClicked;
            _board.RestartClicked += OnRestart;
            _board.MenuClicked += () => MenuRequested?.Invoke();
            _board.RerollClicked += OnRerollClicked;
            _board.TrayDiceClicked += OnTrayDiceClicked;
        }

        /// <summary>지정 난이도로 새 대전 시작(선공 랜덤).</summary>
        public void StartMatch(int level)
        {
            _level = level;
            StopAllCoroutines();
            _mode = InputMode.None;
            _rerollAvailable = true; // 판마다 1회 충전
            _rerollCandidate = null;
            _board.ClearFx();
            _board.HideResult();
            _board.SetTrayPickable(false);
            _board.SetRerollInteractable(false);

            // AI(P2)만 난이도 가중 주사위, 사람은 공정. 설정 에셋이 있으면 그 값을 사용.
            _game = new DiceGame(_difficulty != null
                ? _difficulty.CreateRoller(Ai, level)
                : new DifficultyDiceRoller(Ai, level));
            _ai = _difficulty != null
                ? _difficulty.CreateAi(level)
                : new LeveledAiStrategy(level);

            _board.SetHeader(PlayerProgress.Score, level);

            // 선공 랜덤 → 선공의 첫 주사위는 특수(기획서 9번).
            PlayerId first = _rng.Next(2) == 0 ? PlayerId.One : PlayerId.Two;
            _game.Start(first);
            _board.Render(_game.State);
            _board.RefreshTray(_game.State);
            BeginTurn();
        }

        private void OnRestart() => StartMatch(_level);

        /// <summary>아직 승부가 나지 않은 판이 진행 중인지.</summary>
        public bool IsMatchActive => _game != null && !_game.State.IsGameOver;

        /// <summary>
        /// 진행 중인 판을 즉시 중단한다(뒤로가기로 메뉴 복귀 등).
        /// AI 턴 코루틴과 연출이 메뉴 화면에서 계속 도는 것을 막는다.
        /// </summary>
        public void AbortMatch()
        {
            StopAllCoroutines();
            _mode = InputMode.None;
            _rerollCandidate = null;
            _board.AbortAnimations();
            _board.ClearHighlights();
            _board.HideResult();
        }

        private IEnumerator EndGameAfterDelay()
        {
            yield return new WaitForSeconds(EndGameDelay);
            MatchFinished?.Invoke(_game.Outcome.Value); // 점수 갱신 후 GameManager가 결과창 표시
        }

        private void BeginTurn()
        {
            var s = _game.State;
            if (s.IsGameOver)
            {
                _mode = InputMode.None;
                _board.ClearHighlights();
                _board.SetRerollInteractable(false);
                // 라인별 승자 도장을 볼 수 있도록 잠시 뒤 결과 화면 표시.
                StartCoroutine(EndGameAfterDelay());
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
                _board.HighlightPrimary(s);
            }
            else if (s.Phase == TurnPhase.AwaitingExtraPlacement)
            {
                _mode = InputMode.Extra;
                _board.HighlightExtra(s);
            }

            // 리롤은 "내가 주사위를 배치해야 하는 타이밍"에만 누를 수 있다.
            _board.SetRerollInteractable(_rerollAvailable && _mode != InputMode.None);
        }

        // ---- 리롤 ----

        private void OnRerollClicked()
        {
            if (!_rerollAvailable) return;
            if (_mode != InputMode.Primary && _mode != InputMode.Extra) return;

            _rerollAvailable = false; // 이 판에서는 영구 소진
            _mode = InputMode.None;
            _board.SetRerollInteractable(false);
            _board.ClearHighlights(); // 선택 중에는 라인 배치 불가
            StartCoroutine(RerollRoutine());
        }

        private IEnumerator RerollRoutine()
        {
            _rerollCandidate = _game.RollRerollCandidate();

            yield return StartCoroutine(
                _board.RollCandidateRoutine(_rerollCandidate.Value, _rerollCandidate.IsSpecial));

            _mode = InputMode.RerollPick;
        }

        private void OnTrayDiceClicked(int index)
        {
            if (_mode != InputMode.RerollPick) return;
            _mode = InputMode.None;
            StartCoroutine(ResolveRerollPick(index));
        }

        private IEnumerator ResolveRerollPick(int index)
        {
            // 1 = 새로 굴린 후보를 선택 → 대기 주사위 교체. 0이면 기존 유지.
            if (index == 1) _game.ApplyReroll(_rerollCandidate);
            _rerollCandidate = null;

            yield return StartCoroutine(_board.ResolvePickRoutine(index));

            // 원래 배치 단계로 복귀(리롤 버튼은 소진되어 비활성).
            SetHumanInput();
        }

        private void OnLineClicked(PlayerId field, int line)
        {
            var s = _game.State;

            if (_mode == InputMode.Primary)
            {
                // 내 라인 클릭: 그대로 배치. 상대 라인 클릭: 그 라인이 "제거 가능"이면 동일하게 처리.
                bool myLine = field == _human && s.Field(_human)[line].HasSpace;
                bool removalTarget = field == Ai && s.Field(_human)[line].HasSpace
                    && s.Field(Ai)[line].HasRemovableValue(s.PendingDice.Value);
                if (!myLine && !removalTarget) return;

                _mode = InputMode.None;
                _board.ClearHighlights();
                _board.SetRerollInteractable(false); // 배치 연출 중에는 잠금
                StartCoroutine(HumanPrimary(line));
            }
            else if (_mode == InputMode.Extra)
            {
                if (!s.Field(field)[line].HasSpace) return;
                _mode = InputMode.None;
                _board.ClearHighlights();
                _board.SetRerollInteractable(false); // 배치 연출 중에는 잠금
                StartCoroutine(HumanExtra(field, line));
            }
        }

        private IEnumerator HumanPrimary(int line)
        {
            yield return StartCoroutine(DoPrimary(_human, line));
            BeginTurn();
        }

        private IEnumerator HumanExtra(PlayerId field, int line)
        {
            yield return StartCoroutine(DoExtra(_human, field, line));
            BeginTurn();
        }

        /// <summary>기본 배치 + 이동/제거 연출(완료까지 대기).</summary>
        private IEnumerator DoPrimary(PlayerId actor, int line)
        {
            var s = _game.State;
            Dice die = s.PendingDice;
            int value = die.Value;
            bool special = die.IsSpecial;
            DiceSide side = SideOf(die.Owner);
            PlayerId opp = actor.Other();

            // 자기 라인 삽입 위치(같은 값 그룹화) + 밀려나는 주사위 캡처.
            var ownLine = s.Field(actor)[line];
            int ownInsert = ownLine.InsertIndexFor(value);
            CaptureShift(ownLine, ownInsert, out var sv, out var ss, out var sp);

            // 제거 연출용: 상대 라인의 현재(제거 전) 상태를 캡처.
            var oppLine = s.Field(opp)[line];
            int preCount = oppLine.Count;
            var preValues = new int[preCount];
            var preSides = new DiceSide[preCount];
            var preSpecial = new bool[preCount];
            var preRemoved = new bool[preCount];
            for (int i = 0; i < preCount; i++)
            {
                var d = oppLine.Dice[i];
                preValues[i] = d.Value;
                preSides[i] = SideOf(d.Owner);
                preSpecial[i] = d.IsSpecial;
                preRemoved[i] = d.Value == value && !d.IsSpecial;
            }

            var res = _game.PlacePrimary(line);

            if (res.RemovalOccurred)
            {
                yield return StartCoroutine(_board.RemovalFxRoutine(
                    actor, line, value, special, side, ownInsert, special,
                    opp, preValues, preSides, preSpecial, preRemoved));
            }
            else
            {
                yield return StartCoroutine(_board.PlaceGroupedFxRoutine(
                    actor, line, ownInsert, value, special, side, sv, ss, sp));
            }
            _board.Render(s);
            _board.RefreshTray(s);
        }

        /// <summary>추가(특수) 배치 + 그룹화 이동 연출.</summary>
        private IEnumerator DoExtra(PlayerId actor, PlayerId targetField, int line)
        {
            var s = _game.State;
            Dice die = s.PendingDice;
            int value = die.Value;
            bool special = die.IsSpecial;
            DiceSide side = SideOf(die.Owner);

            var lineModel = s.Field(targetField)[line];
            int insert = lineModel.InsertIndexFor(value);
            CaptureShift(lineModel, insert, out var sv, out var ss, out var sp);

            _game.PlaceExtra(targetField, line);
            yield return StartCoroutine(_board.PlaceGroupedFxRoutine(
                targetField, line, insert, value, special, side, sv, ss, sp));
            _board.Render(s);
            _board.RefreshTray(s);
        }

        private DiceSide SideOf(PlayerId owner) => owner == _human ? DiceSide.Player : DiceSide.Ai;

        /// <summary>insertIndex 이후 밀려나는 주사위들의 값/세트/특수여부를 캡처.</summary>
        private void CaptureShift(Line line, int insertIndex, out int[] values, out DiceSide[] sides, out bool[] specials)
        {
            int shiftCount = line.Count - insertIndex;
            values = new int[shiftCount];
            sides = new DiceSide[shiftCount];
            specials = new bool[shiftCount];
            for (int k = 0; k < shiftCount; k++)
            {
                var d = line.Dice[insertIndex + k];
                values[k] = d.Value;
                sides[k] = SideOf(d.Owner);
                specials[k] = d.IsSpecial;
            }
        }

        private IEnumerator AiTurn()
        {
            _mode = InputMode.None;
            _board.ClearHighlights();
            _board.SetRerollInteractable(false); // 리롤은 플레이어 전용

            var s = _game.State;
            while (!s.IsGameOver && s.CurrentPlayer == Ai)
            {
                // 이번 손패(주사위)는 트레이에 굴려져 표시된 상태. 인지할 시간을 준 뒤 행동.
                yield return new WaitForSeconds(AiThinkDelay);

                if (s.Phase == TurnPhase.AwaitingPrimaryPlacement)
                {
                    int line = _ai.ChoosePrimaryLine(s, Ai);
                    yield return StartCoroutine(DoPrimary(Ai, line));
                }
                else
                {
                    var mv = _ai.ChooseExtraMove(s, Ai);
                    yield return StartCoroutine(DoExtra(Ai, mv.Field, mv.Line));
                }

                yield return new WaitForSeconds(AiActDelay);
            }

            BeginTurn();
        }
    }
}
