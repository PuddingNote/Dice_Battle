using System;
using NUnit.Framework;
using DiceBattle.Core;
using DiceBattle.AI;

namespace DiceBattle.Tests
{
    /// <summary>
    /// AI의 <b>세기</b>를 지킨다. 다른 테스트가 "규칙대로 두는가"를 본다면
    /// 여기서는 "얼마나 잘 두는가"를 본다.
    ///
    /// 난이도는 이제 오로지 배치 실력에서 나온다(주사위 편향을 없앴다).
    /// 그래서 평가 함수를 손대면 난이도 열 단계가 통째로 움직이는데,
    /// 눈으로는 드러나지 않고 한참 플레이해 봐야 알아챈다. 여기서 잡는다.
    ///
    /// 시드를 고정해 결과가 매번 같다. 판수가 적어 실패가 흔들리는 일은 없고,
    /// 실패한다면 진짜로 세기가 변한 것이다. 여유 폭은 "설계 의도"에 맞춰 잡았으므로
    /// 밸런스를 다시 맞춘 뒤 값이 어긋나면 <b>기대치를 고치기 전에 왜 변했는지부터</b> 볼 것.
    /// </summary>
    public class AiStrengthTests
    {
        /// <summary>공정한 주사위로 games판을 두고 playerOne 쪽 승률을 낸다.</summary>
        private static double WinRate(
            Func<Random, IAiStrategy> one, Func<Random, IAiStrategy> two, int games, int seed)
        {
            var master = new Random(seed);
            int wins = 0;

            for (int i = 0; i < games; i++)
            {
                var rng = new Random(master.Next());
                var game = new DiceGame(new RandomDiceRoller(rng));

                // 선공은 유리하다. 한쪽에 몰아주면 승률이 그만큼 기울어 측정이 흐려진다.
                PlayerId first = i % 2 == 0 ? PlayerId.One : PlayerId.Two;
                MatchOutcome outcome = HeadlessGameRunner.Play(game, first, one(rng), two(rng));

                if (outcome.Winner == PlayerId.One) wins++;
            }

            return wins / (double)games;
        }

        private static Func<Random, IAiStrategy> Best => _ => new HeuristicAiStrategy();
        private static Func<Random, IAiStrategy> Chance => rng => new RandomAiStrategy(rng);
        private static Func<Random, IAiStrategy> Level(int lvl) => rng => new LeveledAiStrategy(lvl, rng);

        [Test]
        public void Best_Play_Beats_Chance_Decisively()
        {
            // 이 게임은 운의 비중이 크다. 그래도 평가가 제 일을 한다면 무작위 배치를
            // 크게 앞서야 한다. 여기가 무너지면 아래 난이도 사다리가 전부 의미를 잃는다.
            double rate = WinRate(Best, Chance, 600, seed: 20260810);

            Assert.Greater(rate, 0.68,
                $"최선 수가 무작위 배치를 이기는 비율이 {rate:P1}밖에 안 된다. 평가 함수를 의심할 것.");
        }

        [Test]
        public void The_Top_Level_Beats_The_Bottom_Level_Decisively()
        {
            // 사다리의 양 끝이 실제로 벌어져 있는지. 예전에는 이 차이의 대부분이
            // 주사위 편향에서 나왔고, 편향을 빼면 열 단계가 9%p 안에 몰려 있었다.
            double rate = WinRate(Level(10), Level(1), 600, seed: 20260811);

            Assert.Greater(rate, 0.70,
                $"Lv10이 Lv1을 이기는 비율이 {rate:P1}다. 난이도 폭이 사라졌다.");
        }

        [Test]
        public void The_Ladder_Climbs_All_The_Way_Up()
        {
            // 같은 상대(Lv10)를 놓고 각 단계가 얼마나 버티는지 잰다.
            // 인접 단계는 차이가 작아 흔들리므로 세 칸씩 건너뛰어 확인한다.
            double lv1 = WinRate(Level(1), Level(10), 500, seed: 20260812);
            double lv4 = WinRate(Level(4), Level(10), 500, seed: 20260812);
            double lv7 = WinRate(Level(7), Level(10), 500, seed: 20260812);
            double lv10 = WinRate(Level(10), Level(10), 500, seed: 20260812);

            Assert.Less(lv1, lv4, $"Lv1({lv1:P1})이 Lv4({lv4:P1})보다 잘 버틴다.");
            Assert.Less(lv4, lv7, $"Lv4({lv4:P1})가 Lv7({lv7:P1})보다 잘 버틴다.");
            Assert.Less(lv7, lv10, $"Lv7({lv7:P1})이 Lv10({lv10:P1})보다 잘 버틴다.");
        }

        [Test]
        public void The_Weakest_Level_Still_Loses_To_Nobody_By_Default()
        {
            // Lv1은 쉬워야 하지만 "가만히 있어도 지는" 상대여선 안 된다.
            // 무작위 배치보다는 확실히 약하고(=쉽고), 그렇다고 압도당하지는 않는 자리.
            double rate = WinRate(Level(1), Chance, 600, seed: 20260813);

            Assert.Less(rate, 0.50, $"Lv1이 무작위 배치를 {rate:P1}로 이긴다. 가장 쉬운 단계가 아니다.");
            Assert.Greater(rate, 0.20, $"Lv1의 승률이 {rate:P1}다. 이 정도면 상대가 아니라 구경거리다.");
        }

        // ---- 평가가 실제로 무엇을 보는지 (결정적) ----

        private static GameState Pending(int value, bool special = false)
        {
            var s = new GameState();
            s.CurrentPlayer = PlayerId.One;
            s.Phase = TurnPhase.AwaitingPrimaryPlacement;
            s.PendingDice = new Dice(value, special, PlayerId.One);
            return s;
        }

        [Test]
        public void It_Takes_A_Removal_That_Breaks_A_Losing_Line()
        {
            // 0줄: 상대가 트리플 6(점수 30)로 크게 앞선다. 여기에 6을 놓으면
            // 셋 다 사라지고 그 줄이 되살아난다. 자기 주사위 하나를 잃는 대가는 싸다.
            var s = Pending(6);
            for (int i = 0; i < 3; i++) s.Field(PlayerId.Two)[0].Add(new Dice(6, false, PlayerId.Two));
            s.Field(PlayerId.One)[1].Add(new Dice(6, false, PlayerId.One));
            s.Field(PlayerId.One)[2].Add(new Dice(6, false, PlayerId.One));

            int pick = new HeuristicAiStrategy().ChoosePrimaryLine(s, PlayerId.One);

            Assert.AreEqual(0, pick, "상대의 트리플을 지울 수 있는데 다른 줄에 놓았다.");
        }

        [Test]
        public void It_Does_Not_Pile_Onto_A_Line_It_Already_Won()
        {
            // 0줄은 이미 크게 이겼고(상대 0점), 1줄은 지고 있다.
            // 승패는 라인 승수로 나므로 이긴 줄에 더 쌓는 것은 버리는 수다.
            var s = Pending(6);
            s.Field(PlayerId.One)[0].Add(new Dice(6, false, PlayerId.One));
            s.Field(PlayerId.One)[0].Add(new Dice(6, false, PlayerId.One));
            s.Field(PlayerId.Two)[1].Add(new Dice(5, false, PlayerId.Two));
            s.Field(PlayerId.Two)[1].Add(new Dice(5, false, PlayerId.Two));

            int pick = new HeuristicAiStrategy().ChoosePrimaryLine(s, PlayerId.One);

            Assert.AreNotEqual(0, pick, "이미 이긴 줄에 또 쌓았다. 총점이 아니라 라인 승수로 이긴다.");
        }

        [Test]
        public void Worst_Play_Is_The_Mirror_Of_Best_Play()
        {
            // 하위 난이도의 "일부러 나쁜 수"가 최선 수와 같은 곳을 고르면
            // 난이도를 낮추는 손잡이가 헛돈다.
            var s = Pending(6);
            for (int i = 0; i < 3; i++) s.Field(PlayerId.Two)[0].Add(new Dice(6, false, PlayerId.Two));
            s.Field(PlayerId.One)[1].Add(new Dice(6, false, PlayerId.One));
            s.Field(PlayerId.One)[2].Add(new Dice(6, false, PlayerId.One));

            var ai = new HeuristicAiStrategy();

            Assert.AreNotEqual(
                ai.ChoosePrimaryLine(s, PlayerId.One),
                ai.WorstPrimaryLine(s, PlayerId.One),
                "최선과 최악이 같은 줄을 골랐다.");
        }

        [Test]
        public void The_Extra_Die_Goes_Somewhere_Legal_On_Either_Field()
        {
            // 추가 주사위는 양쪽 필드 어디에나 놓을 수 있다(기획서 6번).
            // 최선/최악 어느 쪽이든 가득 찬 줄을 고르면 규칙 엔진이 예외를 던진다.
            var s = new GameState();
            s.CurrentPlayer = PlayerId.One;
            s.Phase = TurnPhase.AwaitingExtraPlacement;
            s.PendingDice = new Dice(3, true, PlayerId.One);

            // 양쪽 0줄을 가득 채워 둔다.
            for (int i = 0; i < 3; i++)
            {
                s.Field(PlayerId.One)[0].Add(new Dice(2, false, PlayerId.One));
                s.Field(PlayerId.Two)[0].Add(new Dice(2, false, PlayerId.Two));
            }

            var ai = new HeuristicAiStrategy();

            foreach (ExtraMove move in new[]
            {
                ai.ChooseExtraMove(s, PlayerId.One),
                ai.WorstExtraMove(s, PlayerId.One),
            })
            {
                Assert.IsTrue(s.Field(move.Field)[move.Line].HasSpace,
                    $"가득 찬 줄을 골랐다: {move.Field} {move.Line}줄");
            }
        }
    }
}
