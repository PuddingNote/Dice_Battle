using System;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using DiceBattle.Core;

namespace DiceBattle.Managers
{
    /// <summary>방 코드 하나로 진행하는 친선대전의 생명주기 상태(문서 FriendlyMatch.md 2장).</summary>
    public enum RoomStatus { WaitingForOpponent, InProgress, Finished, Unknown }

    /// <summary>
    /// 방 참가 결과. 이 클라이언트가 방장인지 참가자인지에 따라 <see cref="MyPlayerId"/>가
    /// 갈린다 — 그 판이 끝날 때까지 불변이며, BoardView.Build에 그대로 넘기면
    /// 퍼스펙티브 미러링이 저절로 맞는다(문서 2-4).
    /// </summary>
    public sealed class RoomInfo
    {
        public string Code;
        public bool IsHost;
        public PlayerId MyPlayerId => IsHost ? PlayerId.One : PlayerId.Two;
    }

    /// <summary>
    /// 방 코드 생성/참가/생명주기 감지. Firebase Realtime Database 타입은 이 클래스 밖으로
    /// 새어 나가지 않는다(<see cref="FirebaseBootstrapper"/>와 같은 이유).
    ///
    /// <b>권한 모델</b>: 방장만 <c>status</c>/<c>firstPlayer</c>를 쓴다. 참가자는 <c>guestUid</c>
    /// 하나만 쓴다. 그래서 방 생성/참가/시작이 서로 다른 시점의 별개 쓰기가 되고, 보안 규칙이
    /// 같은 다중 경로 쓰기 안에서 형제 필드를 참조하는 애매한 경우가 생기지 않는다
    /// (자세한 이유는 docs/FriendlyMatch.md 3장).
    /// </summary>
    public static class FriendlyRoomService
    {
        private const int CodeDigits = 4;
        private const int MaxCreateAttempts = 5;
        private const long RoomExpiryMs = 5 * 60 * 1000;

        private static readonly System.Random Rng = new System.Random();

        private static DatabaseReference RoomsRoot => FirebaseBootstrapper.Root.Child("rooms");

        private static EventHandler<ValueChangedEventArgs> _guestListener;
        private static EventHandler<ValueChangedEventArgs> _statusListener;
        private static DatabaseReference _listenedGuestRef;
        private static DatabaseReference _listenedStatusRef;

        // ---- 방 만들기(방장) ----

        /// <summary>새 방을 만들고 코드를 발급한다. 코드 충돌 시 자동으로 다른 코드를 재시도한다.</summary>
        public static void CreateRoom(Action<RoomInfo, string> onDone)
        {
            FirebaseBootstrapper.EnsureReady(ok =>
            {
                if (!ok) { onDone?.Invoke(null, FirebaseBootstrapper.LastError); return; }
                TryCreate(0, onDone);
            });
        }

        private static void TryCreate(int attempt, Action<RoomInfo, string> onDone)
        {
            if (attempt >= MaxCreateAttempts)
            {
                onDone?.Invoke(null, "방 코드를 발급하지 못했습니다. 잠시 후 다시 시도해 주세요.");
                return;
            }

            string code = GenerateCode();
            string uid = FirebaseBootstrapper.Uid;
            var roomRef = RoomsRoot.Child(code);

            // hostUid + createdAt을 먼저 커밋한 뒤 status를 따로 쓴다. status 규칙은
            // "hostUid가 이미 나로 되어 있는가"를 형제 필드로 확인하는데, 셋을 한 다중
            // 경로 쓰기에 같이 넣으면 그 순간엔 hostUid가 아직 커밋 전(없는 값)으로
            // 평가돼 규칙이 항상 거부한다 — 두 단계로 나눠야 뒤 단계가 "이미 확정된
            // hostUid"를 볼 수 있다.
            var identity = new Dictionary<string, object>
            {
                ["hostUid"] = uid,
                ["createdAt"] = ServerValue.Timestamp,
            };

            roomRef.UpdateChildrenAsync(identity).ContinueWithOnMainThread(t =>
            {
                if (t.IsCanceled || t.IsFaulted)
                {
                    // 이미 쓰이고 있는(만료 여부와 무관하게) 코드일 가능성이 높다 — 다른 코드로 재시도.
                    TryCreate(attempt + 1, onDone);
                    return;
                }

                roomRef.Child("status").SetValueAsync("WAITING_FOR_OPPONENT").ContinueWithOnMainThread(t2 =>
                {
                    if (t2.IsCanceled || t2.IsFaulted)
                    {
                        onDone?.Invoke(null, "방 상태 초기화에 실패했습니다.");
                        return;
                    }
                    onDone?.Invoke(new RoomInfo { Code = code, IsHost = true }, null);
                });
            });
        }

        private static string GenerateCode()
        {
            int max = 1;
            for (int i = 0; i < CodeDigits; i++) max *= 10;
            return Rng.Next(0, max).ToString(new string('0', CodeDigits));
        }

        /// <summary>
        /// 방장 전용: 참가자가 들어오는 순간을 한 번만 감지해 선공을 랜덤 배정하고
        /// 상태를 IN_PROGRESS로 넘긴다. <see cref="CreateRoom"/> 성공 직후 호출한다.
        /// </summary>
        public static void ListenForGuest(string code, Action<PlayerId> onMatchStarted, Action<string> onError)
        {
            StopListeningForGuest();

            var guestRef = RoomsRoot.Child(code).Child("guestUid");
            _listenedGuestRef = guestRef;
            _guestListener = (sender, e) =>
            {
                if (e.DatabaseError != null)
                {
                    onError?.Invoke(e.DatabaseError.Message);
                    return;
                }
                if (e.Snapshot == null || e.Snapshot.Value == null) return; // 아직 참가자 없음

                StopListeningForGuest(); // 한 번만 반응한다

                PlayerId first = Rng.Next(2) == 0 ? PlayerId.One : PlayerId.Two;
                var updates = new Dictionary<string, object>
                {
                    ["status"] = "IN_PROGRESS",
                    ["firstPlayer"] = first == PlayerId.One ? "One" : "Two",
                };

                RoomsRoot.Child(code).UpdateChildrenAsync(updates).ContinueWithOnMainThread(t =>
                {
                    if (t.IsCanceled || t.IsFaulted)
                    {
                        onError?.Invoke("대전 시작 처리에 실패했습니다.");
                        return;
                    }
                    onMatchStarted?.Invoke(first);
                });
            };
            guestRef.ValueChanged += _guestListener;
        }

        public static void StopListeningForGuest()
        {
            if (_listenedGuestRef != null && _guestListener != null)
                _listenedGuestRef.ValueChanged -= _guestListener;
            _listenedGuestRef = null;
            _guestListener = null;
        }

        // ---- 코드로 입장(참가자) ----

        /// <summary>코드를 검증하고 참가한다. 실패 사유를 사람이 읽을 문구로 돌려준다.</summary>
        public static void JoinRoom(string rawCode, Action<RoomInfo, string> onDone)
        {
            FirebaseBootstrapper.EnsureReady(ok =>
            {
                if (!ok) { onDone?.Invoke(null, FirebaseBootstrapper.LastError); return; }

                string code = NormalizeCode(rawCode);
                if (!IsValidCodeFormat(code))
                {
                    onDone?.Invoke(null, $"코드는 숫자 {CodeDigits}자리입니다.");
                    return;
                }

                string uid = FirebaseBootstrapper.Uid;
                var roomRef = RoomsRoot.Child(code);

                // 참가 가능 여부를 먼저 읽어 친절한 에러로 구분한다. 실제 차단은 항상
                // guestUid 쓰기 보안 규칙이 하므로, 여기서 통과했다고 반드시 성공하는 건
                // 아니다(그 사이 다른 사람이 먼저 들어왔을 수 있음 — 아래에서 다시 처리).
                roomRef.GetValueAsync().ContinueWithOnMainThread(readTask =>
                {
                    if (readTask.IsCanceled || readTask.IsFaulted)
                    {
                        onDone?.Invoke(null, "방 정보를 확인하지 못했습니다. 다시 시도해 주세요.");
                        return;
                    }

                    string reason = ValidateJoinable(readTask.Result, uid);
                    if (reason != null) { onDone?.Invoke(null, reason); return; }

                    roomRef.Child("guestUid").SetValueAsync(uid).ContinueWithOnMainThread(writeTask =>
                    {
                        if (writeTask.IsCanceled || writeTask.IsFaulted)
                        {
                            onDone?.Invoke(null, "입장하지 못했습니다. 다른 사람이 먼저 들어왔을 수 있습니다.");
                            return;
                        }
                        onDone?.Invoke(new RoomInfo { Code = code, IsHost = false }, null);
                    });
                });
            });
        }

        private static string ValidateJoinable(DataSnapshot snap, string myUid)
        {
            if (snap == null || !snap.Exists) return "존재하지 않는 코드입니다.";

            string hostUid = snap.Child("hostUid").Value as string;
            string guestUid = snap.Child("guestUid").Value as string;
            string status = snap.Child("status").Value as string;
            object createdAtRaw = snap.Child("createdAt").Value;

            if (string.IsNullOrEmpty(hostUid)) return "존재하지 않는 코드입니다.";
            if (hostUid == myUid) return "자신이 만든 방에는 입장할 수 없습니다.";
            if (!string.IsNullOrEmpty(guestUid)) return "이미 다른 사람이 입장한 방입니다.";
            if (status != "WAITING_FOR_OPPONENT") return "이미 시작된 방입니다.";

            long createdAt = createdAtRaw != null ? Convert.ToInt64(createdAtRaw) : 0L;
            long ageMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - createdAt;
            if (createdAt <= 0 || ageMs > RoomExpiryMs) return "만료된 코드입니다.";

            return null;
        }

        /// <summary>참가자 전용: 방장이 IN_PROGRESS로 넘기는 순간을 감지해 선공을 받는다.</summary>
        public static void ListenForStart(string code, Action<PlayerId> onMatchStarted, Action<string> onError)
        {
            StopListeningForStart();

            var statusRef = RoomsRoot.Child(code).Child("status");
            _listenedStatusRef = statusRef;
            _statusListener = (sender, e) =>
            {
                if (e.DatabaseError != null)
                {
                    onError?.Invoke(e.DatabaseError.Message);
                    return;
                }
                if (e.Snapshot == null || e.Snapshot.Value as string != "IN_PROGRESS") return;

                StopListeningForStart();

                RoomsRoot.Child(code).Child("firstPlayer").GetValueAsync().ContinueWithOnMainThread(t =>
                {
                    if (t.IsCanceled || t.IsFaulted)
                    {
                        onError?.Invoke("대전 시작 정보를 읽지 못했습니다.");
                        return;
                    }
                    string v = t.Result.Value as string;
                    onMatchStarted?.Invoke(v == "One" ? PlayerId.One : PlayerId.Two);
                });
            };
            statusRef.ValueChanged += _statusListener;
        }

        public static void StopListeningForStart()
        {
            if (_listenedStatusRef != null && _statusListener != null)
                _listenedStatusRef.ValueChanged -= _statusListener;
            _listenedStatusRef = null;
            _statusListener = null;
        }

        // ---- 연결 끊김 감지(onDisconnect) ----

        private static EventHandler<ValueChangedEventArgs> _presenceListener;
        private static DatabaseReference _listenedPresenceRef;

        /// <summary>
        /// 내 연결 상태를 표시하고, 연결이 끊기면 <b>서버가 자동으로</b> 그 사실을 기록하도록
        /// 등록한다. 그리고 상대의 연결 상태를 구독해서, 상대가 (연결이 끊겨서든 스스로
        /// 나가서든) <c>true → false</c>로 바뀌는 순간 <paramref name="onOpponentLeft"/>를
        /// 한 번 호출한다. 유예 없이 즉시다(기획서 6-1 확정 사항).
        ///
        /// <c>true</c>로 한 번도 안 됐다가 <c>false</c>가 뜬 경우(=아직 상대가 들어오기
        /// 전의 기본값)는 무시한다 — 아직 시작도 안 한 걸 "나갔다"로 오인하면 안 된다.
        /// </summary>
        public static void ArmPresence(RoomInfo room, Action onOpponentLeft, Action<string> onError)
        {
            StopPresence();

            string myField = room.IsHost ? "hostConnected" : "guestConnected";
            string oppField = room.IsHost ? "guestConnected" : "hostConnected";
            var roomRef = RoomsRoot.Child(room.Code);

            // 둘 다 지금까지 결과를 확인 안 하고 던지기만 했다 — 등록 자체가 조용히
            // 실패해도 아무 신호가 없었다. 진단을 위해 명시적으로 로그를 남긴다.
            roomRef.Child(myField).OnDisconnect().SetValue(false).ContinueWithOnMainThread(t =>
            {
                if (t.IsCanceled || t.IsFaulted)
                    Debug.LogError($"[Friendly] onDisconnect 등록 실패({myField}): {t.Exception}");
                else
                    Debug.Log($"[Friendly] onDisconnect 등록됨: {myField} → false");
            });
            roomRef.Child(myField).SetValueAsync(true).ContinueWithOnMainThread(t =>
            {
                if (t.IsCanceled || t.IsFaulted)
                    Debug.LogError($"[Friendly] 연결 상태 기록 실패({myField}): {t.Exception}");
            });

            var oppRef = roomRef.Child(oppField);
            _listenedPresenceRef = oppRef;
            bool sawConnected = false;
            _presenceListener = (sender, e) =>
            {
                if (e.DatabaseError != null) { onError?.Invoke(e.DatabaseError.Message); return; }

                bool value = e.Snapshot != null && e.Snapshot.Exists && Convert.ToBoolean(e.Snapshot.Value);
                if (value) { sawConnected = true; return; }

                if (sawConnected)
                {
                    StopPresence();
                    onOpponentLeft?.Invoke();
                }
            };
            oppRef.ValueChanged += _presenceListener;
        }

        /// <summary>
        /// 내가 스스로 판을 나갈 때(뒤로가기/메뉴로 등, 연결은 안 끊겼지만 떠나는 경우) 즉시
        /// 알린다. 실제 연결이 끊기는 경우는 <see cref="ArmPresence"/>가 등록해 둔
        /// onDisconnect가 서버 쪽에서 알아서 처리하므로 이건 "정상적으로 떠나는" 경로 전용이다.
        /// </summary>
        public static void MarkLeft(RoomInfo room)
        {
            if (room == null) return;
            string myField = room.IsHost ? "hostConnected" : "guestConnected";
            RoomsRoot.Child(room.Code).Child(myField).SetValueAsync(false);
        }

        public static void StopPresence()
        {
            if (_listenedPresenceRef != null && _presenceListener != null)
                _listenedPresenceRef.ValueChanged -= _presenceListener;
            _listenedPresenceRef = null;
            _presenceListener = null;
        }

        // ---- 게임 상태 동기화(풀 스냅샷) ----

        private static EventHandler<ValueChangedEventArgs> _stateListener;
        private static DatabaseReference _listenedStateRef;

        /// <summary>
        /// 내 턴 행동이 끝날 때마다(또는 방장의 최초 게임 시작 시) 상태 전체를 덮어쓴다.
        /// 보안 규칙이 "지금 턴이 나인가"를 <c>state.currentPlayer</c>(쓰기 전 값)로
        /// 확인하므로, 내 턴이 아닐 때 호출하면 규칙이 거부한다(문서 FriendlyMatch.md 3장).
        /// </summary>
        public static void PushState(string code, GameStateData data, Action<bool> onDone)
        {
            var wire = ToWire(data);
            RoomsRoot.Child(code).Child("state").SetValueAsync(wire).ContinueWithOnMainThread(t =>
                onDone?.Invoke(!(t.IsCanceled || t.IsFaulted)));
        }

        /// <summary>
        /// 판이 진행되는 내내 구독한다(방 생명주기 리스너와 달리 한 번 반응하고 안 끊는다).
        /// 아직 아무 상태도 없으면(참가자가 방장의 첫 전송을 기다리는 중) 콜백을 부르지 않는다.
        /// </summary>
        public static void ListenForState(string code, Action<GameStateData> onStateChanged, Action<string> onError)
        {
            StopListeningForState();

            var stateRef = RoomsRoot.Child(code).Child("state");
            _listenedStateRef = stateRef;
            _stateListener = (sender, e) =>
            {
                if (e.DatabaseError != null) { onError?.Invoke(e.DatabaseError.Message); return; }
                if (e.Snapshot == null || !e.Snapshot.Exists) return;
                onStateChanged?.Invoke(FromWire(e.Snapshot));
            };
            stateRef.ValueChanged += _stateListener;
        }

        public static void StopListeningForState()
        {
            if (_listenedStateRef != null && _stateListener != null)
                _listenedStateRef.ValueChanged -= _stateListener;
            _listenedStateRef = null;
            _stateListener = null;
        }

        // ---- GameStateData ↔ Firebase 표현 변환 ----
        //
        // 인덱스가 있는 모든 것(fields/lines/dice)에 항상 문자열 키("0","1",...)를 쓴다.
        // Firebase Unity SDK가 정수 키 연속 목록을 List로 돌려줄 때도 있고 Dictionary로
        // 돌려줄 때도 있어(내부 저장 방식에 따라 갈림) 신뢰할 수 없다 — 그래서 쓸 때는
        // Dictionary<string,object>로, 읽을 때는 DataSnapshot.Child(key) 탐색으로만
        // 다뤄서 그 모호함 자체를 피한다. 빈 라인(주사위 0개)은 아예 키를 안 만든다 —
        // Firebase는 빈 객체를 값으로 못 쓴다(그 경로가 없는 것과 같은 뜻이 되므로,
        // 읽는 쪽도 "없으면 빈 라인"으로 다룬다).

        private static Dictionary<string, object> ToWire(GameStateData data)
        {
            var dict = new Dictionary<string, object>
            {
                ["firstPlayer"] = RoleString(data.FirstPlayer),
                ["currentPlayer"] = RoleString(data.CurrentPlayer),
                ["phase"] = data.Phase.ToString(),
            };
            if (data.PendingDice != null) dict["pendingDice"] = DiceToWire(data.PendingDice);

            var fields = new Dictionary<string, object>();
            for (int p = 0; p < data.Fields.Length; p++)
            {
                var fieldWire = FieldToWire(data.Fields[p]);
                if (fieldWire != null) fields[p.ToString()] = fieldWire;
            }
            if (fields.Count > 0) dict["fields"] = fields;

            return dict;
        }

        private static Dictionary<string, object> FieldToWire(FieldData field)
        {
            Dictionary<string, object> lines = null;
            for (int i = 0; i < field.Lines.Length; i++)
            {
                var diceWire = DiceListToWire(field.Lines[i].Dice);
                if (diceWire == null) continue;
                lines ??= new Dictionary<string, object>();
                lines[i.ToString()] = diceWire;
            }
            return lines == null ? null : new Dictionary<string, object> { ["lines"] = lines };
        }

        private static Dictionary<string, object> DiceListToWire(DiceData[] dice)
        {
            if (dice == null || dice.Length == 0) return null;
            var wire = new Dictionary<string, object>();
            for (int i = 0; i < dice.Length; i++)
                wire[i.ToString()] = DiceToWire(dice[i]);
            return wire;
        }

        private static Dictionary<string, object> DiceToWire(DiceData d) => new Dictionary<string, object>
        {
            ["value"] = d.Value,
            ["special"] = d.IsSpecial,
            ["owner"] = RoleString(d.Owner),
        };

        private static GameStateData FromWire(DataSnapshot snap)
        {
            var pendingSnap = snap.Child("pendingDice");
            return new GameStateData
            {
                FirstPlayer = ParseRole(snap.Child("firstPlayer").Value),
                CurrentPlayer = ParseRole(snap.Child("currentPlayer").Value),
                Phase = ParsePhase(snap.Child("phase").Value as string),
                PendingDice = pendingSnap.Exists ? DiceFromWire(pendingSnap) : null,
                Fields = new[]
                {
                    FieldFromWire(snap.Child("fields").Child("0")),
                    FieldFromWire(snap.Child("fields").Child("1")),
                },
            };
        }

        private static FieldData FieldFromWire(DataSnapshot fieldSnap)
        {
            var lines = new LineData[Field.LineCount];
            for (int i = 0; i < Field.LineCount; i++)
                lines[i] = new LineData { Dice = LineDiceFromWire(fieldSnap.Child("lines").Child(i.ToString())) };
            return new FieldData { Lines = lines };
        }

        private static DiceData[] LineDiceFromWire(DataSnapshot lineSnap)
        {
            if (!lineSnap.Exists) return Array.Empty<DiceData>();

            var list = new List<DiceData>(Line.Capacity);
            for (int i = 0; i < Line.Capacity; i++)
            {
                var diceSnap = lineSnap.Child(i.ToString());
                if (!diceSnap.Exists) break; // 그룹화 삽입이라 항상 0부터 빈틈없이 채워진다
                list.Add(DiceFromWire(diceSnap));
            }
            return list.ToArray();
        }

        private static DiceData DiceFromWire(DataSnapshot d) => new DiceData
        {
            Value = Convert.ToInt32(d.Child("value").Value),
            IsSpecial = Convert.ToBoolean(d.Child("special").Value),
            Owner = ParseRole(d.Child("owner").Value),
        };

        private static string RoleString(PlayerId p) => p == PlayerId.One ? "One" : "Two";
        private static PlayerId ParseRole(object raw) => (raw as string) == "Two" ? PlayerId.Two : PlayerId.One;

        private static TurnPhase ParsePhase(string s)
            => Enum.TryParse(s, out TurnPhase phase) ? phase : TurnPhase.NotStarted;

        // ---- 공통 ----

        private static string NormalizeCode(string raw) => (raw ?? "").Trim();

        private static bool IsValidCodeFormat(string code)
        {
            if (code.Length != CodeDigits) return false;
            foreach (char c in code)
                if (c < '0' || c > '9') return false;
            return true;
        }
    }
}
