namespace DiceBattle.UI
{
    /// <summary>
    /// 앱 종료. 에디터에서는 플레이 모드를 끈다.
    /// 뒤로가기 다이얼로그와 강제 업데이트 창이 함께 쓴다.
    /// </summary>
    public static class AppExit
    {
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
