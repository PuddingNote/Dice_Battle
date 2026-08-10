using System;
using System.Collections;
using UnityEngine;
using DiceBattle.Core;
using DiceBattle.AI;

namespace DiceBattle.UI
{
    /// <summary>
    /// 튜토리얼 각본을 진행시킨다. <see cref="TutorialScript.Steps"/>를 앞에서부터 하나씩
    /// 소비하며, 읽기 단계는 탭을 기다리고 행동 단계는 눌러야 할 곳만 열어 둔다.
    ///
    /// 진행 방식은 두 줄로 요약된다:
    ///   1) 지점이 올 때마다 맨 앞 단계를 본다. 그 단계의 지점과 다르면 아무것도 하지 않는다.
    ///   2) 행동 단계가 켜져 있는 채로 다음 지점이 왔다면, 그 행동은 이미 이루어진 것이다.
    ///
    /// 두 번째 규칙이 핵심이다. "눌렀는가"를 직접 감시하면 강조를 그리려고 조건을 확인하는
    /// 것만으로도 단계가 넘어가 버린다. 지점은 오직 실제로 무언가 일어난 뒤에만 오므로
    /// 지점을 신호로 삼는 편이 안전하다.
    /// </summary>
    public sealed class TutorialController : MonoBehaviour
    {
        /// <summary>튜토리얼은 난이도가 무의미하지만(AI가 각본대로 둔다) 표에는 값이 필요하다.</summary>
        private const int ScriptLevel = 1;

        /// <summary>리롤 선택에서 새로 굴린 주사위의 자리.</summary>
        private const int NewDieIndex = 1;

        private GameController _controller;
        private BoardView _board;
        private ConfirmDialogView _dialog;
        private TutorialGuideView _guide;
        private TutorialCompleteView _complete;

        private int _stepIndex;
        private bool _hasActive;
        private TutorialStep _active;

        /// <summary>각본으로 입력을 묶어 두고 있는가. 마지막 단계가 끝나면 풀린다.</summary>
        private bool _forcing;

        /// <summary>
        /// 가르칠 것을 다 가르쳤는가. 이 뒤로는 그만두어도 "다 배운 것"이므로 보상을 준다.
        /// </summary>
        private bool _lessonDone;

        /// <summary>완주 코인은 한 번만 준다. 중간에 마치기와 판 종료가 겹칠 수 있다.</summary>
        private bool _rewarded;

        private bool _tapped;

        /// <summary>튜토리얼이 진행 중인가(결과 정산을 건너뛸지 판단하는 데 쓴다).</summary>
        public bool IsRunning { get; private set; }

        /// <summary>완료 화면이 떠 있는가. 이때 뒤로가기는 건너뛰기가 아니라 마무리다.</summary>
        public bool IsShowingResult => _complete != null && _complete.IsOpen;

        /// <summary>완료하거나 건너뛴 뒤. 다음 화면은 받는 쪽이 정한다.</summary>
        public event Action Finished;

        public void Init(RectTransform canvasRoot, GameController controller, BoardView board,
            ConfirmDialogView dialog)
        {
            _controller = controller;
            _board = board;
            _dialog = dialog;

            _guide = new TutorialGuideView(canvasRoot);
            _guide.Tapped += OnTapped;
            _guide.SkipRequested += AskSkip;

            _complete = new TutorialCompleteView(canvasRoot);
            _complete.Closed += Finish;
        }

        private void Update()
        {
            if (IsRunning) _guide.Tick(Time.unscaledDeltaTime);
        }

        // ---- 시작 / 끝 ----

        /// <summary>각본 대전을 시작한다. 보드는 부르는 쪽이 이미 띄워 둔 상태여야 한다.</summary>
        public void Begin()
        {
            IsRunning = true;
            _stepIndex = 0;
            _hasActive = false;
            _forcing = true;
            _lessonDone = false;
            _rewarded = false;
            _tapped = false;

            _guide.SetVisible(true);
            _guide.HidePanel();
            _guide.HideRing();

            _controller.Interlude = RunBeat;
            _controller.LineGate = AllowLine;
            _controller.RerollGate = AllowReroll;
            _controller.TrayGate = AllowTrayPick;
            _controller.HeaderOverride = "튜토리얼";
            _controller.SuppressAds = true; // FriendlyMode/좌우 퍼스펙티브는 뒤이은 StartMatch가 항상 확정한다

            // 각본이 끝나면 진짜 Lv.1 AI와 Lv.1 주사위가 이어받는다. 뒷구간은 자유 배치라
            // 각본을 계속 밀어붙이면 놓은 자리와 맞지 않는 수가 나온다.
            var roller = new ScriptedDiceRoller(TutorialScript.Dice, _controller.DefaultRoller());
            var ai = new ScriptedAiStrategy(TutorialScript.AiMoves, _controller.DefaultAi(ScriptLevel));

            _controller.StartMatch(ScriptLevel, roller, ai, TutorialScript.FirstPlayer);
        }

        /// <summary>판이 끝났을 때(정산은 하지 않는다). 완료 화면만 띄운다.</summary>
        public void OnMatchFinished(MatchOutcome outcome)
            => ShowComplete(outcome.Winner == TutorialScript.Human);

        /// <summary>
        /// 배울 것을 다 배운 뒤 그만두는 경우. 남은 자유 배치 구간은 가르치는 내용이 없으므로
        /// 여기서 마쳐도 완주로 친다. 보상을 빼앗으면 "다 했는데 왜 못 받지"가 된다.
        /// </summary>
        private void FinishEarly()
        {
            MatchOutcome standing = _controller.CurrentStanding;
            _controller.AbortMatch();
            ShowComplete(standing.Winner == TutorialScript.Human);
        }

        private void ShowComplete(bool won)
        {
            ReleaseControl();
            _guide.SetVisible(false);

            int coins = 0;
            if (!_rewarded)
            {
                _rewarded = true;
                coins = TutorialScript.CompletionCoins;
                PlayerWallet.AddCoins(coins);
            }

            _complete.Open(coins, won);
        }

        /// <summary>건너뛰기·뒤로가기. 확인 창은 부르는 쪽이 이미 받았다고 본다.</summary>
        public void Quit()
        {
            _controller.AbortMatch();
            ReleaseControl();
            _guide.SetVisible(false);
            _complete.Close();
            Finish();
        }

        /// <summary>
        /// 건너뛰기 확인. 뒤로가기도 같은 창을 쓴다.
        /// 배우는 도중이면 "그만두기"(보상 없음), 다 배운 뒤면 "마치기"(완주 처리)로 갈린다.
        /// </summary>
        public void AskSkip()
        {
            if (!IsRunning) return;

            if (_lessonDone)
            {
                _dialog.Open("배울 내용은 모두 끝났습니다.\n여기서 마칠까요?",
                    "계속 두기", "마치기", FinishEarly);
                return;
            }

            _dialog.Open("튜토리얼을 그만둘까요?\n설정 창에서 다시 볼 수 있습니다.",
                "계속하기", "그만두기", Quit);
        }

        private void Finish()
        {
            IsRunning = false;
            TutorialState.MarkDone();
            Finished?.Invoke();
        }

        /// <summary>훅과 잠금을 모두 걷어 낸다. 다음 판이 튜토리얼 상태를 물려받으면 안 된다.</summary>
        private void ReleaseControl()
        {
            _forcing = false;
            _hasActive = false;
            _controller.Interlude = null;
            _controller.LineGate = null;
            _controller.RerollGate = null;
            _controller.TrayGate = null;
            _controller.HeaderOverride = null;
            _controller.SuppressAds = false;
            _guide.HidePanel();
            _guide.HideRing();
        }

        // ---- 각본 진행 ----

        /// <summary>
        /// 진행 지점 하나를 처리한다. 읽기 단계가 이어지는 동안 판이 멈춰 있다.
        /// </summary>
        private IEnumerator RunBeat(TutorialBeat beat)
        {
            if (!IsRunning) yield break;

            // 행동 단계가 켜진 채로 지점이 왔다 = 그 행동이 이루어졌다.
            if (_hasActive)
            {
                _hasActive = false;
                _stepIndex++;
                _guide.HidePanel();
                _guide.HideRing();
            }

            // 이 지점에 매달린 읽기 단계를 순서대로 보여 준다.
            while (_stepIndex < TutorialScript.Steps.Length
                   && TutorialScript.Steps[_stepIndex].Beat == beat
                   && TutorialScript.Steps[_stepIndex].Action == TutorialAction.Read)
            {
                TutorialStep step = TutorialScript.Steps[_stepIndex];
                _guide.HideRing();
                _guide.Show(step.Text, step.Anchor, blocking: true);

                _tapped = false;
                while (!_tapped && IsRunning) yield return null;
                if (!IsRunning) yield break;

                _stepIndex++;
            }

            _guide.HidePanel();

            if (_stepIndex >= TutorialScript.Steps.Length)
            {
                // 각본 끝. 여기서부터는 아무 데나 놓을 수 있고, 그만두어도 완주로 친다.
                _forcing = false;
                _lessonDone = true;
                _guide.HideRing();
                yield break;
            }

            TutorialStep next = TutorialScript.Steps[_stepIndex];
            if (next.Beat != beat) yield break; // 다음 단계는 다른 지점에서 열린다

            Activate(next);
        }

        /// <summary>행동 단계를 켠다. 문구는 탭을 삼키지 않고, 눌러야 할 곳에 테두리가 붙는다.</summary>
        private void Activate(TutorialStep step)
        {
            _hasActive = true;
            _active = step;
            _guide.Show(step.Text, step.Anchor, blocking: false);
            _guide.HighlightTarget(TargetOf(step));
        }

        private RectTransform TargetOf(TutorialStep step)
        {
            switch (step.Action)
            {
                case TutorialAction.PlaceLine: return _board.LineRect(step.Field, step.Line);
                case TutorialAction.Reroll: return _board.RerollRect;
                case TutorialAction.PickNew: return _board.TrayRect(NewDieIndex);
                default: return null;
            }
        }

        private void OnTapped() => _tapped = true;

        // ---- 입력 잠금 ----
        //
        // 셋 다 같은 규칙이다: 각본을 놓은 뒤에는 전부 열고, 각본 중에는 지금 켜진 행동
        // 단계가 가리키는 것 하나만 연다. 행동 단계가 없는 순간(연출 중, AI 차례)은
        // 어차피 입력이 잠겨 있으므로 전부 막아 두는 편이 안전하다.

        private bool AllowLine(PlayerId field, int line)
        {
            if (!_forcing) return true;
            if (!_hasActive || _active.Action != TutorialAction.PlaceLine) return false;
            return _active.Field == field && _active.Line == line;
        }

        private bool AllowReroll()
        {
            if (!_forcing) return true;
            return _hasActive && _active.Action == TutorialAction.Reroll;
        }

        private bool AllowTrayPick(int index)
        {
            if (!_forcing) return true;
            if (!_hasActive || _active.Action != TutorialAction.PickNew) return false;
            return index == NewDieIndex;
        }
    }
}
