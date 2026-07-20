namespace DiceBattle.Core
{
    /// <summary>
    /// 기획서 8번의 턴 순서/스킵 규칙을 순수 함수로 분리.
    /// - 기본은 두 플레이어가 번갈아 진행.
    /// - 한쪽 필드가 먼저 가득 차면, 아직 안 찬 플레이어만 계속 진행.
    /// - 양쪽 모두 가득 차면 게임 종료.
    /// </summary>
    public static class TurnOrder
    {
        /// <summary>
        /// 방금 <paramref name="current"/> 의 턴이 끝난 뒤 다음에 진행할 플레이어를 반환.
        /// 양쪽 모두 가득 찼으면 null(게임 종료).
        /// </summary>
        public static PlayerId? Next(PlayerId current, bool currentFieldFull, bool otherFieldFull)
        {
            if (currentFieldFull && otherFieldFull)
                return null;

            PlayerId other = current.Other();

            // 상대가 아직 안 찼으면 정상적으로 상대에게 넘긴다.
            if (!otherFieldFull)
                return other;

            // 상대는 가득 찼고 본인은 안 찼으므로 본인이 계속 진행.
            return current;
        }
    }
}
