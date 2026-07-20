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

        /// <summary>
        /// (me 기준) 지정 필드/라인에 addedValue 를 놓는다고 가정했을 때
        /// me 가 "이기는(점수 우위)" 라인의 개수. 그리디 평가에 사용.
        /// (제거 효과는 반영하지 않는다 — 제거는 상위 로직에서 별도 우선 처리)
        /// </summary>
        public static int MyLineWinsAfter(GameState s, PlayerId me, PlayerId targetField, int line, int addedValue)
        {
            PlayerId opp = me.Other();
            int wins = 0;

            for (int i = 0; i < Field.LineCount; i++)
            {
                int myScore = s.Field(me)[i].Score();
                int oppScore = s.Field(opp)[i].Score();

                if (i == line)
                {
                    if (targetField == me)
                        myScore = LineScoreWith(s.Field(me)[i], addedValue);
                    else
                        oppScore = LineScoreWith(s.Field(opp)[i], addedValue);
                }

                if (myScore > oppScore) wins++;
            }

            return wins;
        }
    }
}
