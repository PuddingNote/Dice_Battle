using UnityEditor;
using UnityEngine;
using DiceBattle.Managers;

namespace DiceBattle.EditorTools
{
    /// <summary>
    /// 에디터 전용 Firebase 연결 확인 창 (<b>DiceBattle → Firebase 연결 테스트(디버그)</b>).
    ///
    /// 방 코드 UI가 아직 없는 시점에 익명 로그인이 실제로 되는지만 빠르게 확인하려고 만들었다.
    /// <b>Play 모드에서만 동작한다</b> — Firebase SDK의 콜백 디스패치가 플레이어 루프에
    /// 얹혀 있어 에디트 모드에서는 신뢰할 수 없다.
    ///
    /// Realtime Database 읽기/쓰기 확인은 여기서 하지 않는다. 지금은 보안 규칙이
    /// 잠금 모드라 어떤 쓰기를 시도해도 거부되는 게 정상이고, 그걸 성공/실패로 나누려면
    /// 방 스키마가 있어야 의미가 있다(방 코드 시스템 작업에서 실제 규칙과 함께 확인한다).
    ///
    /// 이 파일은 Editor 폴더에 있어 빌드에 포함되지 않는다.
    /// </summary>
    public sealed class FirebaseSmokeTestWindow : EditorWindow
    {
        private string _status = "아직 시도하지 않음";

        [MenuItem("DiceBattle/Firebase 연결 테스트(디버그)")]
        private static void Open() => GetWindow<FirebaseSmokeTestWindow>("Firebase 테스트");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Play 모드로 들어간 뒤 버튼을 누르세요.\n" +
                "성공하면 임의의 익명 UID가 표시됩니다(매번 같은 값일 필요는 없습니다 —\n" +
                "기기/에디터에 이미 로그인된 계정이 있으면 그걸 재사용합니다).",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("상태", FirebaseBootstrapper.CurrentState.ToString());
            EditorGUILayout.LabelField("UID", FirebaseBootstrapper.Uid ?? "-");
            if (!string.IsNullOrEmpty(FirebaseBootstrapper.LastError))
                EditorGUILayout.HelpBox(FirebaseBootstrapper.LastError, MessageType.Error);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("익명 로그인 시도", GUILayout.Height(32)))
                {
                    _status = "시도 중...";
                    FirebaseBootstrapper.EnsureReady(ok =>
                    {
                        _status = ok
                            ? $"성공 — UID: {FirebaseBootstrapper.Uid}"
                            : $"실패 — {FirebaseBootstrapper.LastError}";
                        Repaint();
                    });
                }
            }
            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Play 모드가 아니라 버튼이 비활성화되어 있습니다.", MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("결과", _status);
        }
    }
}
