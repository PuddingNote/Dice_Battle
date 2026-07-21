namespace DiceBattle.Core
{
    /// <summary>
    /// 주사위 굴림 추상화. 테스트에서 결정적 시퀀스를 주입할 수 있도록 인터페이스로 분리.
    /// Roll(player) 은 해당 플레이어가 획득할 1~6 값을 반환한다.
    /// (난이도별 가중 롤러가 플레이어에 따라 분포를 달리하기 위해 player를 받는다.)
    /// </summary>
    public interface IDiceRoller
    {
        int Roll(PlayerId player);
    }
}
