# 친선대전 (방 코드 1:1)

방 코드로 실제 유저 두 명을 붙이는 모드. 기존 게임 규칙은 그대로고, 상대를 AI에서
사람으로 바꾸는 메타 레이어만 얹는다. 원 기획서: `친선대전_방코드_시스템_기획서.md`(개인 데스크톱, 저장소 밖).

---

## 1. 핵심 결정사항

| 항목 | 결정 | 왜 |
|---|---|---|
| 백엔드 | Firebase Realtime Database + 익명 인증 | 프로젝트에 기존 백엔드가 전무해서 신규 도입 비용이 제일 낮은 쪽. 턴제라 Photon 같은 초저지연 SDK는 과함 |
| 동기화 방식 | **풀 상태 스냅샷** — 이벤트 재생 아님 | 두 기기의 로컬 시뮬레이션이 미세하게 어긋나는 버그를 구조적으로 차단. 상태가 작아서(3×3 필드) 매번 통째로 보내도 비용 문제 없음 |
| PlayerId 배정 | 방장 = `PlayerId.One`, 참가자 = `PlayerId.Two`, 판 내내 불변 | [PlayerId.cs](../Dice_Battle_Game/Assets/Scripts/Core/Model/PlayerId.cs)가 원래 One/Two 절대 기준이라 손댈 것 없음 |
| 방 생명주기 권한 | **방장이 유일한 권한자** — 상태 전환(WAITING→IN_PROGRESS)과 선공 배정을 항상 방장이 씀 | 참가자가 쓰는 필드(`guestUid`)와 방장이 쓰는 필드(`status`/`firstPlayer`/`createdAt`)를 완전히 분리해서, 보안 규칙이 같은 다중 경로 쓰기 안에서 서로를 참조할 필요가 없게 만듦(경쟁/모호성 제거) |
| 상대 턴 렌더링 | v1은 스냅(연출 없음) | 이전 상태와 새 상태를 diff해서 이동/제거 연출을 재현하려면 별도 시스템이 필요함. 1인 개발 범위에서 제외 |
| 연결 끊김 | 유예 없이 즉시 무효 처리, 메뉴 복귀 | Firebase `onDisconnect()`로 서버가 자동 처리(7번 작업) |
| 재접속 | 지원 안 함 | 구현 비용 대비 가치 낮음 |
| 리롤 | 판당 1회, 광고 트랙 완전 배제 | 지인이 대기 중인 상황에 광고가 끼면 안 됨(타협 불가) |
| 점수/코인/미션 | 완전 비연동 | 격리된 모드로 취급 |
| 방 코드 | 숫자 4자리, 5분 만료, 선착순 2명 후 마감 | 기획서 제안 그대로 |

---

## 2. 데이터 모델 (`/rooms/{code}`)

```
rooms/
  {code}/                      예: "4829"
    hostUid: string             방장 익명 UID (생성 시 고정)
    guestUid: string | null     참가자 익명 UID (참가 시 1회만 채워짐)
    status: "WAITING_FOR_OPPONENT" | "IN_PROGRESS" | "FINISHED"
    createdAt: number           서버 타임스탬프(ms). 5분 만료 판정 기준
    firstPlayer: "One" | "Two"  참가자 입장 감지 즉시 방장이 랜덤 배정
    hostConnected: bool          방장의 연결 상태. 끊기면 서버가 자동으로 false로 씀
    guestConnected: bool         참가자의 연결 상태. 끊기면 서버가 자동으로 false로 씀
    state:                       게임 상태 풀 스냅샷. 그 턴을 둔 사람이 매번 통째로 덮어씀
      firstPlayer: "One" | "Two"
      currentPlayer: "One" | "Two"   보안 규칙이 "지금 내 턴인가"를 여기로 확인
      phase: "AwaitingPrimaryPlacement" | "AwaitingExtraPlacement" | "GameOver"
      pendingDice: { value, special, owner } | (필드 자체가 없음 = null, GameOver일 때만)
      fields:
        "0":                      PlayerId.One의 필드 (0개 라인이면 이 자체가 없을 수 있음)
          lines:
            "0": { dice: { "0": {value,special,owner}, "1": {...} } }  (빈 라인은 키 자체가 없음)
            "1": { ... }
            "2": { ... }
        "1":                      PlayerId.Two의 필드
          ...
```

인덱스가 있는 모든 것(`fields`/`lines`/`dice`)은 항상 **문자열 키**("0","1",...)를 쓴다.
Firebase Unity SDK가 정수 키 연속 목록을 읽을 때 `List`로 줄지 `Dictionary`로 줄지
내부 저장 방식에 따라 갈려서 신뢰할 수 없다 — 쓸 때는 `Dictionary<string,object>`로,
읽을 때는 `DataSnapshot.Child(key)` 탐색으로만 다뤄서 그 모호함 자체를 피한다.

**빈 라인(주사위 0개)은 그 경로를 아예 안 만든다.** Firebase는 빈 객체를 값으로 못
쓴다(그 경로가 없는 것과 같은 뜻이 된다). 읽는 쪽도 "그 경로가 없으면 빈 라인"으로
다루므로 손실 없이 왕복된다 — 판이 막 시작해 양쪽 필드가 통째로 비어 있으면
`fields` 자체가 아예 없는 채로 전송된다.

`MatchOutcome`(승패 결과)은 전송하지 않는다. `phase`가 `GameOver`가 되면 받는 쪽이
`MatchEvaluator.Evaluate()`로 그대로 재현되는 값이라 중복이기 때문이다.

**같은 코드는 재사용하지 않는다.** 코드 생성 시 이미 그 경로에 뭔가 있으면(만료
여부와 무관하게) 그냥 다른 무작위 코드로 재시도한다. "만료된 방을 지우고 그 자리에
새로 쓰기"는 보안 규칙에서 형제 필드를 같은 다중 경로 쓰기 안에서 참조해야 해서
검증이 애매해진다 — 4자리(10000칸) 공간에 5분 만료를 두면 충돌 자체가 드물고,
안 지워진 만료 방이 좀 쌓여도(닫힌 테스트 규모에서) 저장 비용은 무시할 만하다.
나중에 실사용자가 늘면 Cloud Functions 스케줄러로 청소하는 걸 고려(지금은 안 함).

---

## 3. 보안 규칙

Firebase 콘솔 → Realtime Database → 규칙 탭에 그대로 붙여넣는다.

```json
{
  "rules": {
    ".read": false,
    ".write": false,
    "rooms": {
      "$code": {
        ".read": "auth != null",

        "hostUid": {
          ".write": "auth != null && !data.exists() && newData.val() == auth.uid"
        },

        "guestUid": {
          ".write": "auth != null
              && !data.exists()
              && newData.val() == auth.uid
              && data.parent().child('hostUid').val() != null
              && data.parent().child('hostUid').val() != auth.uid
              && data.parent().child('status').val() == 'WAITING_FOR_OPPONENT'
              && (data.parent().child('createdAt').val() + 300000) > now"
        },

        "status": {
          ".write": "auth != null && data.parent().child('hostUid').val() == auth.uid"
        },

        "createdAt": {
          ".write": "auth != null && !data.exists() && newData.val() == now"
        },

        "firstPlayer": {
          ".write": "auth != null && data.parent().child('hostUid').val() == auth.uid"
        },

        "hostConnected": {
          ".write": "auth != null && data.parent().child('hostUid').val() == auth.uid"
        },

        "guestConnected": {
          ".write": "auth != null && data.parent().child('guestUid').val() == auth.uid"
        },

        "state": {
          ".write": "auth != null && (
              (!data.exists() && data.parent().child('hostUid').val() == auth.uid)
              ||
              (data.exists()
                && data.child('currentPlayer').val() == 'One'
                && data.parent().child('hostUid').val() == auth.uid)
              ||
              (data.exists()
                && data.child('currentPlayer').val() == 'Two'
                && data.parent().child('guestUid').val() == auth.uid)
            )"
        }
      }
    }
  }
}
```

읽기(`auth != null`)는 방 참가자로 제한하지 않았다 — 참가 전에 "이 코드가 존재하는지/
꽉 찼는지/만료됐는지"를 먼저 읽어서 친절한 에러 메시지를 보여주려면 참가자가 되기
전에도 읽을 수 있어야 한다. 노출되는 값이 익명 UID·상태·타임스탬프뿐이라 민감하지
않고, 실제 참가 차단은 어차피 `guestUid` 쓰기 규칙이 한다(먼저 읽었다고 끼어들 수
있는 게 아님).

각 필드 쓰기 규칙이 참조하는 형제 필드(`hostUid`, `status`, `createdAt`)는 전부
**그 필드 자신이 속한 트랜잭션보다 먼저 커밋된 값**이어야 한다 — 같은 다중 경로
쓰기 안에서 막 쓰려는 형제 값은 규칙 평가 시점엔 아직 커밋 전(없는 값)으로 보여서
항상 거부되기 때문이다. 그래서 방 만들기는 **두 단계**로 나눠 보낸다:

1. `hostUid` + `createdAt`을 먼저 커밋(이 둘은 서로를 참조하지 않아 같이 보내도 된다)
2. 그게 성공한 뒤에만 `status: WAITING_FOR_OPPONENT`를 따로 커밋(이제 `hostUid`가
   확정돼 있어 규칙이 통과한다)

참가(`guestUid` 하나만)와 시작(`status`+`firstPlayer`, 둘 다 `hostUid`만 참조하고
서로를 참조하지 않음)은 원래도 문제없다.

(2026-08-10, 실제 테스트에서 이 실수로 방 만들기가 항상 실패했다 — 세 필드를
한 번에 보내던 첫 구현을 고쳐 위 순서로 나눴다.)

**`state` 규칙**: `state`는 매 턴 통째로 한 번에(`SetValueAsync`) 덮어쓰므로 위 필드들과
달리 형제 트랜잭션 문제가 없다 — `data.child('currentPlayer')`는 항상 "이번 쓰기
직전의" 값이라 안전하게 참조할 수 있다. 뜻은: **지금 턴을 가진 사람만 다음 상태를
쓸 수 있다**(최초 1회는 방장이 예외적으로 씀 — 그때는 아직 `state` 자체가 없다).

**의도적으로 안 하는 것**: 이 규칙은 "차례가 맞는 사람이 썼는가"만 확인하지, `state`
**안의 내용**(주사위 값, 배치 결과 등)이 실제 게임 규칙과 맞는지는 검증하지 않는다.
그러려면 Realtime Database 규칙 언어로 게임 엔진 전체를 다시 구현해야 해서 비현실적이다.
친선대전은 "코드를 아는 지인끼리"라는 신뢰 전제가 이미 있는 기능이라(기획서 1장),
이 정도 신뢰 모델이면 충분하다고 판단했다 — 정식 경쟁 매칭이 된다면 그때 서버 권위
검증(Cloud Functions 등)을 고려할 문제다.

**연결 끊김 감지(`hostConnected`/`guestConnected`)**: 판을 시작할 때 각자 자기 필드를
`true`로 쓰고, 동시에 Firebase의 `OnDisconnect().SetValue(false)`를 등록한다 —
연결이 실제로 끊기면(앱 강제종료, 네트워크 단절 등) **서버가 알아서** 그 값을
`false`로 바꿔 준다. 상대는 이 필드를 구독하다가 `true→false`로 바뀌는 순간 즉시
반응한다(유예 없음, 기획서 6-1). 스스로 나가는 경우(뒤로가기 등, 연결은 안 끊김)는
`GameController.AbortMatch`가 같은 필드를 직접 `false`로 써서 같은 효과를 낸다.

게임이 정상 종료(`GameOver`)된 뒤에는 두 리스너(`state`/연결 감지)를 모두 끊는다 —
안 끊으면 이긴/진 쪽이 먼저 "메뉴로"를 눌러 나가는 정상적인 흐름을 상대 화면이
"연결이 끊겼다"로 오인해 버린다.

---

## 4. 진행 상태

- [x] Firebase SDK 도입 + 익명 로그인 (`FirebaseBootstrapper`)
- [x] 방 코드 생성/참가 (`FriendlyRoomService`) — 실기 2클라이언트 테스트로 확인됨
- [x] 턴 동기화(풀 상태 스냅샷) — `GameStateSnapshot`(Core), `FriendlyRoomService.PushState/ListenForState`,
      `GameController.StartFriendlyMatch/RemoteTurn/OnRemoteStateReceived`. **실기 2클라이언트 테스트로 확인됨**
- [x] `onDisconnect()` 기반 즉시 무효 처리 — `FriendlyRoomService.ArmPresence/MarkLeft`,
      `GameController.OnOpponentLeft`. 실기 2클라이언트 테스트로 확인됨
- [ ] 방 만들기/입장 UI — 8번 작업
