using System;
using DiceBattle.Core;

namespace DiceBattle.AI
{
    /// <summary>
    /// 정해진 수를 순서대로 두고, 각본이 끝나면 다른 전략에게 넘기는 AI.
    ///
    /// 튜토리얼 전용이다. 앞부분에서 "상대가 내 주사위를 제거하는 장면" 같은 것을 반드시
    /// 보여줘야 하는데, 진짜 AI에게 맡기면 그 수가 나올지 알 수 없다.
    ///
    /// <b>각본을 그대로 믿지 않는다.</b> 적힌 수가 지금 상황에서 둘 수 없는 수라면
    /// (라인이 가득 찼다거나, 기본 배치 차례인데 추가 배치 수가 적혀 있다거나) 각본을 버리고
    /// 폴백에게 넘긴다. 여기서 잘못된 수를 돌려주면 규칙 엔진이 예외를 던져 판이 멈추는데,
    /// 그게 하필 첫 실행 튜토리얼에서 일어난다.
    /// </summary>
    public sealed class ScriptedAiStrategy : IAiStrategy
    {
        private readonly TutorialAiMove[] _moves;
        private readonly IAiStrategy _fallback;
        private int _index;

        public ScriptedAiStrategy(TutorialAiMove[] moves, IAiStrategy fallback)
        {
            _moves = moves ?? Array.Empty<TutorialAiMove>();
            _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        }

        /// <summary>각본이 아직 남아 있는가.</summary>
        public bool HasScript => _index < _moves.Length;

        public int ChoosePrimaryLine(GameState state, PlayerId me)
        {
            if (!TryTake(isExtra: false, out TutorialAiMove move))
                return _fallback.ChoosePrimaryLine(state, me);

            // 기본 배치는 본인 필드에만 놓을 수 있다.
            if (!state.Field(me)[move.Line].HasSpace)
                return _fallback.ChoosePrimaryLine(state, me);

            return move.Line;
        }

        public ExtraMove ChooseExtraMove(GameState state, PlayerId me)
        {
            if (!TryTake(isExtra: true, out TutorialAiMove move))
                return _fallback.ChooseExtraMove(state, me);

            if (!state.Field(move.Field)[move.Line].HasSpace)
                return _fallback.ChooseExtraMove(state, me);

            return new ExtraMove(move.Field, move.Line);
        }

        /// <summary>
        /// 맨 앞 수를 꺼낸다. 종류가 맞지 않으면 각본이 이미 어긋난 것이므로 통째로 버린다.
        /// 한 수만 건너뛰면 그 뒤가 전부 한 칸씩 밀려 더 이상한 판이 된다.
        /// </summary>
        private bool TryTake(bool isExtra, out TutorialAiMove move)
        {
            move = default;
            if (!HasScript) return false;

            if (_moves[_index].IsExtra != isExtra)
            {
                _index = _moves.Length; // 각본 폐기
                return false;
            }

            move = _moves[_index++];
            return true;
        }
    }
}
