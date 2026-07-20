using System;

namespace DiceBattle.Core
{
    /// <summary>
    /// 다이스 배틀 규칙 엔진(순수 C#, UnityEngine 비의존).
    ///
    /// 턴 흐름(기획서 5/6번):
    ///   턴 시작 → 주사위 획득 → (본인 필드) 라인 선택/배치 → 동일 숫자 제거 판정
    ///     - 제거 없음 → 턴 종료
    ///     - 제거 있음 → 특수 주사위 추가 획득 → (본인/상대 필드) 배치 → 턴 종료
    ///
    /// 상호작용 지점(라인 선택)마다 호출자가 결정해야 하므로 상태 머신 형태로 노출한다:
    ///   Start → PlacePrimary → (필요 시) PlaceExtra → (반복)
    /// </summary>
    public sealed class DiceGame
    {
        private readonly IDiceRoller _roller;

        public GameState State { get; }

        /// <summary>게임 종료 시 결과(진행 중이면 null).</summary>
        public MatchOutcome? Outcome => State.Result;

        public DiceGame(IDiceRoller roller)
        {
            _roller = roller ?? throw new ArgumentNullException(nameof(roller));
            State = new GameState();
        }

        /// <summary>
        /// 게임 시작. 선공 플레이어를 지정하고 첫 주사위를 굴린다.
        /// 기획서 9번: 선공 플레이어의 첫 번째 주사위는 특수 주사위.
        /// </summary>
        public void Start(PlayerId firstPlayer)
        {
            if (State.Phase != TurnPhase.NotStarted)
                throw new InvalidOperationException("이미 시작된 게임이다.");

            State.FirstPlayer = firstPlayer;
            State.CurrentPlayer = firstPlayer;

            int value = _roller.Roll();
            // 선공 첫 주사위는 특수 주사위로 생성.
            State.PendingDice = new Dice(value, isSpecial: true, owner: firstPlayer);
            State.Phase = TurnPhase.AwaitingPrimaryPlacement;
        }

        /// <summary>
        /// 이번 턴 기본 주사위를 "본인 필드"의 지정 라인에 배치한다(기획서 5번).
        /// 상대 같은 라인에 동일 숫자(특수 제외)가 있으면 상호 소멸한다(기획서 6번):
        /// 상대의 동일 숫자 주사위가 모두 제거되고, "배치한 주사위"도 함께 제거된다
        /// (배치 주사위가 특수라면 면역이라 그대로 배치된다 — 기획서 9번).
        /// 제거가 발생하면 특수 주사위를 추가로 굴려 대기 상태로 전환한다.
        /// </summary>
        public PlaceResult PlacePrimary(int lineIndex)
        {
            if (State.Phase != TurnPhase.AwaitingPrimaryPlacement)
                throw new InvalidOperationException("기본 주사위 배치 단계가 아니다.");

            ValidateLineIndex(lineIndex);

            PlayerId current = State.CurrentPlayer;
            Line ownLine = State.Field(current)[lineIndex];
            if (ownLine.IsFull)
                throw new InvalidOperationException("선택한 라인이 가득 차 배치할 수 없다.");

            Dice die = State.PendingDice;
            State.PendingDice = null;

            // 제거 판정: 상대의 같은 라인에서 동일 숫자(특수 제외) 전부 제거.
            Line opponentLine = State.Field(current.Other())[lineIndex];
            int removed = 0;
            if (opponentLine.HasRemovableValue(die.Value))
                removed = opponentLine.RemoveByValue(die.Value);

            if (removed > 0)
            {
                // 상호 소멸: 배치한 주사위도 사라진다. 단 특수 주사위는 면역이라 그대로 배치.
                if (die.IsSpecial)
                    ownLine.Add(die);

                // 기획서 6번: 제거 직후 특수 주사위 추가 획득, 본인/상대 필드 배치 가능.
                int value = _roller.Roll();
                State.PendingDice = new Dice(value, isSpecial: true, owner: current);
                State.Phase = TurnPhase.AwaitingExtraPlacement;
                return new PlaceResult(removalOccurred: true, removedCount: removed, extraDicePending: true);
            }

            // 제거 없음 → 정상 배치.
            ownLine.Add(die);
            EndTurn();
            return new PlaceResult(removalOccurred: false, removedCount: 0, extraDicePending: false);
        }

        /// <summary>
        /// 제거 후 추가 획득한 특수 주사위를 배치한다(기획서 6번).
        /// 본인/상대 필드 어디든 가능하며, 이 주사위로는 제거가 발생하지 않는다(턴당 제거 1회 제한).
        /// </summary>
        public void PlaceExtra(PlayerId targetField, int lineIndex)
        {
            if (State.Phase != TurnPhase.AwaitingExtraPlacement)
                throw new InvalidOperationException("추가 주사위 배치 단계가 아니다.");

            ValidateLineIndex(lineIndex);

            Line line = State.Field(targetField)[lineIndex];
            if (line.IsFull)
                throw new InvalidOperationException("선택한 라인이 가득 차 배치할 수 없다.");

            line.Add(State.PendingDice);
            State.PendingDice = null;

            // 추가 주사위는 제거 판정을 하지 않는다.
            EndTurn();
        }

        /// <summary>턴 종료 후 다음 진행자를 정하고 주사위를 굴린다. 종료 조건이면 결과 확정.</summary>
        private void EndTurn()
        {
            PlayerId current = State.CurrentPlayer;
            bool currentFull = State.Field(current).IsFull;
            bool otherFull = State.Field(current.Other()).IsFull;

            PlayerId? next = TurnOrder.Next(current, currentFull, otherFull);
            if (next == null)
            {
                FinishGame();
                return;
            }

            State.CurrentPlayer = next.Value;
            int value = _roller.Roll();
            // 일반 턴의 기본 주사위는 특수 주사위가 아니다.
            State.PendingDice = new Dice(value, isSpecial: false, owner: next.Value);
            State.Phase = TurnPhase.AwaitingPrimaryPlacement;
        }

        private void FinishGame()
        {
            State.Phase = TurnPhase.GameOver;
            State.PendingDice = null;
            State.Result = MatchEvaluator.Evaluate(State);
        }

        private static void ValidateLineIndex(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= Field.LineCount)
                throw new ArgumentOutOfRangeException(
                    nameof(lineIndex), lineIndex, "라인 인덱스는 0~2 이어야 한다.");
        }
    }
}
