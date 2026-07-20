namespace DiceBattle.Core
{
    /// <summary>
    /// 게임 전체 상태 컨테이너(데이터). 규칙 진행은 <see cref="DiceGame"/> 가 담당한다.
    /// setter 는 internal 이라 같은 어셈블리(DiceGame)만 변경할 수 있다.
    /// </summary>
    public sealed class GameState
    {
        private readonly Field[] _fields;

        public PlayerId FirstPlayer { get; internal set; }
        public PlayerId CurrentPlayer { get; internal set; }
        public TurnPhase Phase { get; internal set; }

        /// <summary>배치를 기다리는 손에 든 주사위(없으면 null).</summary>
        public Dice PendingDice { get; internal set; }

        /// <summary>게임 종료 시 결과(진행 중이면 null).</summary>
        public MatchOutcome? Result { get; internal set; }

        public GameState()
        {
            _fields = new Field[] { new Field(PlayerId.One), new Field(PlayerId.Two) };
            Phase = TurnPhase.NotStarted;
        }

        public Field Field(PlayerId player) => _fields[(int)player];

        public bool BothFieldsFull => _fields[0].IsFull && _fields[1].IsFull;

        public bool IsGameOver => Phase == TurnPhase.GameOver;
    }
}
