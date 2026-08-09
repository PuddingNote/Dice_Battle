namespace DiceBattle.Core
{
    /// <summary>각본이 끼어들 수 있는 지점. 모든 안내는 이 넷 중 하나에 매달린다.</summary>
    public enum TutorialBeat
    {
        /// <summary>첫 턴이 시작되기 전.</summary>
        MatchStart,

        /// <summary>사람이 입력할 수 있게 된 직후(배치/추가 배치/리롤 선택 모두).</summary>
        HumanInputReady,

        /// <summary>사람이 한 수 두고 연출까지 끝난 뒤.</summary>
        HumanActed,

        /// <summary>AI가 한 수 두고 연출까지 끝난 뒤.</summary>
        AiActed
    }

    /// <summary>한 단계에서 사람이 해야 할 일.</summary>
    public enum TutorialAction
    {
        /// <summary>읽기만 한다. 아무 데나 탭하면 넘어간다.</summary>
        Read,

        /// <summary>지정한 필드의 지정한 줄을 누른다.</summary>
        PlaceLine,

        /// <summary>리롤 버튼을 누른다.</summary>
        Reroll,

        /// <summary>리롤로 새로 굴린 주사위를 고른다.</summary>
        PickNew
    }

    /// <summary>
    /// 안내 패널을 화면 어디에 띄울지. 설명하는 대상을 가리지 않는 쪽을 고른다
    /// (윗줄을 설명하면 아래, 트레이를 설명하면 위).
    /// </summary>
    public enum TutorialAnchor
    {
        Top,
        Center,
        Bottom
    }

    /// <summary>튜토리얼 한 단계.</summary>
    public readonly struct TutorialStep
    {
        public TutorialBeat Beat { get; }
        public TutorialAction Action { get; }
        public TutorialAnchor Anchor { get; }
        public string Text { get; }

        /// <summary><see cref="TutorialAction.PlaceLine"/>일 때 눌러야 하는 필드.</summary>
        public PlayerId Field { get; }

        /// <summary><see cref="TutorialAction.PlaceLine"/>일 때 눌러야 하는 줄(0~2).</summary>
        public int Line { get; }

        private TutorialStep(TutorialBeat beat, TutorialAction action, TutorialAnchor anchor,
            string text, PlayerId field, int line)
        {
            Beat = beat;
            Action = action;
            Anchor = anchor;
            Text = text;
            Field = field;
            Line = line;
        }

        public static TutorialStep Message(TutorialBeat beat, TutorialAnchor anchor, string text)
            => new TutorialStep(beat, TutorialAction.Read, anchor, text, PlayerId.One, 0);

        public static TutorialStep PlaceAt(TutorialBeat beat, TutorialAnchor anchor, string text,
            PlayerId field, int line)
            => new TutorialStep(beat, TutorialAction.PlaceLine, anchor, text, field, line);

        public static TutorialStep UseReroll(TutorialBeat beat, TutorialAnchor anchor, string text)
            => new TutorialStep(beat, TutorialAction.Reroll, anchor, text, PlayerId.One, 0);

        public static TutorialStep PickNewDie(TutorialBeat beat, TutorialAnchor anchor, string text)
            => new TutorialStep(beat, TutorialAction.PickNew, anchor, text, PlayerId.One, 0);
    }

    /// <summary>AI가 각본대로 두는 한 수.</summary>
    public readonly struct TutorialAiMove
    {
        /// <summary>제거 후 받은 특수 주사위를 놓는 수인가.</summary>
        public bool IsExtra { get; }

        /// <summary>놓을 필드(추가 배치만 상대 필드를 고를 수 있다).</summary>
        public PlayerId Field { get; }

        public int Line { get; }

        private TutorialAiMove(bool isExtra, PlayerId field, int line)
        {
            IsExtra = isExtra;
            Field = field;
            Line = line;
        }

        public static TutorialAiMove Primary(int line)
            => new TutorialAiMove(false, TutorialScript.Ai, line);

        public static TutorialAiMove Extra(PlayerId field, int line)
            => new TutorialAiMove(true, field, line);
    }

    /// <summary>
    /// 첫 실행 튜토리얼의 각본.
    ///
    /// <b>주사위 눈·AI의 수·사람이 눌러야 할 곳이 한 파일에 모여 있다.</b> 셋 중 하나만 고치면
    /// 판이 통째로 어긋나는데(예: 제거를 가르치는 순간 상대 줄에 같은 숫자가 없다), 서로 다른
    /// 파일에 흩어져 있으면 그게 어긋났다는 사실을 실행해 보기 전까지 알 수 없다.
    /// 고칠 때는 반드시 아래 진행표를 같이 고칠 것.
    ///
    /// 진행표(P = 사람, A = AI. 굵은 수가 가르치는 장면):
    ///   1  A  2*  → A 0줄            A0=[2*]
    ///   2  P  5   → P 0줄            P0=[5]
    ///   3  A  3   → A 1줄            A1=[3]
    ///   4  P  3   → <b>A 1줄</b>     상호 소멸, A1=[] P1=[]
    ///   4b P  6*  → P 1줄            P1=[6*]
    ///   5  A  5   → A 0줄            <b>P의 5가 제거됨</b>, P0=[]
    ///   5b A  1*  → A 2줄            A2=[1*]
    ///   6  P  1 → <b>리롤</b> → 6 → P 0줄   P0=[6]
    ///   ────────── 여기까지가 각본. 이후는 자유 배치 ──────────
    ///   이 시점 점수  P 6 / 6 / 0   A 2 / 0 / 1  → 두 줄 우세
    ///
    /// <b>승리는 보장이 아니라 "사실상 확정"이다.</b> 뒷구간을 자유 배치로 열어 두었으므로
    /// 한 줄에 몰아 놓으면 질 수도 있다. 완료 화면은 그 경우에도 자연스럽게 나와야 한다.
    /// </summary>
    public static class TutorialScript
    {
        /// <summary>튜토리얼에서 사람은 항상 1P다(GameController와 같은 약속).</summary>
        public const PlayerId Human = PlayerId.One;

        public const PlayerId Ai = PlayerId.Two;

        /// <summary>선공. 선공의 첫 주사위는 특수 주사위이므로 AI에게 준다.</summary>
        /// <remarks>
        /// 사람이 선공이면 첫 수부터 금색 특수 주사위를 쥐게 되어, 제거를 배우기도 전에
        /// "제거되지 않는 주사위"를 설명해야 한다. 배치 → 제거 → 특수 순서가 무너진다.
        /// </remarks>
        public const PlayerId FirstPlayer = Ai;

        /// <summary>
        /// 굴림 순서대로의 눈. <see cref="DiceGame"/>이 굴리는 순서 그대로다:
        /// 시작(A) → 턴이 넘어갈 때마다 한 번 → 제거가 나면 특수 주사위로 한 번 더 → 리롤 후보.
        ///
        /// 마지막 6은 리롤 후보다. <b>바로 앞의 1과 반드시 달라야 한다</b> —
        /// 같으면 <see cref="DiceGame.RollRerollCandidate"/>가 "다른 눈"을 찾느라
        /// 다음 값까지 삼켜 이후 각본이 한 칸씩 밀린다.
        /// </summary>
        public static readonly int[] Dice = { 2, 5, 3, 3, 6, 5, 1, 1, 6 };

        /// <summary>AI가 둘 수. 각본이 끝나면 진짜 Lv.1 AI가 이어받는다.</summary>
        public static readonly TutorialAiMove[] AiMoves =
        {
            TutorialAiMove.Primary(0),      // 2*  → A 0줄
            TutorialAiMove.Primary(1),      // 3   → A 1줄
            TutorialAiMove.Primary(0),      // 5   → A 0줄, 사람의 5를 제거
            TutorialAiMove.Extra(Ai, 2)     // 1*  → A 2줄
        };

        /// <summary>
        /// 안내와 강제 입력. 순서대로 하나씩 소비된다.
        ///
        /// 각 단계는 자기가 붙을 <see cref="TutorialBeat"/>를 들고 있다. 지점이 와도 맨 앞
        /// 단계의 지점이 다르면 아무 일도 일어나지 않는다 — AI가 한 턴에 두 번 두거나
        /// (제거 후 추가 배치) 하는 경우에도 안내가 엉뚱한 곳에서 튀어나오지 않는다.
        /// </summary>
        public static readonly TutorialStep[] Steps =
        {
            // ---- 판이 시작되기 전: 무엇을 보고 있는지부터 ----
            TutorialStep.Message(TutorialBeat.MatchStart, TutorialAnchor.Center,
                "왼쪽이 내 필드, 오른쪽이 상대 필드입니다.\n" +
                "각각 세 줄이고, 한 줄에 주사위 세 개가 들어갑니다."),

            TutorialStep.Message(TutorialBeat.MatchStart, TutorialAnchor.Center,
                "같은 높이의 줄끼리 점수를 겨룹니다.\n" +
                "세 줄 중 더 많이 이긴 쪽이 최종 승리입니다."),

            TutorialStep.Message(TutorialBeat.MatchStart, TutorialAnchor.Center,
                "이번 판은 상대가 먼저 시작합니다."),

            // ---- 1. 기본 배치 ----
            TutorialStep.Message(TutorialBeat.HumanInputReady, TutorialAnchor.Bottom,
                "내 차례입니다.\n주사위는 아래에 자동으로 굴려집니다."),

            TutorialStep.PlaceAt(TutorialBeat.HumanInputReady, TutorialAnchor.Bottom,
                "주사위를 놓을 줄을 고르세요.\n밝게 표시된 줄을 눌러 보세요.",
                Human, 0),

            TutorialStep.Message(TutorialBeat.AiActed, TutorialAnchor.Bottom,
                "상대도 같은 방식으로 자기 필드에 놓습니다."),

            // ---- 2. 제거 ----
            TutorialStep.Message(TutorialBeat.HumanInputReady, TutorialAnchor.Bottom,
                "이번에 굴린 3은 상대 가운데 줄에 있는 3과 같은 숫자입니다."),

            TutorialStep.PlaceAt(TutorialBeat.HumanInputReady, TutorialAnchor.Bottom,
                "같은 줄의 같은 숫자끼리 만나면 주사위를 제거할 수 있습니다.\n상대의 가운데 줄을 눌러 보세요.",
                Ai, 1),

            // ---- 3. 특수 주사위 ----
            TutorialStep.Message(TutorialBeat.HumanInputReady, TutorialAnchor.Bottom,
                "제거에 성공하면 금색 특수 주사위를 하나 받습니다."),

            TutorialStep.PlaceAt(TutorialBeat.HumanInputReady, TutorialAnchor.Bottom,
                "특수 주사위는 제거되지 않고, 어느 필드에나 놓을 수 있습니다.\n" +
                "내 가운데 줄에 놓아 보세요.",
                Human, 1),

            // ---- 4. 반대로 당해 보기 ----
            TutorialStep.Message(TutorialBeat.AiActed, TutorialAnchor.Bottom,
                "반대로 상대도 내 주사위를 제거할 수 있습니다.\n방금 내 5가 사라졌습니다."),

            // ---- 5. 리롤 ----
            TutorialStep.Message(TutorialBeat.HumanInputReady, TutorialAnchor.Bottom,
                "빈자리가 생겼는데 이번 주사위는 1이네요."),

            TutorialStep.UseReroll(TutorialBeat.HumanInputReady, TutorialAnchor.Bottom,
                "이번엔 리롤을 사용해봅시다. 왼쪽 아래 버튼을 눌러 보세요.\n한 번만 사용 가능하고 광고 시청 후 한번 더 사용할 수 있습니다."),

            // 고를 쪽을 지정한다. "둘 중 아무거나"라고 적어 놓고 한쪽만 눌리게 하면 고장으로
            // 보이고, 정말 둘 다 열어 주면 1을 골랐을 때 바로 다음 안내("두 줄에서 앞선다")가
            // 거짓이 된다.
            TutorialStep.PickNewDie(TutorialBeat.HumanInputReady, TutorialAnchor.Bottom,
                "새로 6이 나왔습니다.\n오른쪽 주사위를 골라 보세요."),

            TutorialStep.PlaceAt(TutorialBeat.HumanInputReady, TutorialAnchor.Bottom,
                "고른 주사위를 맨 윗줄에 놓아 봅시다.",
                Human, 0),

            // ---- 마무리: 여기서 강제를 푼다 ----
            TutorialStep.Message(TutorialBeat.HumanActed, TutorialAnchor.Center,
                "기본 튜토리얼은 여기까지입니다.\n이대로 승리해보세요!!")
        };

        /// <summary>튜토리얼을 끝까지 마쳤을 때 주는 코인.</summary>
        /// <remarks>
        /// 출석 한 주 전액이 100이므로 그 절반이다. Lv.1 점수 보호권이 40이라
        /// 받자마자 한 번 써 볼 수 있는 액수이기도 하다.
        /// </remarks>
        public const int CompletionCoins = 50;
    }
}
