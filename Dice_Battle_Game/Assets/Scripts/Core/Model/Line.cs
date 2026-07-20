using System;
using System.Collections.Generic;
using System.Linq;

namespace DiceBattle.Core
{
    /// <summary>
    /// 한 라인(최대 3칸). 기획서 4번: 각 라인은 최대 3개의 주사위를 보관.
    /// </summary>
    public sealed class Line
    {
        public const int Capacity = 3;

        private readonly List<Dice> _dice = new List<Dice>(Capacity);

        public IReadOnlyList<Dice> Dice => _dice;
        public int Count => _dice.Count;
        public bool IsFull => _dice.Count >= Capacity;
        public bool HasSpace => _dice.Count < Capacity;

        /// <summary>주사위를 배치. 라인이 가득 찼으면 예외.</summary>
        public void Add(Dice dice)
        {
            if (dice == null) throw new ArgumentNullException(nameof(dice));
            if (IsFull) throw new InvalidOperationException("라인이 가득 차 배치할 수 없다.");
            _dice.Add(dice);
        }

        /// <summary>
        /// 기획서 6번 제거 규칙: 지정한 숫자와 같은 값의 주사위를 모두 제거한다.
        /// 단 특수 주사위(IsSpecial)는 제거 대상이 아니다(기획서 9번).
        /// 제거된 개수를 반환한다.
        /// </summary>
        public int RemoveByValue(int value)
            => _dice.RemoveAll(d => d.Value == value && !d.IsSpecial);

        /// <summary>해당 숫자로 제거 가능한(=특수가 아닌 동일 숫자) 주사위가 있는지.</summary>
        public bool HasRemovableValue(int value)
            => _dice.Any(d => d.Value == value && !d.IsSpecial);

        /// <summary>라인 점수(기획서 7번: 합계 + 더블/트리플 보너스).</summary>
        public int Score() => ScoreCalculator.LineScore(_dice);

        public override string ToString()
            => string.Join(" ", _dice.Select(d => d.ToString()));
    }
}
