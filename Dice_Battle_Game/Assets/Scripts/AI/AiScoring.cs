using System.Collections.Generic;
using DiceBattle.Core;

namespace DiceBattle.AI
{
    /// <summary>AI 휴리스틱용 평가 헬퍼(Core 규칙을 재사용).</summary>
    internal static class AiScoring
    {
        /// <summary>해당 라인에 addedValue 주사위를 하나 더 놓았을 때의 가상 라인 점수.</summary>
        public static int LineScoreWith(Line line, int addedValue)
        {
            var tmp = new List<Dice>(line.Count + 1);
            for (int i = 0; i < line.Dice.Count; i++) tmp.Add(line.Dice[i]);
            // 특수 여부는 점수 계산에 영향이 없으므로 임의 값 사용.
            tmp.Add(new Dice(addedValue, false, PlayerId.One));
            return ScoreCalculator.LineScore(tmp);
        }

        /// <summary>해당 라인에서 value(특수 제외)를 모두 제거했다고 가정한 라인 점수.</summary>
        public static int LineScoreWithoutValue(Line line, int value)
        {
            var tmp = new List<Dice>(line.Count);
            for (int i = 0; i < line.Dice.Count; i++)
            {
                var d = line.Dice[i];
                if (d.Value == value && !d.IsSpecial) continue; // 제거 대상
                tmp.Add(d);
            }
            return ScoreCalculator.LineScore(tmp);
        }

        /// <summary>해당 라인에서 value 로 제거 가능한(=특수 아님) 주사위 개수.</summary>
        public static int CountRemovable(Line line, int value)
        {
            int c = 0;
            for (int i = 0; i < line.Dice.Count; i++)
            {
                var d = line.Dice[i];
                if (d.Value == value && !d.IsSpecial) c++;
            }
            return c;
        }
    }
}
