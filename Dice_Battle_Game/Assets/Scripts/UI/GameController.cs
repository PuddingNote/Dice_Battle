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
        /// <summary>지금 사람이 무엇을 눌러야 하는 상태인가. 튜토리얼이 이 값으로 안내를 고른다.</summary>
        public enum InputMode { None, Primary, Extra, RerollPick }

        private BoardView _board;
        private DifficultyConfig _difficulty;
        private DiceGame _game;
        private IAiStrategy _ai;
        private InputMode _mode = InputMode.None;

        // 이 판의 난이도. 시작할 때 고정되고 판이 끝날 때까지 바뀌지 않는다 —
        // 점수 정산도 이 값으로 해야 도중에 기준이 흔들리지 않는다.
        private int _level;

        /// <summary>이번 판에서 내가 제거한 상대 주사위 누적 개수(전적 집계용).</summary>
        private int _humanRemoved;

        // 아래 셋은 일일 미션 집계용. 판이 시작될 때마다 0으로 돌아간다.
        private int _humanRerolls;
        private int _humanExtras;
        private int _humanExtrasOnOpponent;

        // 리롤: 판당 1회. 사용하면 그 판이 끝날 때까지 다시 못 쓴다.
        private bool _rerollAvailable;
        // 광고를 보고 얻는 추가 리롤. 기본 리롤과 별도로 판당 1회.
        private bool _adRerollAvailable;
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

        /// <summary>
        /// 결과 화면에서 "계속하기" 선택 시 발생(전면 광고가 끝난 뒤).
        /// 새 난이도가 열렸는지는 점수를 정산하는 쪽만 알 수 있으므로, 다음 화면은 거기서 정한다.
        /// </summary>
        public event Action ContinueRequested;

        /// <summary>
        /// 기본 리롤을 소진한 뒤 리롤 버튼을 눌렀을 때 발생.
        /// 확인 창과 광고 재생은 <see cref="GameManager"/>가 처리하고,
        /// 광고를 끝까지 본 경우에만 <see cref="GrantAdReroll"/>를 호출한다.
        /// </summary>
        public event Action AdRerollRequested;

        /// <summary>한 판 종료 시 결과를 전달(점수·해금 갱신용).</summary>
        public event Action<MatchOutcome> MatchFinished;

        // ---- 튜토리얼 훅 ----
        //
        // 아래 다섯은 전부 null이 기본이고, null이면 이 파일은 평소와 똑같이 동작한다.
        // 튜토리얼 각본을 진행 로직 안에 섞어 넣지 않으려고 밖에서 끼워 넣는 구조로 뒀다.

        /// <summary>
        /// 진행 중 정해진 지점에서 실행되고, 끝날 때까지 판이 멈춘다(안내 문구 표시용).
        /// null이면 아무 데서도 멈추지 않는다.
        /// </summary>
        public Func<TutorialBeat, IEnumerator> Interlude { get; set; }

        /// <summary>지금 누를 수 있는 줄만 통과시킨다. null이면 규칙이 허락하는 곳 전부.</summary>
        public Func<PlayerId, int, bool> LineGate { get; set; }

        /// <summary>리롤 버튼을 눌러도 되는가. null이면 평소 조건대로.</summary>
        public Func<bool> RerollGate { get; set; }

        /// <summary>리롤 선택에서 고를 수 있는 쪽만 통과시킨다(0=기존, 1=새로 굴린 것).</summary>
        public Func<int, bool> TrayGate { get; set; }

        /// <summary>
        /// 상단 표시를 점수 대신 이 문구로 고정한다. null이면 점수를 표시한다.
        /// </summary>
        public string HeaderOverride { get; set; }

        /// <summary>
        /// 광고를 일절 띄우지 않는다. 첫 실행 튜토리얼에 광고가 끼면 그 자체로 이탈 사유이고,
        /// 각본이 광고 화면에 끊기면 어디까지 진행됐는지도 흐려진다.
        /// </summary>
        public bool SuppressAds { get; set; }

        /// <summary>지금 사람이 무엇을 눌러야 하는 상태인가.</summary>
        public InputMode Mode => _mode;

        public void Init(BoardView board, DifficultyConfig difficulty)
        {
            _board = board;
            _difficulty = difficulty;
            _board.LineClicked += OnLineClicked;
            // 결과 화면을 벗어나는 순간이 전면 광고를 띄우기에 자연스러운 지점이다.
            // 이 두 버튼은 결과 오버레이 안에만 있으므로 판당 정확히 한 번 지나간다.
            // 판이 진행되는 도중에는 절대 띄우지 않는다 — 정책 위반이자 최악의 경험이다.
            _board.ContinueClicked += () => LeaveResult(() => ContinueRequested?.Invoke());
            _board.MenuClicked += () => LeaveResult(() => MenuRequested?.Invoke());
            _board.RerollClicked += OnRerollClicked;
            _board.TrayDiceClicked += OnTrayDiceClicked;
        }

        /// <summary>결과 화면을 벗어난다. 광고를 막아 둔 판(튜토리얼)에서는 그냥 넘어간다.</summary>
        private void LeaveResult(Action next)
        {
            if (SuppressAds) next?.Invoke();
            else AdManager.ShowInterstitial(next);
        }

        /// <summary>이번 판의 난이도(시작 시점에 고정).</summary>
        public int Level => _level;

        /// <summary>
        /// 이번 판에서 <b>내가</b> 제거한 상대 주사위 개수(제거가 일어난 횟수가 아니다).
        /// 전적 집계용이라 판이 시작될 때마다 0으로 돌아간다.
        /// </summary>
        public int HumanRemovedThisMatch => _humanRemoved;

        /// <summary>이번 판에서 내가 리롤한 횟수(기본 + 광고).</summary>
        public int HumanRerollsThisMatch => _humanRerolls;

        /// <summary>이번 판에서 내가 배치한 특수(추가) 주사위 개수.</summary>
        public int HumanExtrasThisMatch => _humanExtras;

        /// <summary>그중 상대 필드에 놓은 개수.</summary>
        public int HumanExtrasOnOpponentThisMatch => _humanExtrasOnOpponent;

        /// <summary>지정 난이도로 새 대전 시작(선공 랜덤).</summary>
        public void StartMatch(int level) => StartMatch(level, null, null, null);

        /// <summary>
        /// 주사위와 AI를 직접 지정해 대전을 시작한다(튜토리얼 각본용).
        /// null을 넘긴 자리는 평소대로 난이도 설정에서 만든다.
        /// </summary>
        /// <param name="firstPlayer">선공. null이면 매 판 랜덤.</param>
        public void StartMatch(int level, IDiceRoller roller, IAiStrategy ai, PlayerId? firstPlayer)
        {
            StopAllCoroutines();
            _level = level;
            _humanRemoved = 0;
            _humanRerolls = 0;
            _humanExtras = 0;
            _humanExtrasOnOpponent = 0;
            _mode = InputMode.None;
            _rerollAvailable = true; // 판마다 1회 충전
            _adRerollAvailable = true; // 광고로 얻는 추가 리롤도 판당 1회
            _rerollCandidate = null;
            _board.ClearFx();
            _board.HideResult();
            _board.SetTrayPickable(false);
            _board.SetRerollInteractable(false);

            // AI(P2)만 난이도 가중 주사위, 사람은 공정. 설정 에셋이 있으면 그 값을 사용.
            _game = new DiceGame(roller ?? DefaultRoller(level));
            _ai = ai ?? DefaultAi(level);

            if (HeaderOverride != null) _board.SetHeaderText(HeaderOverride);
            else _board.SetHeader(PlayerProgress.Score, level);

            // 선공 랜덤 → 선공의 첫 주사위는 특수(기획서 9번).
            PlayerId first = firstPlayer
                ?? (_rng.Next(2) == 0 ? PlayerId.One : PlayerId.Two);
            _game.Start(first);
            _board.Render(_game.State);
            _board.RefreshTray(_game.State);
            StartCoroutine(OpeningRoutine());
        }

        /// <summary>난이도 설정 에셋이 있으면 그 값으로, 없으면 코드 기본값으로 롤러를 만든다.</summary>
        public IDiceRoller DefaultRoller(int level)
            => _difficulty != null ? _difficulty.CreateRoller(Ai, level) : new DifficultyDiceRoller(Ai, level);

        /// <summary>같은 규칙으로 AI를 만든다. 튜토리얼 각본이 끝난 뒤 이어받을 폴백이기도 하다.</summary>
        public IAiStrategy DefaultAi(int level)
            => _difficulty != null ? _difficulty.CreateAi(level) : new LeveledAiStrategy(level);

        /// <summary>첫 턴 전에 한 번 멈출 자리(튜토리얼 도입 안내).</summary>
        private IEnumerator OpeningRoutine()
        {
            yield return RunInterlude(TutorialBeat.MatchStart);
            BeginTurn();
        }

        /// <summary>각본이 끼어들 자리. 훅이 없으면 한 프레임도 쉬지 않는다.</summary>
        private IEnumerator RunInterlude(TutorialBeat beat)
        {
            var hook = Interlude;
            if (hook == null) yield break;
            yield return StartCoroutine(hook(beat));
        }

        /// <summary>아직 승부가 나지 않은 판이 진행 중인지.</summary>
        public bool IsMatchActive => _game != null && !_game.State.IsGameOver;

        /// <summary>
        /// 지금 이 순간의 판세를 판정 규칙 그대로 계산한다(빈 자리는 0점으로 친다).
        /// 튜토리얼을 도중에 마칠 때 "이기고 있었는지"를 정직하게 적기 위한 것이다.
        /// </summary>
        public MatchOutcome CurrentStanding => MatchEvaluator.Evaluate(_game.State);

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
                LockInput();
                // 라인별 승자 도장을 볼 수 있도록 잠시 뒤 결과 화면 표시.
                StartCoroutine(EndGameAfterDelay());
                return;
            }

            if (s.CurrentPlayer == _human)
                StartCoroutine(HumanTurn());
            else
                StartCoroutine(AiTurn());
        }

        /// <summary>
        /// 연출이 도는 동안 라인 클릭과 리롤을 모두 막는다.
        /// 입력 경로가 두 개(라인/리롤)라 한쪽만 잠그면 다른 쪽으로 새어 들어온다.
        /// </summary>
        private void LockInput()
        {
            _mode = InputMode.None;
            _board.ClearHighlights();
            _board.SetRerollInteractable(false);
        }

        /// <summary>
        /// 사람 턴 시작. <b>주사위가 트레이에서 자리를 잡은 뒤에</b> 입력을 연다.
        /// 굴림·이동 연출은 1초쯤 걸리는데, 그 사이에 리롤을 누르면
        /// 이동 중이던 주사위가 좌측 슬롯으로 순간이동한다.
        /// </summary>
        private IEnumerator HumanTurn()
        {
            LockInput();
            yield return _board.WaitForTrayIdle();
            // 안내를 먼저 띄운다. 어느 줄을 열어 줄지가 안내 단계에 달려 있어
            // 순서를 바꾸면 지난 단계의 조건으로 강조하게 된다.
            yield return RunInterlude(TutorialBeat.HumanInputReady);
            SetHumanInput();
        }

        private void SetHumanInput()
        {
            var s = _game.State;
            if (s.Phase == TurnPhase.AwaitingPrimaryPlacement)
            {
                _mode = InputMode.Primary;
                _board.HighlightPrimary(s, LineGate);
            }
            else if (s.Phase == TurnPhase.AwaitingExtraPlacement)
            {
                _mode = InputMode.Extra;
                _board.HighlightExtra(s, LineGate);
            }

            // 리롤은 "내가 주사위를 배치해야 하는 타이밍"에만 누를 수 있다.
            // 기본 리롤을 소진해도 광고 리롤이 남아 있으면 계속 눌린다.
            _board.SetRerollInteractable(
                (_rerollAvailable || CanOfferAdReroll) && _mode != InputMode.None
                && (RerollGate == null || RerollGate()));
        }

        // ---- 리롤 ----

        /// <summary>광고 리롤을 제안할 수 있는 상태인가. 광고가 안 실려 있으면 제안하지 않는다.</summary>
        private bool CanOfferAdReroll => _adRerollAvailable && !SuppressAds && AdManager.IsRewardedReady;

        private void OnRerollClicked()
        {
            if (_mode != InputMode.Primary && _mode != InputMode.Extra) return;
            if (RerollGate != null && !RerollGate()) return;

            if (_rerollAvailable)
            {
                _rerollAvailable = false; // 이 판에서는 영구 소진
                BeginReroll();
                return;
            }

            // 기본 리롤을 다 썼다 → 광고로 한 번 더 제안.
            if (!CanOfferAdReroll) return;
            AdRerollRequested?.Invoke();
        }

        /// <summary>
        /// 광고를 끝까지 본 뒤 호출된다. 추가 리롤 1회를 소진하고 굴림을 시작한다.
        /// 광고를 보는 동안 판이 끝났거나 단계가 바뀌었을 수 있으므로 조건을 다시 확인한다.
        /// </summary>
        public void GrantAdReroll()
        {
            if (!_adRerollAvailable) return;
            if (_mode != InputMode.Primary && _mode != InputMode.Extra) return;

            _adRerollAvailable = false;
            BeginReroll();
        }

        private void BeginReroll()
        {
            // 기본 리롤과 광고 리롤이 모두 여기로 모인다. 미션 집계는 여기 한 곳이면 된다.
            _humanRerolls++;

            LockInput(); // 선택 중에는 라인 배치도 불가
            StartCoroutine(RerollRoutine());
        }

        private IEnumerator RerollRoutine()
        {
            _rerollCandidate = _game.RollRerollCandidate();

            yield return StartCoroutine(_board.RollCandidateRoutine(_rerollCandidate));

            // 두 주사위가 다 놓인 뒤에 안내를 띄운다. 무엇을 고르라는 말이니
            // 고를 것이 보이기 전에 띄우면 뜻이 통하지 않는다.
            yield return RunInterlude(TutorialBeat.HumanInputReady);
            _mode = InputMode.RerollPick;
        }

        private void OnTrayDiceClicked(int index)
        {
            if (_mode != InputMode.RerollPick) return;
            if (TrayGate != null && !TrayGate(index)) return;
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
            yield return RunInterlude(TutorialBeat.HumanInputReady);
            SetHumanInput();
        }

        private void OnLineClicked(PlayerId field, int line)
        {
            var s = _game.State;
            if (LineGate != null && !LineGate(field, line)) return;

            if (_mode == InputMode.Primary)
            {
                // 내 라인 클릭: 그대로 배치. 상대 라인 클릭: 그 라인이 "제거 가능"이면 동일하게 처리.
                bool myLine = field == _human && s.Field(_human)[line].HasSpace;
                bool removalTarget = field == Ai && s.Field(_human)[line].HasSpace
                    && s.Field(Ai)[line].HasRemovableValue(s.PendingDice.Value);
                if (!myLine && !removalTarget) return;

                LockInput(); // 배치 연출 중에는 잠금
                StartCoroutine(HumanPrimary(line));
            }
            else if (_mode == InputMode.Extra)
            {
                if (!s.Field(field)[line].HasSpace) return;
                LockInput(); // 배치 연출 중에는 잠금
                StartCoroutine(HumanExtra(field, line));
            }
        }

        private IEnumerator HumanPrimary(int line)
        {
            yield return StartCoroutine(DoPrimary(_human, line));
            yield return RunInterlude(TutorialBeat.HumanActed);
            BeginTurn();
        }

        private IEnumerator HumanExtra(PlayerId field, int line)
        {
            yield return StartCoroutine(DoExtra(_human, field, line));
            yield return RunInterlude(TutorialBeat.HumanActed);
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

            // 제거는 기본 배치에서만 일어난다(추가 배치는 제거를 유발하지 않는다).
            // 그래서 DoExtra에는 같은 집계가 없다.
            if (actor == _human) _humanRemoved += res.RemovedCount;

            if (res.RemovalOccurred)
            {
                // 제거 연출은 자체 사운드(떨림/충돌)를 BoardView에서 낸다.
                yield return StartCoroutine(_board.RemovalFxRoutine(
                    actor, line, value, special, side, ownInsert, special,
                    opp, preValues, preSides, preSpecial, preRemoved));
            }
            else
            {
                AudioManager.PlayDicePlace();
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

            if (actor == _human)
            {
                _humanExtras++;
                // 자기 라인이 아니라 상대 라인에 놓았는가(견제 플레이).
                if (targetField != actor) _humanExtrasOnOpponent++;
            }

            AudioManager.PlayDicePlace();
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
            LockInput(); // 리롤은 플레이어 전용

            var s = _game.State;
            while (!s.IsGameOver && s.CurrentPlayer == Ai)
            {
                // 이번 손패(주사위)는 트레이에 굴려져 표시된 상태. 인지할 시간을 준 뒤 행동.
                yield return new WaitForSeconds(AiThinkDelay);

                // 배치 연출이 주사위의 실제 위치에서 출발하도록 확인만 한다.
                // 굴림은 생각 시간보다 짧아 보통 0프레임이다 — 앞에 두면 두 시간이
                // 겹치지 못하고 순차로 더해져 AI 턴이 통째로 느려진다.
                yield return _board.WaitForTrayIdle();

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
                yield return RunInterlude(TutorialBeat.AiActed);
            }

            BeginTurn();
        }
    }
}
