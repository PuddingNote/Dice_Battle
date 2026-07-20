using System;
using System.Collections.Generic;
using DiceBattle.Core;

namespace DiceBattle.AI
{
    /// <summary>
    /// 난이도 "낮음": 합법 수 중 완전 무작위 선택.
    /// System.Random 을 주입해 테스트에서 결정적으로 재현 가능.
    /// </summary>
    public sealed class RandomAiStrategy : IAiStrategy
    {
        private readonly Random _rng;

        public RandomAiStrategy() : this(new Random()) { }
        public RandomAiStrategy(int seed) : this(new Random(seed)) { }
        public RandomAiStrategy(Random rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        public int ChoosePrimaryLine(GameState state, PlayerId me)
        {
            var field = state.Field(me);
            var candidates = new List<int>(Field.LineCount);
            for (int i = 0; i < Field.LineCount; i++)
                if (field[i].HasSpace) candidates.Add(i);

            // 활성 플레이어는 필드에 빈칸이 보장되므로 candidates 는 비지 않는다.
            return candidates[_rng.Next(candidates.Count)];
        }

        public ExtraMove ChooseExtraMove(GameState state, PlayerId me)
        {
            var candidates = new List<ExtraMove>(Field.LineCount * 2);
            PlayerId opp = me.Other();

            for (int i = 0; i < Field.LineCount; i++)
                if (state.Field(me)[i].HasSpace) candidates.Add(new ExtraMove(me, i));
            for (int i = 0; i < Field.LineCount; i++)
                if (state.Field(opp)[i].HasSpace) candidates.Add(new ExtraMove(opp, i));

            return candidates[_rng.Next(candidates.Count)];
        }
    }
}
