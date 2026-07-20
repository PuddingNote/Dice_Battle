using System;
using DiceBattle.Core;

namespace DiceBattle.AI
{
    /// <summary>
    /// UI 없이 두 전략으로 게임을 끝까지 자동 진행한다(AI 자기대전/테스트/시뮬레이션용).
    /// 게임은 규칙상 반드시 종료되지만, 전략 버그로 인한 무한 진행을 막기 위해
    /// 배치 횟수 상한(guard)을 둔다.
    /// </summary>
    public static class HeadlessGameRunner
    {
        public static MatchOutcome Play(
            DiceGame game, PlayerId first, IAiStrategy playerOne, IAiStrategy playerTwo,
            int maxPlacements = 100000)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            if (playerOne == null) throw new ArgumentNullException(nameof(playerOne));
            if (playerTwo == null) throw new ArgumentNullException(nameof(playerTwo));

            game.Start(first);

            int guard = 0;
            while (!game.State.IsGameOver)
            {
                if (++guard > maxPlacements)
                    throw new InvalidOperationException("배치 횟수 상한 초과(전략 버그 의심).");

                PlayerId me = game.State.CurrentPlayer;
                IAiStrategy strat = me == PlayerId.One ? playerOne : playerTwo;

                switch (game.State.Phase)
                {
                    case TurnPhase.AwaitingPrimaryPlacement:
                        game.PlacePrimary(strat.ChoosePrimaryLine(game.State, me));
                        break;

                    case TurnPhase.AwaitingExtraPlacement:
                        ExtraMove mv = strat.ChooseExtraMove(game.State, me);
                        game.PlaceExtra(mv.Field, mv.Line);
                        break;

                    default:
                        throw new InvalidOperationException($"예상치 못한 페이즈: {game.State.Phase}");
                }
            }

            return game.Outcome.Value;
        }
    }
}
