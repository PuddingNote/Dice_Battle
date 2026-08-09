using UnityEngine;

namespace DiceBattle.UI
{
    /// <summary>
    /// 튜토리얼을 이미 지나왔는지 기억한다.
    ///
    /// 저장 키 하나로 끝나지 않는다. 튜토리얼이 없던 시절부터 게임을 하던 사람들에게는
    /// 이 키가 아예 없어서, 키만 보면 <b>이미 몇 달째 플레이 중인 비공개 테스터 전원이
    /// 다음 업데이트에서 튜토리얼을 다시 보게 된다.</b> 그래서 키가 없을 때는 한 번 더
    /// 물어본다 — 판을 끝낸 기록이 있으면 기존 사용자이므로 조용히 완료로 넘긴다.
    ///
    /// 판단은 처음 물어본 그 순간에 굳어진다. 그 뒤로는 키만 보므로, 튜토리얼을 건너뛴
    /// 신규 사용자가 첫 판을 마쳤다고 해서 결론이 바뀌지 않는다.
    /// </summary>
    public static class TutorialState
    {
        private const string DoneKey = "dicebattle.tutorial.done";

        /// <summary>지금 튜토리얼을 권해야 하는가.</summary>
        public static bool ShouldPlay
        {
            get
            {
                if (PlayerPrefs.HasKey(DoneKey)) return PlayerPrefs.GetInt(DoneKey, 0) == 0;

                if (IsExistingPlayer)
                {
                    MarkDone(); // 다음부터는 이 검사를 하지 않는다
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// 튜토리얼이 생기기 전부터 플레이하던 사람인가.
        ///
        /// 지갑(코인)은 보지 않는다. 출석 보상 창이 메인 메뉴에 들어서는 순간 열리므로,
        /// 갓 설치한 사람도 "게임 시작"을 누르기 전에 이미 지갑이 저장돼 있다.
        /// 판을 끝내야만 생기는 기록만 본다.
        /// </summary>
        private static bool IsExistingPlayer => PlayerProgress.HasPlayed || PlayerStats.HasRecord;

        /// <summary>끝까지 봤든 건너뛰었든 다시 띄우지 않는다.</summary>
        public static void MarkDone()
        {
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 전용: 다음 "게임 시작"에서 튜토리얼이 다시 뜨게 한다.
        ///
        /// 키를 지우는 것으로는 되지 않는다. 키가 없으면 <see cref="IsExistingPlayer"/> 검사로
        /// 넘어가는데, 개발 중에는 점수 기록이 이미 있어 곧바로 완료로 판정된다.
        /// 그래서 지우지 않고 "아직 안 봤음"으로 덮어쓴다.
        /// </summary>
        public static void EditorOffer()
        {
            PlayerPrefs.SetInt(DoneKey, 0);
            PlayerPrefs.Save();
        }
#endif
    }
}
