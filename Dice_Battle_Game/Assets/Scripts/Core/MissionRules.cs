namespace DiceBattle.Core
{
    /// <summary>일일 미션이 무엇을 세는가.</summary>
    public enum MissionKind
    {
        /// <summary>판을 끝낸 횟수(승패 무관).</summary>
        PlayMatches,

        /// <summary>내가 제거한 상대 주사위 개수(누적).</summary>
        RemoveDice,

        /// <summary>내가 이긴 라인 수(판을 져도 쌓인다).</summary>
        WinLines,

        /// <summary>리롤한 횟수(기본 + 광고).</summary>
        UseReroll,

        /// <summary>내가 배치한 특수(추가) 주사위 개수.</summary>
        PlaceExtra,

        /// <summary>특수 주사위를 상대 필드에 놓은 횟수.</summary>
        PlaceOnOpponent,

        /// <summary>한 판에서 주사위를 여러 개 한꺼번에 제거한 횟수.</summary>
        BigRemoval
    }

    /// <summary>
    /// 일일 미션 후보와 보상.
    ///
    /// <b>"N판 승리" 같은 조건은 두지 않는다.</b> 낮은 난이도에서 빨리 이기는 것이
    /// 최적이 되어, "이길 수 있는 가장 높은 난이도를 골라라"는 설계를 정면으로 거스른다
    /// (docs/Difficulty.md 6장이 연승 보너스에 대해 적어 둔 것과 같은 문제다).
    ///
    /// 여기 있는 것들은 모두 난이도를 낮춰도 이득이 없다.
    /// 라인 승리는 판을 져도 쌓이므로 승리 파밍과 무관하다.
    /// </summary>
    public static class MissionRules
    {
        /// <summary>하루에 주어지는 미션 수.</summary>
        public const int DailyCount = 3;

        /// <summary><see cref="MissionKind.BigRemoval"/>로 인정되는 한 판 제거 개수.</summary>
        public const int BigRemovalThreshold = 3;

        public readonly struct Mission
        {
            public MissionKind Kind { get; }
            public int Target { get; }

            /// <summary>
            /// 보상 비율(100 = 최대 해금 난이도의 승리 코인 1배).
            /// 정수 배수로는 "조금 더 어려운 미션에 조금 더" 같은 차이를 낼 수 없어 퍼센트로 둔다.
            /// </summary>
            public int RewardPercent { get; }

            public Mission(MissionKind kind, int target, int rewardPercent)
            {
                Kind = kind;
                Target = target;
                RewardPercent = rewardPercent;
            }
        }

        /// <summary>
        /// 미션 후보 전체. <b>순서를 바꾸거나 중간에 끼워 넣지 말 것</b> —
        /// 저장된 그날의 선택이 번호로 기록되어 있어, 순서가 바뀌면 진행 중인 미션이 뒤바뀐다.
        /// 추가할 때는 반드시 끝에 붙인다.
        /// </summary>
        public static readonly Mission[] Pool =
        {
            new Mission(MissionKind.PlayMatches, 3, 100),
            new Mission(MissionKind.PlayMatches, 5, 150),
            new Mission(MissionKind.RemoveDice, 10, 130),
            new Mission(MissionKind.WinLines, 6, 130),
            new Mission(MissionKind.WinLines, 9, 190),
            new Mission(MissionKind.UseReroll, 3, 100),
            new Mission(MissionKind.PlaceExtra, 5, 140),
            new Mission(MissionKind.PlaceOnOpponent, 3, 140),
            new Mission(MissionKind.BigRemoval, 1, 160)
        };

        public static int PoolSize => Pool.Length;

        public static bool IsValidPoolIndex(int index) => index >= 0 && index < Pool.Length;

        /// <summary>
        /// 미션 보상. <b>최대 해금 난이도에 비례한다.</b>
        ///
        /// 고정 액수로 주면 상위 난이도에서는 있으나 마나 하고(보호권이 560인데 보상이 20),
        /// 하위 난이도에서는 하루 수입을 통째로 뒤집는다(수입 50에 보상 40).
        /// 승리 코인에 비례시키면 "미션 하루치 = 승리 몇 번치"가 전 구간 같아진다.
        /// </summary>
        public static int Reward(int poolIndex, DifficultyTier unlockedTier)
        {
            if (!IsValidPoolIndex(poolIndex)) return 0;

            int reward = CoinRules.WinCoins(unlockedTier) * Pool[poolIndex].RewardPercent / 100;
            return reward < 1 ? 1 : reward;
        }

        /// <summary>
        /// 그날의 미션을 뽑는다. 날짜만 있으면 언제 불러도 같은 결과가 나오므로 따로 저장할
        /// 필요가 없다.
        ///
        /// <b>System.Random을 쓰지 않는다.</b> 구현이 런타임마다 다를 수 있어 에디터와
        /// 실기기가 서로 다른 미션을 뽑을 수 있다. 직접 섞으면 어디서든 같은 결과가 나온다.
        ///
        /// 같은 종류는 하루에 하나만 나온다. "3판 플레이"와 "5판 플레이"가 같이 뜨면
        /// 사실상 미션이 둘로 줄어드는 셈이라 재미가 없다.
        /// </summary>
        public static int[] PickForDay(int day)
        {
            var order = new int[Pool.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;

            // Fisher-Yates. 뒤에서부터 훑으며 앞쪽의 임의 위치와 맞바꾼다.
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = (int)(Hash(day, i) % (uint)(i + 1));
                (order[i], order[j]) = (order[j], order[i]);
            }

            var picked = new int[DailyCount];
            var usedKinds = new bool[System.Enum.GetValues(typeof(MissionKind)).Length];
            int count = 0;

            for (int i = 0; i < order.Length && count < DailyCount; i++)
            {
                int kind = (int)Pool[order[i]].Kind;
                if (usedKinds[kind]) continue;

                usedKinds[kind] = true;
                picked[count++] = order[i];
            }

            // 후보가 모자라 못 채운 자리는 앞에서부터 메운다(정상적으로는 일어나지 않는다).
            for (int i = count; i < DailyCount; i++) picked[i] = i % Pool.Length;

            return picked;
        }

        /// <summary>날짜와 자리를 섞어 고르게 퍼진 값을 만든다(플랫폼 무관).</summary>
        private static uint Hash(int day, int salt)
        {
            unchecked
            {
                uint h = (uint)day * 2654435761u + (uint)salt * 40503u + 2166136261u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                h *= 3266489917u;
                h ^= h >> 16;
                return h;
            }
        }

        /// <summary>그날 세 미션을 모두 달성했을 때의 총액(밸런스 확인용).</summary>
        public static int DailyTotal(int day, DifficultyTier unlockedTier)
        {
            int[] picked = PickForDay(day);
            int sum = 0;
            for (int i = 0; i < picked.Length; i++) sum += Reward(picked[i], unlockedTier);
            return sum;
        }
    }
}
