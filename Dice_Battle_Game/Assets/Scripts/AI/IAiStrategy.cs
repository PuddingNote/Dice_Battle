using DiceBattle.Core;

namespace DiceBattle.AI
{
    /// <summary>제거 후 추가(특수) 주사위 배치 결정: 대상 필드 + 라인.</summary>
    public readonly struct ExtraMove
    {
        public PlayerId Field { get; }
        public int Line { get; }

        public ExtraMove(PlayerId field, int line)
        {
            Field = field;
            Line = line;
        }
    }

    /// <summary>
    /// AI 의사결정 전략(순수 로직, UI 비의존).
    /// 반드시 "합법 수"만 반환해야 한다:
    /// - 기본 배치는 본인 필드의 빈칸이 있는 라인.
    /// - 추가 배치는 본인/상대 필드 중 빈칸이 있는 라인.
    /// </summary>
    public interface IAiStrategy
    {
        /// <summary>이번 턴 기본 주사위를 놓을 본인 필드의 라인(0~2)을 고른다.</summary>
        int ChoosePrimaryLine(GameState state, PlayerId me);

        /// <summary>제거 후 추가 특수 주사위를 놓을 필드/라인을 고른다.</summary>
        ExtraMove ChooseExtraMove(GameState state, PlayerId me);
    }
}
