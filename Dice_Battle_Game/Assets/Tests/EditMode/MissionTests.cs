using System.Collections.Generic;
using NUnit.Framework;
using DiceBattle.Core;

namespace DiceBattle.Tests
{
    /// <summary>
    /// 일일 미션의 뽑기·진행·수령·초기화 규칙과 보상 비례.
    /// 저장(PlayerPrefs)은 UI 계층이라 다루지 않는다.
    /// </summary>
    public class MissionTests
    {
        private const int Day = 100;

        private static DifficultyTable Table() => DifficultyCurve.Default.Build();

        /// <summary>그날 뽑힌 미션 중 해당 종류가 있는 자리. 없으면 -1.</summary>
        private static int SlotOf(MissionData data, int today, MissionKind kind)
        {
            for (int slot = 0; slot < MissionRules.DailyCount; slot++)
                if (data.MissionAt(today, slot).Kind == kind) return slot;

            return -1;
        }

        /// <summary>해당 종류가 뽑힌 날을 찾는다. 뽑기가 결정적이라 언제나 찾을 수 있다.</summary>
        private static int FindDayWith(MissionData data, MissionKind kind, out int slot)
        {
            for (int day = 0; day < 500; day++)
            {
                slot = SlotOf(data, day, kind);
                if (slot >= 0) return day;
            }

            slot = -1;
            Assert.Fail($"{kind} 미션이 500일 안에 한 번도 뽑히지 않았다");
            return -1;
        }

        // ---- 설계 원칙 ----

        [Test]
        public void Missions_Only_Count_Difficulty_Neutral_Things()
        {
            // "N판 승리" 조건을 넣으면 낮은 난이도에서 빨리 이기는 것이 최적이 되어
            // "이길 수 있는 가장 높은 난이도를 골라라"는 설계가 무너진다.
            // 아래 종류들은 난이도를 낮춰도 이득이 없다.
            foreach (var mission in MissionRules.Pool)
            {
                bool neutral = mission.Kind == MissionKind.PlayMatches
                               || mission.Kind == MissionKind.RemoveDice
                               || mission.Kind == MissionKind.WinLines
                               || mission.Kind == MissionKind.UseReroll
                               || mission.Kind == MissionKind.PlaceExtra
                               || mission.Kind == MissionKind.PlaceOnOpponent
                               || mission.Kind == MissionKind.BigRemoval;

                Assert.IsTrue(neutral,
                    $"{mission.Kind}는 난이도 중립이 아니다. 승리 자체를 세는 미션은 두지 않는다");
            }
        }

        [Test]
        public void Reward_Scales_With_The_Unlocked_Level()
        {
            var table = Table();

            int low = MissionRules.Reward(0, table[DifficultyTable.MinLevel]);
            int high = MissionRules.Reward(0, table[DifficultyTable.MaxLevel]);

            Assert.Greater(high, low, "해금 난이도가 높을수록 보상도 커야 한다");
        }

        [Test]
        public void Every_Level_Gets_At_Least_One_Coin_Per_Mission()
        {
            var table = Table();
            for (int level = DifficultyTable.MinLevel; level <= DifficultyTable.MaxLevel; level++)
                for (int i = 0; i < MissionRules.PoolSize; i++)
                    Assert.GreaterOrEqual(MissionRules.Reward(i, table[level]), 1);
        }

        [Test]
        public void Harder_Variants_Pay_More()
        {
            // 같은 종류인데 목표가 더 큰 미션은 보상도 더 커야 한다.
            for (int a = 0; a < MissionRules.PoolSize; a++)
            {
                for (int b = 0; b < MissionRules.PoolSize; b++)
                {
                    var first = MissionRules.Pool[a];
                    var second = MissionRules.Pool[b];
                    if (first.Kind != second.Kind || first.Target >= second.Target) continue;

                    Assert.Less(first.RewardPercent, second.RewardPercent,
                        $"{first.Kind}: 목표 {first.Target}보다 {second.Target}이 더 줘야 한다");
                }
            }
        }

        // ---- 매일 뽑기 ----

        [Test]
        public void The_Same_Day_Always_Picks_The_Same_Missions()
        {
            int[] first = MissionRules.PickForDay(Day);
            int[] second = MissionRules.PickForDay(Day);

            CollectionAssert.AreEqual(first, second, "앱을 다시 켜도 그날 미션은 같아야 한다");
        }

        [Test]
        public void A_Day_Picks_Three_Distinct_Missions()
        {
            for (int day = 0; day < 200; day++)
            {
                int[] picked = MissionRules.PickForDay(day);

                Assert.AreEqual(MissionRules.DailyCount, picked.Length);
                CollectionAssert.AllItemsAreUnique(picked, $"{day}일: 같은 미션이 두 번 나왔다");
            }
        }

        [Test]
        public void A_Day_Never_Repeats_A_Kind()
        {
            // "3판 플레이"와 "5판 플레이"가 같이 뜨면 사실상 미션이 둘로 줄어든다.
            for (int day = 0; day < 200; day++)
            {
                var kinds = new List<MissionKind>();
                foreach (int index in MissionRules.PickForDay(day))
                    kinds.Add(MissionRules.Pool[index].Kind);

                CollectionAssert.AllItemsAreUnique(kinds, $"{day}일: 같은 종류가 겹쳤다");
            }
        }

        [Test]
        public void Different_Days_Give_Different_Sets()
        {
            var seen = new HashSet<string>();
            for (int day = 0; day < 60; day++)
                seen.Add(string.Join(",", MissionRules.PickForDay(day)));

            // 60일이 전부 같은 조합이면 뽑기가 고장 난 것이다.
            Assert.Greater(seen.Count, 5, "날마다 조합이 거의 바뀌지 않는다");
        }

        [Test]
        public void Every_Mission_Gets_Picked_Eventually()
        {
            var seen = new HashSet<int>();
            for (int day = 0; day < 500; day++)
                foreach (int index in MissionRules.PickForDay(day))
                    seen.Add(index);

            Assert.AreEqual(MissionRules.PoolSize, seen.Count, "한 번도 안 나오는 미션이 있다");
        }

        // ---- 진행 ----

        [Test]
        public void Progress_Advances_Only_For_The_Chosen_Kind()
        {
            var m = new MissionData();
            int day = FindDayWith(m, MissionKind.PlayMatches, out int slot);

            m.Advance(day, MissionKind.PlayMatches, 1);
            Assert.AreEqual(1, m.Progress(day, slot));
        }

        [Test]
        public void Progress_Stops_At_The_Target()
        {
            var m = new MissionData();
            int day = FindDayWith(m, MissionKind.RemoveDice, out int slot);
            int target = m.MissionAt(day, slot).Target;

            m.Advance(day, MissionKind.RemoveDice, target + 50);

            Assert.AreEqual(target, m.Progress(day, slot), "목표를 넘겨 쌓지 않는다");
        }

        [Test]
        public void Lines_Won_Count_Even_When_The_Match_Is_Lost()
        {
            var m = new MissionData();
            int day = FindDayWith(m, MissionKind.WinLines, out int slot);

            // 라인 승리는 판의 승패와 무관하게 쌓인다. 그래서 승리 파밍 유인이 없다.
            m.Advance(day, MissionKind.WinLines, 1);
            Assert.AreEqual(1, m.Progress(day, slot));
        }

        // ---- 수령 ----

        [Test]
        public void Cannot_Claim_Before_Completing()
        {
            var m = new MissionData();
            int day = FindDayWith(m, MissionKind.PlayMatches, out int slot);

            m.Advance(day, MissionKind.PlayMatches, 1);

            Assert.IsFalse(m.IsComplete(day, slot));
            Assert.IsFalse(m.TryClaim(day, slot));
        }

        [Test]
        public void Can_Claim_Once_After_Completing()
        {
            var m = new MissionData();
            int day = FindDayWith(m, MissionKind.PlayMatches, out int slot);

            m.Advance(day, MissionKind.PlayMatches, m.MissionAt(day, slot).Target);

            Assert.IsTrue(m.CanClaim(day, slot));
            Assert.IsTrue(m.TryClaim(day, slot));
            Assert.IsFalse(m.TryClaim(day, slot), "두 번 받을 수 없다");
            Assert.IsTrue(m.IsClaimed(day, slot));
        }

        [Test]
        public void HasClaimable_Reports_Only_Finished_And_Unclaimed()
        {
            var m = new MissionData();
            int day = FindDayWith(m, MissionKind.PlayMatches, out int slot);

            Assert.IsFalse(m.HasClaimable(day), "아무것도 안 했으면 없다");

            m.Advance(day, MissionKind.PlayMatches, m.MissionAt(day, slot).Target);
            Assert.IsTrue(m.HasClaimable(day));

            m.TryClaim(day, slot);
            Assert.IsFalse(m.HasClaimable(day), "받고 나면 사라진다");
        }

        // ---- 날짜 초기화 ----

        [Test]
        public void Progress_Resets_The_Next_Day()
        {
            var m = new MissionData();
            int day = FindDayWith(m, MissionKind.PlayMatches, out int slot);

            m.Advance(day, MissionKind.PlayMatches, m.MissionAt(day, slot).Target);
            m.TryClaim(day, slot);

            Assert.AreEqual(0, m.Progress(day + 1, 0), "진행도가 지워진다");
            Assert.IsFalse(m.IsClaimed(day + 1, 0), "수령 기록도 지워진다");
        }

        [Test]
        public void Turning_The_Clock_Back_Also_Resets()
        {
            var m = new MissionData();
            int day = FindDayWith(m, MissionKind.PlayMatches, out int slot);

            m.Advance(day, MissionKind.PlayMatches, m.MissionAt(day, slot).Target);

            // 미션은 되돌린다고 이득이 없다. 진행도가 사라지므로 오히려 손해다.
            Assert.AreEqual(0, m.Progress(day - 10, 0));
        }

        [Test]
        public void The_Day_Change_Repicks_The_Missions()
        {
            var m = new MissionData();
            m.EnsureDay(Day);
            int[] before = (int[])m.chosen.Clone();

            // 조합이 바뀌는 날을 찾는다(연속 이틀이 같을 수도 있다).
            int changed = Day;
            while (changed < Day + 50)
            {
                changed++;
                m.EnsureDay(changed);
                if (!AreEqual(before, m.chosen)) return;
            }

            Assert.Fail("50일이 지나도 조합이 한 번도 바뀌지 않았다");
        }

        private static bool AreEqual(int[] a, int[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;

            return true;
        }

        // ---- 손상 데이터 ----

        [Test]
        public void Repair_Rebuilds_Missing_Arrays()
        {
            var m = new MissionData();
            m.chosen = null;
            m.progress = null;
            m.claimed = null;

            m.Repair();

            Assert.AreEqual(MissionRules.DailyCount, m.chosen.Length);
            Assert.AreEqual(MissionRules.DailyCount, m.progress.Length);
            Assert.AreEqual(MissionRules.DailyCount, m.claimed.Length);
        }

        [Test]
        public void An_Out_Of_Range_Choice_Is_Repicked()
        {
            var m = new MissionData();
            m.EnsureDay(Day);

            // 후보 목록이 줄어든 뒤의 낡은 저장본을 흉내 낸다.
            m.chosen[0] = 9999;
            m.EnsureDay(Day);

            foreach (int index in m.chosen)
                Assert.IsTrue(MissionRules.IsValidPoolIndex(index));
        }

        [Test]
        public void Advance_Works_On_A_Corrupted_Record_Without_Throwing()
        {
            var m = new MissionData();
            m.chosen = null;
            m.progress = null;

            Assert.DoesNotThrow(() => m.Advance(Day, MissionKind.PlayMatches, 1));
        }
    }
}
