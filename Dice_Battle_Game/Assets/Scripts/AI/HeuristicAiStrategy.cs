using DiceBattle.Core;

namespace DiceBattle.AI
{
    /// <summary>
    /// AI의 "최선 수"를 담당하는 평가기(결정적). 난이도는
    /// <see cref="LeveledAiStrategy"/>가 이 최선 수와 최악 수, 무작위 수를 섞어 만든다.
    ///
    /// <b>평가 기준은 "3줄 중 2줄을 이길 확률"이다.</b> 승패 판정이 라인 승수로
    /// 나기 때문이다(기획서 8번). 점수 합을 키우는 평가로는 이미 크게 이긴 줄에
    /// 계속 투자해 총점만 높고 라인 승수는 지는 판을 만든다.
    ///
    /// 줄마다 "지금 점수 + 빈칸 × 칸당 기대 득점"으로 최종 점수를 어림하고,
    /// 그 차이를 승리 확률로 바꾼 뒤 세 줄을 독립으로 보고 2줄 이상 이길 확률을 낸다.
    /// 세 줄이 실제로 독립은 아니지만(주사위를 서로 나눠 갖는다), 어느 배치가 더 나은지
    /// 고르는 데는 충분하고 한 수 두는 비용이 무시할 만하다.
    /// </summary>
    public sealed class HeuristicAiStrategy : IAiStrategy
    {
        /// <summary>
        /// 빈칸 하나가 앞으로 벌어들일 기대 점수. 주사위 눈 기대값 3.5에
        /// 더블/트리플 보너스 기대분을 얹은 값이다.
        /// </summary>
        private const double PerEmptySlot = 3.9;

        /// <summary>
        /// 점수 차를 승률로 바꿀 때의 완만함. 작을수록 작은 점수 차에도 승패를
        /// 확신하고, 크면 끝까지 팽팽하다고 본다.
        /// </summary>
        private const double Softness = 6.0;

        /// <summary>
        /// 제거에 성공했을 때 얹는 값(승률 단위, -1~1 척도).
        /// 제거하면 특수 주사위를 하나 더 놓는다 — 그 이득은 이 평가에 안 잡힌다.
        /// 실제로 한 수 더 내다보는 것과 이 상수 하나를 얹는 것의 차이는 승률 1%p라
        /// 계산이 싼 쪽을 택했다. 0.15~0.45 구간에서 결과가 거의 같아 값에 예민하지 않다.
        /// </summary>
        private const double RemovalBonus = 0.20;

        public int ChoosePrimaryLine(GameState state, PlayerId me)
            => PickPrimary(state, me, best: true);

        /// <summary>일부러 가장 나쁜 라인을 고른다(하위 난이도용).</summary>
        public int WorstPrimaryLine(GameState state, PlayerId me)
            => PickPrimary(state, me, best: false);

        public ExtraMove ChooseExtraMove(GameState state, PlayerId me)
            => PickExtra(state, me, best: true);

        /// <summary>일부러 가장 나쁜 추가 배치를 고른다(하위 난이도용).</summary>
        public ExtraMove WorstExtraMove(GameState state, PlayerId me)
            => PickExtra(state, me, best: false);

        // ---- 기본 배치 ----

        private int PickPrimary(GameState state, PlayerId me, bool best)
        {
            int value = state.PendingDice.Value;
            bool special = state.PendingDice.IsSpecial;
            Field mine = state.Field(me);
            Field theirs = state.Field(me.Other());

            int pick = -1;
            double pickScore = 0;

            for (int line = 0; line < Field.LineCount; line++)
            {
                if (!mine[line].HasSpace) continue;

                double score = EvaluatePrimary(mine, theirs, line, value, special);
                if (pick < 0 || (best ? score > pickScore : score < pickScore))
                {
                    pickScore = score;
                    pick = line;
                }
            }

            return pick;
        }

        /// <summary>라인 line에 value를 놓았을 때의 승률 우위.</summary>
        private static double EvaluatePrimary(Field mine, Field theirs, int line, int value, bool special)
        {
            var board = Board.Snapshot(mine, theirs);

            int removable = AiScoring.CountRemovable(theirs[line], value);
            bool removal = removable > 0;

            if (removal)
            {
                // 상호 소멸: 상대의 같은 숫자가 사라진다.
                board.OppScore[line] = AiScoring.LineScoreWithoutValue(theirs[line], value);
                board.OppCount[line] -= removable;

                // 배치한 주사위도 함께 사라진다 — 특수 주사위만 면역이라 남는다(기획서 9번).
                if (special)
                {
                    board.MyScore[line] = AiScoring.LineScoreWith(mine[line], value);
                    board.MyCount[line]++;
                }
            }
            else
            {
                board.MyScore[line] = AiScoring.LineScoreWith(mine[line], value);
                board.MyCount[line]++;
            }

            double score = board.WinEdge();
            if (removal) score += RemovalBonus;
            return score;
        }

        // ---- 추가(특수) 주사위 배치 ----

        private ExtraMove PickExtra(GameState state, PlayerId me, bool best)
        {
            int value = state.PendingDice.Value;
            PlayerId opp = me.Other();
            Field mine = state.Field(me);
            Field theirs = state.Field(opp);

            ExtraMove pick = default;
            double pickScore = 0;
            bool found = false;

            // 본인 필드와 상대 필드 양쪽이 후보다. 상대 칸을 낮은 눈으로 막는 방해 배치는
            // 따로 가중치를 주지 않아도, 상대의 남은 빈칸이 줄어 승률 계산에 저절로 잡힌다.
            for (int side = 0; side < 2; side++)
            {
                bool ownField = side == 0;
                Field target = ownField ? mine : theirs;

                for (int line = 0; line < Field.LineCount; line++)
                {
                    if (!target[line].HasSpace) continue;

                    var board = Board.Snapshot(mine, theirs);
                    if (ownField)
                    {
                        board.MyScore[line] = AiScoring.LineScoreWith(mine[line], value);
                        board.MyCount[line]++;
                    }
                    else
                    {
                        board.OppScore[line] = AiScoring.LineScoreWith(theirs[line], value);
                        board.OppCount[line]++;
                    }

                    double score = board.WinEdge();
                    if (!found || (best ? score > pickScore : score < pickScore))
                    {
                        pickScore = score;
                        pick = new ExtraMove(ownField ? me : opp, line);
                        found = true;
                    }
                }
            }

            return pick;
        }

        // ---- 평가용 보드 요약 ----

        /// <summary>
        /// 평가에 필요한 것은 줄별 "현재 점수"와 "찬 칸 수"뿐이다.
        /// 주사위 목록을 통째로 복사하지 않고 이 네 배열만 들고 다닌다.
        /// </summary>
        private struct Board
        {
            public int[] MyScore;
            public int[] OppScore;
            public int[] MyCount;
            public int[] OppCount;

            public static Board Snapshot(Field mine, Field theirs)
            {
                var b = new Board
                {
                    MyScore = new int[Field.LineCount],
                    OppScore = new int[Field.LineCount],
                    MyCount = new int[Field.LineCount],
                    OppCount = new int[Field.LineCount],
                };

                for (int i = 0; i < Field.LineCount; i++)
                {
                    b.MyScore[i] = mine[i].Score();
                    b.OppScore[i] = theirs[i].Score();
                    b.MyCount[i] = mine[i].Count;
                    b.OppCount[i] = theirs[i].Count;
                }

                return b;
            }

            /// <summary>2줄 이상 이길 확률 - 그러지 못할 확률. 범위는 -1~1.</summary>
            public double WinEdge()
            {
                var lineWin = new double[Field.LineCount];
                for (int i = 0; i < Field.LineCount; i++)
                {
                    double mineFinal = MyScore[i] + (Line.Capacity - MyCount[i]) * PerEmptySlot;
                    double theirsFinal = OppScore[i] + (Line.Capacity - OppCount[i]) * PerEmptySlot;
                    lineWin[i] = Sigmoid((mineFinal - theirsFinal) / Softness);
                }

                // 줄 3개의 승패 조합 8가지를 그대로 세어 2줄 이상 이길 확률을 낸다.
                double win = 0;
                for (int mask = 0; mask < 8; mask++)
                {
                    double probability = 1;
                    int wins = 0;

                    for (int i = 0; i < Field.LineCount; i++)
                    {
                        bool takesLine = (mask & (1 << i)) != 0;
                        probability *= takesLine ? lineWin[i] : 1 - lineWin[i];
                        if (takesLine) wins++;
                    }

                    if (wins >= 2) win += probability;
                }

                return win - (1 - win);
            }

            /// <summary>점수 차를 0~1의 승률로 눌러 담는다.</summary>
            private static double Sigmoid(double x) => 0.5 + 0.5 * System.Math.Tanh(x);
        }
    }
}
