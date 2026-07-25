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

        /// <summary>
        /// 주사위를 배치. 라인이 가득 찼으면 예외.
        /// 같은 값의 주사위가 이미 있으면 그 뒤(마지막 동일 값 다음)에 삽입해
        /// 같은 눈끼리 인접하도록 그룹화한다(더블/트리플 가독성). 순서는 점수/제거에 영향 없음.
        /// </summary>
        public void Add(Dice dice)
        {
            if (dice == null) throw new ArgumentNullException(nameof(dice));
            if (IsFull) throw new InvalidOperationException("라인이 가득 차 배치할 수 없다.");

            int insert = _dice.Count;
            for (int i = _dice.Count - 1; i >= 0; i--)
            {
                if (_dice[i].Value == dice.Value) { insert = i + 1; break; }
            }
            _dice.Insert(insert, dice);
        }

        /// <summary>Add가 dice 값을 삽입할 위치(그룹화 규칙). 배치 연출 계산용.</summary>
        public int InsertIndexFor(int value)
        {
            for (int i = _dice.Count - 1; i >= 0; i--)
                if (_dice[i].Value == value) return i + 1;
            return _dice.Count;
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
