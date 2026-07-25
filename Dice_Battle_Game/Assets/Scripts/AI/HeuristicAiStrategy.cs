using DiceBattle.Core;

namespace DiceBattle.AI
{
    /// <summary>
    /// 난이도 "보통": 간단한 규칙 기반(결정적).
    /// 상호 소멸 규칙(같은 숫자 배치 시 배치 주사위도 소멸)을 반영해,
    /// 각 배치의 "실제 보드 결과(이기는 라인 수 + 점수 마진)"를 평가해 최선을 고른다.
    /// - 제거는 상대의 강한 라인을 정리해 이득일 때만 자연스럽게 선택된다.
    /// - 제거 발생 시 추가 특수 주사위를 얻는 이점도 가중치로 반영.
    /// </summary>
    public sealed class HeuristicAiStrategy : IAiStrategy
    {
        // 제거로 추가 특수 주사위를 1개 더 놓을 수 있는 가치(대략치, 점수차 스케일).
        private const double ExtraDieValue = 8.0;
        private const double WinBonus = 6.0;
        // 가장 뒤진 "포기 라인"에 투자하는 것을 억제하는 페널티(2라인 집중 전략).
        private const double SacrificePenalty = 8.0;

        public int ChoosePrimaryLine(GameState state, PlayerId me)
            => PickPrimary(state, me, best: true);

        /// <summary>일부러 가장 나쁜 라인을 고른다(하위 난이도용).</summary>
        public int WorstPrimaryLine(GameState state, PlayerId me)
            => PickPrimary(state, me, best: false);

        private int PickPrimary(GameState state, PlayerId me, bool best)
        {
            double[] scores = ScorePrimaryLines(state, me);
            int pick = -1;
            double bestVal = best ? double.NegativeInfinity : double.PositiveInfinity;
            for (int i = 0; i < scores.Length; i++)
            {
                if (double.IsNaN(scores[i])) continue; // 배치 불가 라인
                if (best ? scores[i] > bestVal : scores[i] < bestVal)
                {
                    bestVal = scores[i];
                    pick = i;
                }
            }
            return pick;
        }

        /// <summary>각 라인 배치 점수(배치 불가 라인은 NaN).</summary>
        private double[] ScorePrimaryLines(GameState state, PlayerId me)
        {
            int v = state.PendingDice.Value;
            bool special = state.PendingDice.IsSpecial;
            PlayerId opp = me.Other();
            Field myField = state.Field(me);
            Field oppField = state.Field(opp);

            int sacrifice = WorstLine(myField, oppField);

            var scores = new double[Field.LineCount];
            for (int i = 0; i < Field.LineCount; i++)
            {
                if (!myField[i].HasSpace) { scores[i] = double.NaN; continue; }

                double score = EvaluatePrimary(state, me, opp, i, v, special);
                if (i == sacrifice) score -= SacrificePenalty; // 포기 라인 투자 억제
                scores[i] = score;
            }
            return scores;
        }

        private static void SortDescending(int[] a)
        {
            for (int i = 0; i < a.Length; i++)
                for (int j = i + 1; j < a.Length; j++)
                    if (a[j] > a[i]) { int t = a[i]; a[i] = a[j]; a[j] = t; }
        }

        /// <summary>현재 가장 뒤진(내 점수-상대 점수가 최소인) 라인 = 포기 후보.</summary>
        private static int WorstLine(Field myField, Field oppField)
        {
            int worst = 0;
            int worstDiff = int.MaxValue;
            for (int i = 0; i < Field.LineCount; i++)
            {
                int diff = myField[i].Score() - oppField[i].Score();
                if (diff < worstDiff)
                {
                    worstDiff = diff;
                    worst = i;
                }
            }
            return worst;
        }

        /// <summary>라인 i에 값 v를 배치했을 때의 보드 평가(이기는 라인 수*W + 점수 마진 + 제거 이점).</summary>
        private double EvaluatePrimary(GameState state, PlayerId me, PlayerId opp, int i, int v, bool special)
        {
            Field myField = state.Field(me);
            Field oppField = state.Field(opp);

            int removable = AiScoring.CountRemovable(oppField[i], v);
            bool removal = removable > 0;

            // 배치 후 내 3라인 점수 및 상대 대비 우열
            var my = new int[Field.LineCount];
            var op = new int[Field.LineCount];
            for (int j = 0; j < Field.LineCount; j++)
            {
                my[j] = myField[j].Score();
                op[j] = oppField[j].Score();
            }

            if (removal)
            {
                op[i] = AiScoring.LineScoreWithoutValue(oppField[i], v);
                if (special) my[i] = AiScoring.LineScoreWith(myField[i], v);
            }
            else
            {
                my[i] = AiScoring.LineScoreWith(myField[i], v);
            }

            // 목적: 내 "상위 2개 라인 점수 합" 최대화(더블/트리플 뭉치기로 2라인 집중).
            // + 이미 상대를 이긴 라인 보너스(초반 노이즈를 줄이려 가중치는 작게).
            var mine = new int[Field.LineCount];
            int wins = 0;
            for (int j = 0; j < Field.LineCount; j++)
            {
                mine[j] = my[j];
                if (my[j] > op[j]) wins++;
            }
            SortDescending(mine);
            double top2 = mine[0] + mine[1];

            double score = top2 + wins * WinBonus;
            if (removal) score += ExtraDieValue;
            return score;
        }

        public ExtraMove ChooseExtraMove(GameState state, PlayerId me)
        {
            int v = state.PendingDice.Value;
            PlayerId opp = me.Other();
            Field myField = state.Field(me);
            Field oppField = state.Field(opp);

            ExtraMove best = default;
            double bestScore = double.NegativeInfinity;
            bool found = false;

            // 본인 필드 + 상대 필드의 모든 배치 후보를 평가해 가장 좋은 곳을 고른다.
            for (int i = 0; i < Field.LineCount; i++)
            {
                if (myField[i].HasSpace)
                {
                    double sc = EvaluateExtra(state, me, me, i, v);
                    if (!found || sc > bestScore) { bestScore = sc; best = new ExtraMove(me, i); found = true; }
                }
                if (oppField[i].HasSpace)
                {
                    double sc = EvaluateExtra(state, me, opp, i, v);
                    if (!found || sc > bestScore) { bestScore = sc; best = new ExtraMove(opp, i); found = true; }
                }
            }
            return best;
        }

        // 추가(특수) 주사위를 (field, line)에 놓았을 때의 가치.
        // 본인 필드: 이기는 라인/점수 향상. 상대 필드: 낮은 값으로 칸을 막아 상대 잠재점수를 깎는 이득(방해).
        private double EvaluateExtra(GameState state, PlayerId me, PlayerId field, int line, int v)
        {
            PlayerId opp = me.Other();
            Field myField = state.Field(me);
            Field oppField = state.Field(opp);

            var my = new int[Field.LineCount];
            var op = new int[Field.LineCount];
            for (int j = 0; j < Field.LineCount; j++)
            {
                my[j] = myField[j].Score();
                op[j] = oppField[j].Score();
            }

            if (field == me) my[line] = AiScoring.LineScoreWith(myField[line], v);
            else op[line] = AiScoring.LineScoreWith(oppField[line], v);

            int wins = 0;
            double margin = 0;
            for (int j = 0; j < Field.LineCount; j++)
            {
                if (my[j] > op[j]) wins++;
                margin += my[j] - op[j];
            }
            double score = wins * WinBonus * 10 + margin;

            if (field == opp)
            {
                // 상대 칸을 막는 이득: 낮은 값일수록 크고, 상대가 이기는(잠재적) 라인일수록 가치.
                double denial = (Dice.MaxValue + Dice.MinValue) * 0.5 - v; // (3.5 - v)
                bool oppAheadHere = oppField[line].Score() > myField[line].Score();
                score += denial * (oppAheadHere ? 8.0 : 3.0);
            }
            return score;
        }
    }
}
