using System;
using System.Collections.Generic;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    /// <summary>
    /// 테스트용 결정적 롤러: 미리 지정한 값을 순서대로 반환하고,
    /// 소진되면 기본값(1)을 반환한다.
    /// (턴 종료 시 엔진이 다음 턴 주사위를 미리 굴리므로, 관심 없는 후행 값은 기본값 처리)
    /// </summary>
    public sealed class QueueDiceRoller : IDiceRoller
    {
        private const int DefaultValue = 1;
        private readonly Queue<int> _values;

        public QueueDiceRoller(params int[] values)
        {
            _values = new Queue<int>(values ?? Array.Empty<int>());
        }

        public int Roll(PlayerId player) => _values.Count > 0 ? _values.Dequeue() : DefaultValue;
    }
}
