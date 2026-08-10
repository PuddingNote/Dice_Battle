namespace DiceBattle.Core
{
    /// <summary>주사위 한 개의 순수 데이터 표현(친선대전 전송용).</summary>
    public sealed class DiceData
    {
        public int Value;
        public bool IsSpecial;
        public PlayerId Owner;
    }

    /// <summary>한 라인(최대 3칸)의 순수 데이터 표현.</summary>
    public sealed class LineData
    {
        public DiceData[] Dice;
    }

    /// <summary>한 필드(3라인)의 순수 데이터 표현.</summary>
    public sealed class FieldData
    {
        public LineData[] Lines;
    }

    /// <summary>
    /// 이번 턴 배치 직전에 리롤이 있었는지(연출 재현 전용 — 게임 규칙에는 영향 없다).
    /// <see cref="GameState"/>는 이 정보를 담지 않는다(리롤 결과는 이미 PendingDice에
    /// 녹아 있어 규칙상 필요 없음) — 그래서 전송 시점에만 부가로 실어 보낸다.
    /// </summary>
    public sealed class RerollData
    {
        /// <summary>리롤로 굴린 후보 주사위의 값/특수 여부.</summary>
        public int Value;
        public bool IsSpecial;

        /// <summary>후보를 골랐는가(true=후보로 교체, false=기존 유지).</summary>
        public bool Picked;
    }

    /// <summary><see cref="GameState"/> 전체의 순수 데이터 표현.</summary>
    public sealed class GameStateData
    {
        public PlayerId FirstPlayer;
        public PlayerId CurrentPlayer;
        public TurnPhase Phase;

        /// <summary>배치 대기 중인 주사위(없으면 null — Phase가 GameOver일 때만 그렇다).</summary>
        public DiceData PendingDice;

        /// <summary>인덱스 0 = PlayerId.One의 필드, 1 = PlayerId.Two의 필드.</summary>
        public FieldData[] Fields;

        /// <summary>
        /// 직전에 놓인 주사위가 이번 턴 중 리롤을 거쳤다면 그 기록(없으면 null).
        /// 친선대전 상대 턴 연출 재현에만 쓰인다 — <see cref="GameStateSnapshot.Restore"/>는
        /// 이 값을 읽지 않는다(GameState 규칙과 무관).
        /// </summary>
        public RerollData LastReroll;
    }

    /// <summary>
    /// <see cref="GameState"/> ↔ 순수 데이터(<see cref="GameStateData"/>) 왕복 변환.
    ///
    /// 친선대전의 "풀 상태 스냅샷 동기화"에 쓰인다(docs/FriendlyMatch.md 1장) — 네트워크
    /// 계층(Managers)이 실제 전송 형식(Firebase Dictionary 등)으로 바꾸기 전 단계의
    /// 중립 표현이다. <see cref="GameState"/>의 진행 관련 속성은 setter가 internal이라
    /// 같은 어셈블리(Core) 안에서만 <see cref="Restore"/>를 만들 수 있다.
    ///
    /// <see cref="MatchOutcome"/>(승패 결과)은 전송하지 않는다 — Phase가 GameOver일 때
    /// 받는 쪽이 <see cref="MatchEvaluator.Evaluate"/>로 그대로 재현되는 값이라 중복이다.
    /// </summary>
    public static class GameStateSnapshot
    {
        public static GameStateData Capture(GameState state)
        {
            return new GameStateData
            {
                FirstPlayer = state.FirstPlayer,
                CurrentPlayer = state.CurrentPlayer,
                Phase = state.Phase,
                PendingDice = ToDto(state.PendingDice),
                Fields = new[]
                {
                    CaptureField(state.Field(PlayerId.One)),
                    CaptureField(state.Field(PlayerId.Two)),
                },
            };
        }

        private static FieldData CaptureField(Field field)
        {
            var lines = new LineData[Field.LineCount];
            for (int i = 0; i < Field.LineCount; i++)
            {
                Line line = field[i];
                var dice = new DiceData[line.Count];
                for (int d = 0; d < line.Count; d++)
                    dice[d] = ToDto(line.Dice[d]);
                lines[i] = new LineData { Dice = dice };
            }
            return new FieldData { Lines = lines };
        }

        private static DiceData ToDto(Dice d)
            => d == null ? null : new DiceData { Value = d.Value, IsSpecial = d.IsSpecial, Owner = d.Owner };

        public static GameState Restore(GameStateData data)
        {
            var state = new GameState
            {
                FirstPlayer = data.FirstPlayer,
                CurrentPlayer = data.CurrentPlayer,
                Phase = data.Phase,
                PendingDice = FromDto(data.PendingDice),
            };

            for (int p = 0; p < 2; p++)
            {
                Field field = state.Field((PlayerId)p);
                FieldData fieldData = data.Fields[p];
                for (int i = 0; i < Field.LineCount; i++)
                {
                    // Add()는 같은 값끼리 인접하도록 그룹화하며 삽입한다. 캡처 순서 그대로
                    // 다시 Add하면 원래 배치 결과가 그대로 재현된다(순서는 결정적).
                    foreach (DiceData dd in fieldData.Lines[i].Dice)
                        field[i].Add(FromDto(dd));
                }
            }

            // Result는 전송하지 않고 여기서 다시 계산한다 — 이미 복원한 필드 데이터만으로
            // DiceGame.FinishGame이 하던 것과 동일하게 재현된다.
            if (state.Phase == TurnPhase.GameOver)
                state.Result = MatchEvaluator.Evaluate(state);

            return state;
        }

        private static Dice FromDto(DiceData d)
            => d == null ? null : new Dice(d.Value, d.IsSpecial, d.Owner);

        /// <summary>
        /// 두 스냅샷이 진행 상태 기준으로 완전히 같은가. <see cref="GameStateData.LastReroll"/>은
        /// 일부러 비교하지 않는다 — 내가 방금 쓴 값이 그대로 되돌아온 "메아리"인지 판별하는
        /// 용도인데, 로컬에서 새로 캡처한 쪽은 이 필드를 채우지 않으므로 포함하면 항상 달라진다.
        /// </summary>
        public static bool StatesEqual(GameStateData a, GameStateData b)
        {
            if (a == null || b == null) return a == b;
            if (a.FirstPlayer != b.FirstPlayer) return false;
            if (a.CurrentPlayer != b.CurrentPlayer) return false;
            if (a.Phase != b.Phase) return false;
            if (!DiceEqual(a.PendingDice, b.PendingDice)) return false;

            for (int p = 0; p < 2; p++)
            {
                var fa = a.Fields[p];
                var fb = b.Fields[p];
                for (int i = 0; i < Field.LineCount; i++)
                    if (!DiceListEqual(fa.Lines[i].Dice, fb.Lines[i].Dice)) return false;
            }
            return true;
        }

        private static bool DiceListEqual(DiceData[] a, DiceData[] b)
        {
            int la = a?.Length ?? 0;
            int lb = b?.Length ?? 0;
            if (la != lb) return false;
            for (int i = 0; i < la; i++)
                if (!DiceEqual(a[i], b[i])) return false;
            return true;
        }

        private static bool DiceEqual(DiceData a, DiceData b)
        {
            if (a == null || b == null) return a == b;
            return a.Value == b.Value && a.IsSpecial == b.IsSpecial && a.Owner == b.Owner;
        }
    }
}
