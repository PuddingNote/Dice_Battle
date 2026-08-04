using System;
using System.Collections.Generic;
using NUnit.Framework;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    /// <summary>
    /// 난이도 표와 해금 판정.
    ///
    /// 수치는 아직 확정 전이므로, 여기서는 <b>임시 수치를 검증하지 않는다</b>.
    /// 표를 직접 만들어 규칙만 확인하고, 공식 생성기는 "어떤 상수를 넣어도
    /// 성립해야 하는 성질"만 본다. 그래야 밸런스를 바꿔도 테스트가 살아남는다.
    /// </summary>
    public class DifficultyTableTests
    {
        /// <summary>해금선만 지정한 10단계 표(나머지 수치는 판정에 관여하지 않는다).</summary>
        private static DifficultyTable TableWithUnlocks(params int[] unlockScores)
        {
            var tiers = new List<DifficultyTier>();
            for (int i = 0; i < unlockScores.Length; i++)
                tiers.Add(new DifficultyTier(i + 1, unlockScores[i], 10, 5));
            return new DifficultyTable(tiers);
        }

        private static DifficultyTable SampleTable()
            => TableWithUnlocks(0, 100, 200, 300, 400, 500, 600, 700, 800, 900);

        // ---- 표 자체의 유효성 ----

        [Test]
        public void Table_Must_Have_Exactly_Ten_Tiers()
        {
            Assert.Throws<ArgumentException>(() => TableWithUnlocks(0, 100, 200));
            Assert.DoesNotThrow(() => SampleTable());
        }

        [Test]
        public void Level1_Must_Unlock_At_Zero()
        {
            // Lv1이 잠겨 있으면 고를 수 있는 난이도가 하나도 없다.
            Assert.Throws<ArgumentException>(
                () => TableWithUnlocks(50, 100, 200, 300, 400, 500, 600, 700, 800, 900));
        }

        [Test]
        public void Unlock_Scores_Must_Not_Go_Backwards()
        {
            // 뒤 단계가 더 싸면 상위 난이도가 하위보다 먼저 열린다.
            Assert.Throws<ArgumentException>(
                () => TableWithUnlocks(0, 100, 90, 300, 400, 500, 600, 700, 800, 900));
        }

        [Test]
        public void Level_Index_Is_Clamped_To_Range()
        {
            var table = SampleTable();
            Assert.AreEqual(1, table[0].Level, "범위 아래는 Lv1로 접힌다.");
            Assert.AreEqual(10, table[99].Level, "범위 위는 Lv10으로 접힌다.");
            Assert.AreEqual(7, table[7].Level);
        }

        // ---- 해금 판정 ----

        [Test]
        public void Level1_Is_Unlocked_From_The_Start()
        {
            var table = SampleTable();
            Assert.IsTrue(table.IsUnlocked(1, 0));
            Assert.AreEqual(1, table.MaxUnlockedLevel(0));
        }

        [Test]
        public void Unlock_Happens_Exactly_At_The_Threshold()
        {
            var table = SampleTable();
            Assert.IsFalse(table.IsUnlocked(3, 199), "199점에서는 Lv3(200)이 잠겨 있다.");
            Assert.IsTrue(table.IsUnlocked(3, 200), "경계값은 해금 쪽에 포함된다.");
        }

        [Test]
        public void MaxUnlockedLevel_Picks_The_Highest_Satisfied_Tier()
        {
            var table = SampleTable();
            Assert.AreEqual(1, table.MaxUnlockedLevel(99));
            Assert.AreEqual(2, table.MaxUnlockedLevel(100));
            Assert.AreEqual(5, table.MaxUnlockedLevel(450));
            Assert.AreEqual(10, table.MaxUnlockedLevel(900));
            Assert.AreEqual(10, table.MaxUnlockedLevel(999999), "만점을 넘겨도 Lv10이 끝이다.");
        }

        [Test]
        public void Next_Unlock_Reports_Target_And_Remaining()
        {
            var table = SampleTable();

            Assert.AreEqual(4, table.NextLockedLevel(250));
            Assert.AreEqual(50, table.PointsToNextUnlock(250));

            Assert.IsNull(table.NextLockedLevel(900), "전부 해금하면 다음 목표가 없다.");
            Assert.AreEqual(0, table.PointsToNextUnlock(900));
        }

        // ---- 저장값 접기 ----

        [Test]
        public void Selected_Level_Folds_Into_Unlocked_Range()
        {
            var table = SampleTable();

            // 손상되거나 구버전에서 넘어온 값이 해금 범위를 넘어도 안전하게 접힌다.
            Assert.AreEqual(3, table.ClampToUnlocked(9, 200), "해금은 Lv3까지다.");
            Assert.AreEqual(2, table.ClampToUnlocked(2, 500), "해금 안쪽이면 그대로 둔다.");
            Assert.AreEqual(1, table.ClampToUnlocked(-5, 0));
        }

        // ---- 한 판 결과 반영 ----

        /// <summary>해금선 100 간격, 승점 = 레벨×10, 패점 = 레벨×5인 표.</summary>
        private static DifficultyTable PointsTable()
        {
            var tiers = new List<DifficultyTier>();
            for (int i = 0; i < DifficultyTable.LevelCount; i++)
            {
                int level = i + 1;
                tiers.Add(new DifficultyTier(level, i * 100, level * 10, level * 5));
            }
            return new DifficultyTable(tiers);
        }

        [Test]
        public void Win_Raises_Both_Score_And_Highest()
        {
            var table = PointsTable();
            var update = table.ApplyMatch(score: 100, highestScore: 100, playedLevel: 3,
                PlayerMatchResult.Win);

            Assert.AreEqual(130, update.Score);
            Assert.AreEqual(130, update.HighestScore);
            Assert.AreEqual(30, update.Delta);
        }

        [Test]
        public void Loss_Lowers_Score_But_Never_The_Unlock()
        {
            // 이 시스템에서 가장 중요한 성질이다.
            // 400점에서 Lv5가 열렸는데, 한 판 지고 375점이 됐다고 Lv5가 다시 잠기면 안 된다.
            var table = PointsTable();
            var update = table.ApplyMatch(score: 400, highestScore: 400, playedLevel: 5,
                PlayerMatchResult.Lose);

            Assert.AreEqual(375, update.Score, "현재 점수는 내려간다.");
            Assert.AreEqual(400, update.HighestScore, "최고 점수는 그대로다.");

            Assert.AreEqual(4, table.MaxUnlockedLevel(update.Score),
                "현재 점수로 판정하면 Lv4로 내려앉는다 — 이렇게 판정하면 안 된다.");
            Assert.AreEqual(5, update.UnlockedAfter, "최고 점수로 판정하므로 Lv5가 유지된다.");
            Assert.IsFalse(update.HasNewUnlock);
        }

        [Test]
        public void Crossing_A_Threshold_Reports_A_New_Unlock()
        {
            var table = PointsTable();
            var update = table.ApplyMatch(score: 390, highestScore: 390, playedLevel: 4,
                PlayerMatchResult.Win);

            Assert.AreEqual(430, update.Score);
            Assert.AreEqual(4, update.UnlockedBefore);
            Assert.AreEqual(5, update.UnlockedAfter);
            Assert.IsTrue(update.HasNewUnlock, "화면 흐름이 이 값으로 갈린다.");
        }

        [Test]
        public void Winning_Without_Crossing_Reports_No_Unlock()
        {
            var table = PointsTable();
            var update = table.ApplyMatch(score: 300, highestScore: 300, playedLevel: 4,
                PlayerMatchResult.Win);

            Assert.AreEqual(340, update.Score);
            Assert.IsFalse(update.HasNewUnlock);
        }

        [Test]
        public void Points_Come_From_The_Level_That_Was_Played()
        {
            // Lv6까지 열려 있어도 Lv1을 골라 놀았으면 Lv1의 점수만 받는다.
            var table = PointsTable();
            var update = table.ApplyMatch(score: 500, highestScore: 500, playedLevel: 1,
                PlayerMatchResult.Win);

            Assert.AreEqual(10, update.Delta, "Lv1의 승점이어야 한다.");
            Assert.AreEqual(510, update.Score);
        }

        [Test]
        public void Delta_Reports_The_Real_Change_At_The_Floor()
        {
            // 하한에 걸리면 실제 변화가 난이도의 차감량보다 작다.
            // 결과 화면은 -50이 아니라 -10을 보여줘야 한다.
            var table = PointsTable();
            var update = table.ApplyMatch(score: 10, highestScore: 900, playedLevel: 10,
                PlayerMatchResult.Lose);

            Assert.AreEqual(0, update.Score);
            Assert.AreEqual(-10, update.Delta);
            Assert.AreEqual(10, update.UnlockedAfter, "점수가 0이 되어도 해금은 그대로다.");
        }

        [Test]
        public void Highest_Below_Current_Is_Repaired()
        {
            // 구버전 데이터를 이관한 직후나 저장이 손상된 경우.
            // 최고 점수를 현재 점수까지 끌어올리지 않으면 이미 도달한 해금이 사라진다.
            var table = PointsTable();
            var update = table.ApplyMatch(score: 500, highestScore: 0, playedLevel: 1,
                PlayerMatchResult.Draw);

            Assert.AreEqual(500, update.HighestScore);
            Assert.AreEqual(6, update.UnlockedBefore);
            Assert.AreEqual(6, update.UnlockedAfter);
        }

        // ---- 공식 생성기 ----

        [Test]
        public void Curve_Builds_A_Valid_Ten_Tier_Table()
        {
            var table = DifficultyCurve.Placeholder.Build();
            Assert.AreEqual(DifficultyTable.LevelCount, table.Tiers.Count);
            Assert.AreEqual(0, table[1].UnlockScore);
        }

        [Test]
        public void Curve_Increases_Every_Column()
        {
            var table = DifficultyCurve.Placeholder.Build();

            for (int level = DifficultyTable.MinLevel + 1; level <= DifficultyTable.MaxLevel; level++)
            {
                var prev = table[level - 1];
                var cur = table[level];

                Assert.Greater(cur.UnlockScore, prev.UnlockScore, $"Lv{level} 해금선");

                // 승리 점수는 반드시 벌어져야 한다. 같아지면 더 어려운 난이도를
                // 같은 보상에 하는 셈이라 그 단계를 고를 이유가 사라진다.
                Assert.Greater(cur.WinPoints, prev.WinPoints, $"Lv{level} 승리 점수");

                // 차감은 승점에서 뽑고 단위가 굵어 같은 값이 이어질 수 있다.
                Assert.GreaterOrEqual(cur.LosePoints, prev.LosePoints, $"Lv{level} 패배 차감");
            }
        }

        [Test]
        public void Curve_Unlock_Gaps_Widen()
        {
            // 누진 증가: 뒤로 갈수록 다음 단계까지의 간격이 벌어져야 한다.
            // 등차로 늘면 승리 점수만 커져서 상위 난이도가 너무 쉽게 뚫린다.
            var table = DifficultyCurve.Placeholder.Build();

            for (int level = DifficultyTable.MinLevel + 2; level <= DifficultyTable.MaxLevel; level++)
            {
                int gap = table[level].UnlockScore - table[level - 1].UnlockScore;
                int prevGap = table[level - 1].UnlockScore - table[level - 2].UnlockScore;
                Assert.GreaterOrEqual(gap, prevGap, $"Lv{level} 구간이 이전 구간보다 좁다.");
            }
        }

        [Test]
        public void Curve_Keeps_Break_Even_Win_Rate_Flat()
        {
            // 손익분기 승률이 단계마다 크게 흔들리면 특정 난이도만 파밍 구간이 된다.
            // 정확히 같을 수는 없으므로 폭으로 확인한다. 낮은 단계는 숫자가 작아
            // 10단위 반올림의 상대 오차가 커지므로(예: 9 → 10) 여유를 둔다.
            var table = DifficultyCurve.Placeholder.Build();

            double min = double.MaxValue;
            double max = double.MinValue;
            foreach (var tier in table.Tiers)
            {
                double rate = tier.BreakEvenWinRate;
                if (rate < min) min = rate;
                if (rate > max) max = rate;
            }

            Assert.Less(max - min, 0.10,
                $"손익분기 승률 편차가 크다({min:P1} ~ {max:P1}).");
        }

        [Test]
        public void Curve_Playtime_Scales_With_WinsPerTier()
        {
            // 이 상수 하나로 전체 플레이타임을 조절할 수 있어야 한다.
            var shortRun = new DifficultyCurve(20d, 1.35d, 0.45d,
                winsPerTier: 5d, pointRoundTo: 10, unlockRoundTo: 100).Build();
            var longRun = new DifficultyCurve(20d, 1.35d, 0.45d,
                winsPerTier: 20d, pointRoundTo: 10, unlockRoundTo: 100).Build();

            Assert.Greater(longRun[DifficultyTable.MaxLevel].UnlockScore,
                shortRun[DifficultyTable.MaxLevel].UnlockScore * 2,
                "WinsPerTier를 4배로 하면 최종 해금선도 크게 늘어야 한다.");
        }
    }
}
