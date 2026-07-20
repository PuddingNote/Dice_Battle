using DiceBattle.Core;

namespace DiceBattle.AI
{
    /// <summary>
    /// 난이도 "보통": 간단한 규칙 기반.
    /// - 기본 배치: 제거가 가능한 라인을 최우선(제거 수가 많을수록 우선, 특수 주사위 추가 획득 이득).
    ///   제거가 불가능하면 배치 후 "이기는 라인 수"를 최대화하고, 동률이면 해당 라인 점수를 높이는 쪽.
    /// - 추가 배치: 가능하면 본인 필드에 놓아 이기는 라인 수/점수를 최대화.
    ///   본인 필드가 가득 찼을 때만 상대 필드에 놓되, 상대에게 도움이 가장 적은 라인 선택.
    /// 결정적(무작위 없음)이라 테스트가 용이하다.
    /// </summary>
    public sealed class HeuristicAiStrategy : IAiStrategy
    {
        public int ChoosePrimaryLine(GameState state, PlayerId me)
        {
            int v = state.PendingDice.Value;
            PlayerId opp = me.Other();
            Field myField = state.Field(me);
            Field oppField = state.Field(opp);

            // 1) 제거 우선: 상대 같은 라인에서 제거 가능한 수가 가장 많은 라인.
            int bestRemovalLine = -1;
            int bestRemovalCount = 0;
            int bestRemovalOwnScore = -1;
            for (int i = 0; i < Field.LineCount; i++)
            {
                if (!myField[i].HasSpace) continue;
                int removable = AiScoring.CountRemovable(oppField[i], v);
                if (removable <= 0) continue;

                int ownScore = AiScoring.LineScoreWith(myField[i], v);
                if (removable > bestRemovalCount ||
                    (removable == bestRemovalCount && ownScore > bestRemovalOwnScore))
                {
                    bestRemovalLine = i;
                    bestRemovalCount = removable;
                    bestRemovalOwnScore = ownScore;
                }
            }
            if (bestRemovalLine >= 0)
                return bestRemovalLine;

            // 2) 제거 불가: 이기는 라인 수 최대화 → 동률이면 라인 점수 최대화.
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
            return bestLine;
        }

        public ExtraMove ChooseExtraMove(GameState state, PlayerId me)
        {
            int v = state.PendingDice.Value;
            PlayerId opp = me.Other();
            Field myField = state.Field(me);
            Field oppField = state.Field(opp);

            // 본인 필드에 여유가 있으면 본인 필드에 배치(이기는 라인 수/점수 최대화).
            bool ownHasSpace = myField.HasSpace;
            if (ownHasSpace)
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
