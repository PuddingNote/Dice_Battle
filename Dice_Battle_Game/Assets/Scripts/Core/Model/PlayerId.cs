namespace DiceBattle.Core
{
    /// <summary>
    /// 두 플레이어를 식별한다. 순수 로직에서는 중립적으로 One/Two 로 다룬다.
    /// (선공/후공, 사람/AI 매핑은 상위 레이어에서 결정)
    /// </summary>
    public enum PlayerId
    {
        One = 0,
        Two = 1
    }

    public static class PlayerIdExtensions
    {
        /// <summary>상대 플레이어를 반환.</summary>
        public static PlayerId Other(this PlayerId p)
            => p == PlayerId.One ? PlayerId.Two : PlayerId.One;

        public static int Index(this PlayerId p) => (int)p;
    }
}
