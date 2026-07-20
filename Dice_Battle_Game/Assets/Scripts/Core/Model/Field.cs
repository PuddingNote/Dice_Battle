using System.Collections.Generic;

namespace DiceBattle.Core
{
    /// <summary>
    /// 한 플레이어의 필드(3개 라인 = 3x3). 기획서 4번.
    /// </summary>
    public sealed class Field
    {
        public const int LineCount = 3;

        public PlayerId Owner { get; }

        private readonly Line[] _lines;
        public IReadOnlyList<Line> Lines => _lines;

        public Field(PlayerId owner)
        {
            Owner = owner;
            _lines = new Line[LineCount];
            for (int i = 0; i < LineCount; i++)
                _lines[i] = new Line();
        }

        public Line this[int lineIndex] => _lines[lineIndex];

        /// <summary>모든 라인이 가득 찼는지(=필드 9칸 전부 채워짐).</summary>
        public bool IsFull
        {
            get
            {
                for (int i = 0; i < LineCount; i++)
                    if (!_lines[i].IsFull) return false;
                return true;
            }
        }

        public bool HasSpace => !IsFull;

        /// <summary>총 배치된 주사위 수(0~9).</summary>
        public int DiceCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < LineCount; i++) n += _lines[i].Count;
                return n;
            }
        }
    }
}
