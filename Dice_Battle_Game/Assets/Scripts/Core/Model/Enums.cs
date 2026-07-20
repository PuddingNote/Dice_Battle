namespace DiceBattle.Core
{
    /// <summary>턴 진행 상태.</summary>
    public enum TurnPhase
    {
        NotStarted,                // Start() 호출 전
        AwaitingPrimaryPlacement,  // 이번 턴의 기본 주사위 배치 대기 (본인 필드만)
        AwaitingExtraPlacement,    // 제거 발생 후 특수 주사위 배치 대기 (본인/상대 필드)
        GameOver
    }

    /// <summary>라인별 승패 결과.</summary>
    public enum LineResult
    {
        PlayerOne,
        PlayerTwo,
        Draw
    }
}
