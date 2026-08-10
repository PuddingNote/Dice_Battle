using UnityEditor;
using UnityEngine;
using DiceBattle.Core;
using DiceBattle.Managers;
using DiceBattle.UI;

namespace DiceBattle.EditorTools
{
    /// <summary>
    /// 에디터 전용 방 코드 생성/참가 + 실제 대전 진입 확인 창
    /// (<b>DiceBattle → 친선대전 방 테스트(디버그)</b>).
    ///
    /// 방 만들기/입장 UI(8번 작업)가 아직 없는 시점에 방 생명주기와 턴 동기화(6번 작업)를
    /// 실제 게임 화면으로 확인하는 용도. <b>Play 모드에서만 동작한다.</b>
    ///
    /// 방장/참가자 역할을 각각 다른 클라이언트가 맡아야 하므로, 혼자 확인하려면
    /// 프로젝트를 두 개의 에디터 인스턴스로 열거나(같은 프로젝트를 두 번 Play할 수는
    /// 없다 — 별도 클론 폴더 필요) 한쪽은 에디터, 한쪽은 실기기 빌드로 확인한다.
    ///
    /// 이 파일은 Editor 폴더에 있어 빌드에 포함되지 않는다.
    /// </summary>
    public sealed class FriendlyRoomDebugWindow : EditorWindow
    {
        private string _hostLog = "-";
        private string _joinCodeInput = "";
        private string _joinLog = "-";

        private RoomInfo _hostRoom;
        private PlayerId? _hostFirstPlayer;
        private RoomInfo _guestRoom;
        private PlayerId? _guestFirstPlayer;

        [MenuItem("DiceBattle/친선대전 방 테스트(디버그)")]
        private static void Open() => GetWindow<FriendlyRoomDebugWindow>("방 테스트");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Play 모드에서만 동작합니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                DrawHostSection();
                EditorGUILayout.Space(16);
                DrawGuestSection();
            }
        }

        private void DrawHostSection()
        {
            EditorGUILayout.LabelField("방장", EditorStyles.boldLabel);
            if (GUILayout.Button("방 만들기", GUILayout.Height(28)))
            {
                _hostLog = "생성 중...";
                _hostRoom = null;
                _hostFirstPlayer = null;

                FriendlyRoomService.CreateRoom((room, error) =>
                {
                    if (room == null)
                    {
                        _hostLog = $"실패 — {error}";
                        Repaint();
                        return;
                    }

                    _hostRoom = room;
                    _hostLog = $"방 생성됨 — 코드: {room.Code} (참가자 대기 중)";
                    Repaint();

                    FriendlyRoomService.ListenForGuest(room.Code,
                        first =>
                        {
                            _hostFirstPlayer = first;
                            _hostLog = $"코드 {room.Code} — 참가자 입장! 선공: {first}";
                            Repaint();
                        },
                        err =>
                        {
                            _hostLog = $"코드 {room.Code} — 참가자 대기 중 오류: {err}";
                            Repaint();
                        });
                });
            }
            EditorGUILayout.LabelField("결과", _hostLog);

            using (new EditorGUI.DisabledScope(_hostRoom == null || _hostFirstPlayer == null))
            {
                if (GUILayout.Button("실제 게임 화면으로 진입", GUILayout.Height(24)))
                    EnterGame(_hostRoom, _hostFirstPlayer.Value);
            }
        }

        private void DrawGuestSection()
        {
            EditorGUILayout.LabelField("참가자", EditorStyles.boldLabel);
            _joinCodeInput = EditorGUILayout.TextField("코드", _joinCodeInput);
            if (GUILayout.Button("참가", GUILayout.Height(28)))
            {
                _joinLog = "참가 시도 중...";
                _guestRoom = null;
                _guestFirstPlayer = null;

                FriendlyRoomService.JoinRoom(_joinCodeInput, (room, error) =>
                {
                    if (room == null)
                    {
                        _joinLog = $"실패 — {error}";
                        Repaint();
                        return;
                    }

                    _guestRoom = room;
                    _joinLog = $"참가 완료 — 코드: {room.Code} (시작 대기 중)";
                    Repaint();

                    FriendlyRoomService.ListenForStart(room.Code,
                        first =>
                        {
                            _guestFirstPlayer = first;
                            _joinLog = $"코드 {room.Code} — 대전 시작! 선공: {first}";
                            Repaint();
                        },
                        err =>
                        {
                            _joinLog = $"코드 {room.Code} — 시작 대기 중 오류: {err}";
                            Repaint();
                        });
                });
            }
            EditorGUILayout.LabelField("결과", _joinLog);

            using (new EditorGUI.DisabledScope(_guestRoom == null || _guestFirstPlayer == null))
            {
                if (GUILayout.Button("실제 게임 화면으로 진입", GUILayout.Height(24)))
                    EnterGame(_guestRoom, _guestFirstPlayer.Value);
            }
        }

        private static void EnterGame(RoomInfo room, PlayerId firstPlayer)
        {
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm == null)
            {
                Debug.LogError("[FriendlyRoomDebugWindow] 씬에서 GameManager를 찾지 못했습니다" +
                    "(GameBootstrap이 붙은 오브젝트가 있는 씬에서 Play 중인지 확인하세요).");
                return;
            }
            gm.StartFriendlyMatch(room, firstPlayer);
        }

        private void OnDisable()
        {
            FriendlyRoomService.StopListeningForGuest();
            FriendlyRoomService.StopListeningForStart();
        }
    }
}
