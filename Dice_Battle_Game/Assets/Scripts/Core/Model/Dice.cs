using System;

namespace DiceBattle.Core
{
    /// <summary>
    /// 주사위 한 개. 값(1~6), 특수 주사위 여부, 배치한 주체를 가진다.
    /// 기획서 5/9번:
    /// - 특수 주사위(IsSpecial)는 제거 대상이 되지 않는다.
    /// - Owner 는 "이 주사위를 배치한 플레이어"로, 주로 연출/시각 구분용이며
    ///   핵심 규칙(점수/제거)은 주사위가 놓인 위치(필드+라인)로 판정한다.
    /// </summary>
    public sealed class Dice
    {
        public const int MinValue = 1;
        public const int MaxValue = 6;

        public int Value { get; }
        public bool IsSpecial { get; }
        public PlayerId Owner { get; }

        public Dice(int value, bool isSpecial, PlayerId owner)
        {
            if (value < MinValue || value > MaxValue)
                throw new ArgumentOutOfRangeException(
                    nameof(value), value, "주사위 값은 1~6 이어야 한다.");

            Value = value;
            IsSpecial = isSpecial;
            Owner = owner;
        }

        public override string ToString()
            => IsSpecial ? $"[{Value}*]" : $"[{Value}]";
    }
}
