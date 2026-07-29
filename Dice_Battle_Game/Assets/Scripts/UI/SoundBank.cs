using UnityEngine;

namespace DiceBattle.UI
{
    /// <summary>
    /// 게임에서 쓰는 사운드 모음. 비어 있는 슬롯은 그냥 소리가 나지 않는다(에러 없음).
    /// 에디터 Project 창에서 우클릭 → Create → DiceBattle → Sound Bank 로 만들고
    /// GameBootstrap 컴포넌트의 Sounds 슬롯에 연결한다.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundBank", menuName = "DiceBattle/Sound Bank")]
    public sealed class SoundBank : ScriptableObject
    {
        [Header("BGM")]
        [Tooltip("게임 실행 내내 무한 반복되는 배경음악")]
        public AudioClip bgm;

        [Header("효과음")]
        [Tooltip("버튼을 누를 때(시작/설정/리롤/닫기 등 모든 버튼)")]
        public AudioClip buttonSelect;

        [Tooltip("주사위를 라인에 배치할 때")]
        public AudioClip dicePlace;

        [Tooltip("주사위를 굴릴 때(트레이 굴림 연출)")]
        public AudioClip diceShake;

        [Tooltip("제거 직전 주사위가 부들부들 떨릴 때")]
        public AudioClip diceRemoveStart;

        [Tooltip("주사위끼리 부딪혀 튕겨나갈 때")]
        public AudioClip diceRemoveHit;

        [Tooltip("한 판이 끝나고 결과 창이 뜨는 순간")]
        public AudioClip roundEnd;
    }
}
