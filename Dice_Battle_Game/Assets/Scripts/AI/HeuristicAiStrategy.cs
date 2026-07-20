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
        {
            int v = state.PendingDice.Value;
            bool special = state.PendingDice.IsSpecial;
            PlayerId opp = me.Other();
            Field myField = state.Field(me);
            Field oppField = state.Field(opp);

            int sacrifice = WorstLine(myField, oppField);

            int bestLine = -1;
            double bestScore = double.NegativeInfinity;

            for (int i = 0; i < Field.LineCount; i++)
            {
                if (!myField[i].HasSpace) continue;

                double score = EvaluatePrimary(state, me, opp, i, v, special);
                if (i == sacrifice) score -= SacrificePenalty; // 포기 라인 투자 억제
                if (score > bestScore)
                {
                    bestScore = score;
                    bestLine = i;
                }
            }
            return bestLine;
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

            // 본인 필드에 여유가 있으면 본인 필드에 배치(이기는 라인 수/점수 최대화).
            if (myField.HasSpace)
            {
                int bestLine = -1;
                int bestWins = -1;
                int bestScore = -1;
                for (int i = 0; i < Field.LineCount; i++)
                {
                    if (!myField[i].HasSpace) continue;

                    int wins = AiScoring.MyLineWinsAfter(state, me, me, i, v);
                    int score = AiScoring.LineScoreWith(myField[i], v);
                    if (wins > bestWins ||
                        (wins == bestWins && score > bestScore))
                    {
                        bestLine = i;
                        bestWins = wins;
                        bestScore = score;
                    }
                }
                return new ExtraMove(me, bestLine);
            }

            // 본인 필드가 가득 찼으면 상대 필드에 배치 — 상대에게 도움이 가장 적은(추가 점수 최소) 라인.
            int worstLine = -1;
            int worstOppScore = int.MaxValue;
            for (int i = 0; i < Field.LineCount; i++)
            {
                if (!oppField[i].HasSpace) continue;

                int oppScore = AiScoring.LineScoreWith(oppField[i], v);
                if (oppScore < worstOppScore)
                {
                    worstOppScore = oppScore;
                    worstLine = i;
                }
            }
            return new ExtraMove(opp, worstLine);
        }
    }
}
