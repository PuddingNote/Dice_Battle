using System;
using System.Collections.Generic;

namespace DiceBattle.Core
{
    /// <summary>
    /// Lv.1~10 난이도 표. 해금 판정의 유일한 기준점이다.
    ///
    /// <b>해금은 반드시 "최고 달성 점수"로 판정한다.</b> 현재 점수로 판정하면
    /// 패배로 점수가 내려갈 때 이미 해금한 난이도가 다시 잠긴다 —
    /// 성취를 도로 빼앗는 셈이라 절대 그렇게 두면 안 된다.
    ///
    /// 표가 깨져 있으면 생성 시점에 예외를 던진다. 잘못된 표로 조용히 굴러가면
    /// 해금이 어긋난 채로 저장까지 되어 되돌리기 어려워지기 때문이다.
    /// 인스펙터 에셋을 읽어 만드는 쪽에서 예외를 받아 코드 기본값으로 되돌린다.
    /// </summary>
    public sealed class DifficultyTable
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 10;
        public const int LevelCount = MaxLevel - MinLevel + 1;

        private readonly DifficultyTier[] _tiers;

        public DifficultyTable(IReadOnlyList<DifficultyTier> tiers)
        {
            if (tiers == null) throw new ArgumentNullException(nameof(tiers));
            if (tiers.Count != LevelCount)
                throw new ArgumentException(
                    $"난이도 표는 정확히 {LevelCount}단계여야 한다(현재 {tiers.Count}).", nameof(tiers));

            _tiers = new DifficultyTier[LevelCount];
            for (int i = 0; i < LevelCount; i++)
            {
                int level = MinLevel + i;
                var t = tiers[i];
                _tiers[i] = new DifficultyTier(level, t.UnlockScore, t.WinPoints, t.LosePoints);
            }

            // Lv1은 시작부터 열려 있어야 한다. 아니면 아무 난이도도 못 고르는 상태가 된다.
            if (_tiers[0].UnlockScore != 0)
                throw new ArgumentException("Lv1의 해금 점수는 0이어야 한다.", nameof(tiers));

            // 해금선이 뒤로 갈수록 낮아지면 상위 난이도가 하위보다 먼저 열린다.
            for (int i = 1; i < LevelCount; i++)
            {
                if (_tiers[i].UnlockScore < _tiers[i - 1].UnlockScore)
                    throw new ArgumentException(
                        $"해금 점수는 단계마다 같거나 커야 한다(Lv{i + MinLevel}).", nameof(tiers));
            }
        }

        public DifficultyTier this[int level] => _tiers[Clamp(level) - MinLevel];

        /// <summary>표 전체(밸런스 확인·표시용).</summary>
        public IReadOnlyList<DifficultyTier> Tiers => _tiers;

        public static int Clamp(int level)
            => level < MinLevel ? MinLevel : (level > MaxLevel ? MaxLevel : level);

        /// <param name="highestScore">최고 달성 점수. 현재 점수가 아니다.</param>
        public bool IsUnlocked(int level, int highestScore)
            => highestScore >= this[level].UnlockScore;

        /// <summary>지금까지 해금한 가장 높은 난이도. 최소 <see cref="MinLevel"/>.</summary>
        public int MaxUnlockedLevel(int highestScore)
        {
            int max = MinLevel;
            for (int level = MinLevel; level <= MaxLevel; level++)
            {
                if (highestScore >= this[level].UnlockScore) max = level;
            }
            return max;
        }

        /// <summary>다음에 해금될 난이도. 전부 해금했으면 null.</summary>
        public int? NextLockedLevel(int highestScore)
        {
            int max = MaxUnlockedLevel(highestScore);
            return max >= MaxLevel ? (int?)null : max + 1;
        }

        /// <summary>다음 해금까지 남은 점수. 전부 해금했으면 0.</summary>
        public int PointsToNextUnlock(int highestScore)
        {
            int? next = NextLockedLevel(highestScore);
            if (next == null) return 0;

            int remain = this[next.Value].UnlockScore - highestScore;
            return remain < 0 ? 0 : remain;
        }

        /// <summary>
        /// 한 판 결과를 반영해 점수·최고 점수·해금을 한 번에 갱신한다.
        ///
        /// 점수가 오른 경우에만 최고 점수가 따라 오르고, 해금은 그 최고 점수로 판정한다.
        /// 그래서 패배해도 이미 열린 난이도는 잠기지 않는다.
        /// </summary>
        /// <param name="playedLevel">
        /// 그 판을 <b>시작할 때 고정된</b> 난이도. 판 도중에 바뀌지 않으므로,
        /// 결과를 반영할 때도 시작 시점의 값을 그대로 넣어야 한다.
        /// </param>
        public ProgressUpdate ApplyMatch(int score, int highestScore, int playedLevel,
            PlayerMatchResult result)
        {
            // 저장된 최고 점수가 현재 점수보다 낮으면(구버전 데이터 이관 직후나 손상)
            // 현재 점수까지 끌어올린다. 그러지 않으면 이미 도달한 해금이 사라진다.
            int highest = highestScore < score ? score : highestScore;

            int before = MaxUnlockedLevel(highest);

            int next = RankSystem.ApplyResult(score, result, this[playedLevel]);
            if (next > highest) highest = next;

            return new ProgressUpdate(next, highest, next - score, before, MaxUnlockedLevel(highest));
        }

        /// <summary>
        /// 난이도 값을 지금 고를 수 있는 범위로 접는다.
        ///
        /// 오프라인 단일 기기 게임이라 저장 데이터 변조를 막을 방법은 없다.
        /// 이건 변조 방어가 아니라, 밸런스 조정으로 해금선이 올라갔거나 값이 손상되어
        /// 고를 수 없는 난이도가 넘어왔을 때 안전하게 접기 위한 것이다.
        /// </summary>
        public int ClampToUnlocked(int level, int highestScore)
        {
            int max = MaxUnlockedLevel(highestScore);
            int clamped = Clamp(level);
            return clamped > max ? max : clamped;
        }
    }
}
