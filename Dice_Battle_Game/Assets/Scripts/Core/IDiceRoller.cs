namespace DiceBattle.Core
{
    /// <summary>
    /// 주사위 굴림 추상화. 테스트에서 결정적 시퀀스를 주입할 수 있도록 인터페이스로 분리.
    /// Roll() 은 1~6 사이의 값을 반환해야 한다.
    /// </summary>
    public interface IDiceRoller
    {
        int Roll();
    }
}
