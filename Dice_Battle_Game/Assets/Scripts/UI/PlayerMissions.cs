using UnityEngine;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 일일 미션 진행도를 PlayerPrefs에 저장/로드한다.
    /// <see cref="PlayerStats"/>·<see cref="PlayerWallet"/>과 같은 방식이다.
    ///
    /// 날짜 기준은 지갑과 같은 한국 시간이고, 에디터의 날짜 이동도 함께 따른다.
    /// </summary>
    public static class PlayerMissions
    {
        private const string MissionsKey = "dicebattle.missions";

        private static MissionData _data;

        public static MissionData Data => _data ??= Load();

        private static int Today => PlayerWallet.Today;

        /// <summary>슬롯(0~2)의 미션 정의.</summary>
        public static MissionRules.Mission MissionAt(int slot) => Data.MissionAt(Today, slot);

        /// <summary>지금 최대 해금 난이도 기준의 보상 액수.</summary>
        public static int Reward(int slot)
            => MissionRules.Reward(Data.PoolIndexAt(Today, slot),
                PlayerProgress.Tier(PlayerProgress.MaxUnlockedLevel));

        public static int Progress(int slot) => Data.Progress(Today, slot);
        public static int Target(int slot) => MissionAt(slot).Target;

        public static bool IsComplete(int slot) => Data.IsComplete(Today, slot);
        public static bool IsClaimed(int slot) => Data.IsClaimed(Today, slot);
        public static bool CanClaim(int slot) => Data.CanClaim(Today, slot);

        /// <summary>받을 것이 하나라도 있는가. 메뉴 버튼에 표시를 띄울지 정한다.</summary>
        public static bool HasClaimable => Data.HasClaimable(Today);

        /// <summary>한 판이 끝났을 때 진행도를 올린다.</summary>
        public static void ReportMatch(int removedDice, int wonLines, int rerolls,
            int extras, int extrasOnOpponent)
        {
            int today = Today;

            Data.Advance(today, MissionKind.PlayMatches, 1);
            Data.Advance(today, MissionKind.RemoveDice, removedDice);
            Data.Advance(today, MissionKind.WinLines, wonLines);
            Data.Advance(today, MissionKind.UseReroll, rerolls);
            Data.Advance(today, MissionKind.PlaceExtra, extras);
            Data.Advance(today, MissionKind.PlaceOnOpponent, extrasOnOpponent);

            // "한 판에 N개 이상"은 누적이 아니라 그 판 하나로 판정한다.
            if (removedDice >= MissionRules.BigRemovalThreshold)
                Data.Advance(today, MissionKind.BigRemoval, 1);

            Save();
        }

        /// <summary>수령액을 돌려준다. 받을 수 없으면 0.</summary>
        public static int Claim(int slot)
        {
            // 보상액을 먼저 읽는다. 수령 처리 뒤에 읽어도 값은 같지만, 순서가 바뀌어도
            // 안전하도록 지급할 액수를 먼저 확정한다.
            int reward = Reward(slot);
            if (!Data.TryClaim(Today, slot)) return 0;

            PlayerWallet.AddCoins(reward);
            Save();
            return reward;
        }

        private static MissionData Load()
        {
            string json = PlayerPrefs.GetString(MissionsKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return new MissionData();

            MissionData loaded = null;
            try
            {
                loaded = JsonUtility.FromJson<MissionData>(json);
            }
            catch
            {
                Debug.LogWarning("[PlayerMissions] 미션 데이터를 읽지 못해 새로 시작합니다.");
            }

            if (loaded == null) return new MissionData();

            loaded.Repair();
            return loaded;
        }

        private static void Save()
        {
            Data.Repair();
            PlayerPrefs.SetString(MissionsKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        /// <summary>디버그/테스트용 초기화.</summary>
        public static void Reset()
        {
            _data = new MissionData();
            Save();
        }

#if UNITY_EDITOR
        /// <summary>에디터 전용: 오늘 미션을 전부 목표치까지 채운다(수령은 직접).</summary>
        public static void EditorCompleteAll()
        {
            int today = Today;
            for (int slot = 0; slot < MissionRules.DailyCount; slot++)
            {
                MissionRules.Mission mission = Data.MissionAt(today, slot);
                Data.Advance(today, mission.Kind, mission.Target);
            }

            Save();
        }
#endif
    }
}
