using System;

namespace DiceBattle.Core
{
    /// <summary>
    /// 오늘의 미션 세 자리와 각 자리의 진행도·수령 여부.
    /// <see cref="WalletData"/>와 같이 JSON 한 덩어리로 저장되므로 필드는 전부 public이다.
    ///
    /// <b>"오늘"은 밖에서 넣어 준다.</b> 날짜 기준은 지갑과 같은 한국 시간이며,
    /// <see cref="WalletData.Today"/>로 만든 날짜 번호를 그대로 쓴다.
    /// </summary>
    [Serializable]
    public sealed class MissionData
    {
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;

        /// <summary>아래 값들이 속한 날짜. 오늘과 다르면 전부 다시 뽑는다.</summary>
        public int day = WalletData.NeverClaimed;

        /// <summary>
        /// 오늘 뽑힌 미션의 후보 번호. 날짜로 다시 계산할 수 있지만 저장해 둔다 —
        /// 후보 목록이 바뀌어도 그날 진행 중이던 미션이 무엇이었는지 알 수 있어야 한다.
        /// </summary>
        public int[] chosen = new int[MissionRules.DailyCount];

        public int[] progress = new int[MissionRules.DailyCount];
        public bool[] claimed = new bool[MissionRules.DailyCount];

        /// <summary>
        /// 날짜가 바뀌었으면 미션을 새로 뽑고 진행도를 지운다.
        ///
        /// <b>시계를 과거로 돌려도 초기화된다.</b> 출석과 달리 미션은 되돌린다고 이득이
        /// 없다 — 진행도가 사라지므로 오히려 손해다. 그래서 방향을 따지지 않는다.
        /// </summary>
        public void EnsureDay(int today)
        {
            Repair();
            if (day == today && IsChosenValid()) return;

            day = today;
            chosen = MissionRules.PickForDay(today);
            progress = new int[MissionRules.DailyCount];
            claimed = new bool[MissionRules.DailyCount];
        }

        /// <summary>슬롯이 가리키는 후보 번호.</summary>
        public int PoolIndexAt(int today, int slot)
        {
            if (!IsValidSlot(slot)) return 0;
            EnsureDay(today);
            return chosen[slot];
        }

        /// <summary>슬롯의 미션 정의.</summary>
        public MissionRules.Mission MissionAt(int today, int slot)
            => MissionRules.Pool[PoolIndexAt(today, slot)];

        /// <summary>해당 종류의 미션이 오늘 뽑혔다면 진행도를 올린다.</summary>
        public void Advance(int today, MissionKind kind, int amount)
        {
            if (amount <= 0) return;
            EnsureDay(today);

            for (int slot = 0; slot < MissionRules.DailyCount; slot++)
            {
                MissionRules.Mission mission = MissionRules.Pool[chosen[slot]];
                if (mission.Kind != kind) continue;

                // 목표를 넘겨 쌓아 둘 이유가 없다. 화면에 "12 / 10"으로 나오면 지저분하다.
                int next = progress[slot] + amount;
                progress[slot] = next > mission.Target ? mission.Target : next;
            }
        }

        public int Progress(int today, int slot)
        {
            if (!IsValidSlot(slot)) return 0;
            EnsureDay(today);
            return progress[slot];
        }

        public bool IsComplete(int today, int slot)
            => IsValidSlot(slot) && Progress(today, slot) >= MissionAt(today, slot).Target;

        public bool IsClaimed(int today, int slot)
        {
            if (!IsValidSlot(slot)) return false;
            EnsureDay(today);
            return claimed[slot];
        }

        public bool CanClaim(int today, int slot)
            => IsComplete(today, slot) && !IsClaimed(today, slot);

        /// <summary>수령 처리. 코인 지급은 호출부가 한다.</summary>
        public bool TryClaim(int today, int slot)
        {
            if (!CanClaim(today, slot)) return false;

            claimed[slot] = true;
            return true;
        }

        /// <summary>지금 받을 수 있는 미션이 하나라도 있는가(메뉴 표시용).</summary>
        public bool HasClaimable(int today)
        {
            for (int slot = 0; slot < MissionRules.DailyCount; slot++)
                if (CanClaim(today, slot)) return true;

            return false;
        }

        private static bool IsValidSlot(int slot)
            => slot >= 0 && slot < MissionRules.DailyCount;

        private bool IsChosenValid()
        {
            if (chosen == null || chosen.Length != MissionRules.DailyCount) return false;

            for (int i = 0; i < chosen.Length; i++)
                if (!MissionRules.IsValidPoolIndex(chosen[i])) return false;

            return true;
        }

        /// <summary>
        /// 손상되거나 낡은 저장본 보정.
        /// 배열 길이만 맞춘다. 내용이 이상하면 <see cref="EnsureDay"/>가 다시 뽑는다.
        /// </summary>
        public void Repair()
        {
            chosen = Resize(chosen, MissionRules.DailyCount);
            progress = Resize(progress, MissionRules.DailyCount);
            claimed = Resize(claimed, MissionRules.DailyCount);

            for (int i = 0; i < progress.Length; i++)
                if (progress[i] < 0) progress[i] = 0;

            version = CurrentVersion;
        }

        private static int[] Resize(int[] source, int length)
        {
            if (source != null && source.Length == length) return source;

            var fixedArray = new int[length];
            if (source != null)
            {
                int copy = source.Length < length ? source.Length : length;
                for (int i = 0; i < copy; i++) fixedArray[i] = source[i];
            }
            return fixedArray;
        }

        private static bool[] Resize(bool[] source, int length)
        {
            if (source != null && source.Length == length) return source;

            var fixedArray = new bool[length];
            if (source != null)
            {
                int copy = source.Length < length ? source.Length : length;
                for (int i = 0; i < copy; i++) fixedArray[i] = source[i];
            }
            return fixedArray;
        }
    }
}
