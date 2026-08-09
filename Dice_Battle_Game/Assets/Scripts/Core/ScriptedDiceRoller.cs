using System;

namespace DiceBattle.Core
{
    /// <summary>
    /// 정해진 눈을 순서대로 내놓고, 다 쓰면 다른 롤러에게 넘기는 롤러.
    ///
    /// 튜토리얼처럼 "앞부분만 각본이고 뒤는 평소대로"인 경우를 위한 것이다. 각본이 끝났다고
    /// 굴림이 멈출 수는 없으므로 폴백을 반드시 들고 있어야 한다.
    ///
    /// <b>어느 플레이어가 굴리는지는 보지 않는다.</b> 굴림 순서 자체가 각본이라
    /// 소유자로 갈래를 나누면 한쪽이 한 번 더 굴리는 순간(제거 후 특수 주사위) 어긋난다.
    /// 폴백으로 넘어간 뒤에는 폴백이 알아서 소유자를 본다.
    /// </summary>
    public sealed class ScriptedDiceRoller : IDiceRoller
    {
        private readonly int[] _values;
        private readonly IDiceRoller _fallback;
        private int _index;

        public ScriptedDiceRoller(int[] values, IDiceRoller fallback)
        {
            _values = values ?? Array.Empty<int>();
            _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        }

        /// <summary>각본이 아직 남아 있는가.</summary>
        public bool HasScript => _index < _values.Length;

        public int Roll(PlayerId player)
        {
            if (!HasScript) return _fallback.Roll(player);

            int value = _values[_index++];
            // 각본에 엉뚱한 값이 적혀 있으면 규칙 엔진이 아니라 여기서 걸러 낸다.
            return value < Dice.MinValue || value > Dice.MaxValue
                ? _fallback.Roll(player)
                : value;
        }
    }
}
