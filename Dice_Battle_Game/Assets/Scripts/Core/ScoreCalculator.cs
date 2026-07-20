using System.Collections.Generic;

namespace DiceBattle.Core
{
    /// <summary>
    /// 기획서 7번 점수 계산.
    /// - 라인 점수 = 주사위 숫자의 합
    /// - 더블(같은 숫자 2개): 보너스 = 그 숫자 x 1
    /// - 트리플(같은 숫자 3개): 보너스 = 그 숫자 x 2
    /// 라인 최대 3칸이므로 페어/트리플은 최대 한 종류만 존재한다.
    /// </summary>
    public static class ScoreCalculator
    {
        public static int LineScore(IReadOnlyList<Dice> dice)
        {
            int sum = 0;
            // index 1~6 사용 (0은 미사용)
            var counts = new int[Dice.MaxValue + 1];

            for (int i = 0; i < dice.Count; i++)
            {
                int v = dice[i].Value;
                sum += v;
                counts[v]++;
            }

            int bonus = 0;

            // 트리플 우선 확인
            for (int v = Dice.MinValue; v <= Dice.MaxValue; v++)
            {
                if (counts[v] == 3) { bonus = v * 2; break; }
            }

            // 트리플이 없으면 더블 확인
            if (bonus == 0)
            {
                for (int v = Dice.MinValue; v <= Dice.MaxValue; v++)
                {
                    if (counts[v] == 2) { bonus = v * 1; break; }
                }
            }

            return sum + bonus;
        }
    }
}
