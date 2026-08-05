# 전적 · 통계

무엇을 기록하고, 무엇을 보여주고, 왜 그렇게 나눴는지 정리한다.

---

## 1. 보여주는 것과 저장하는 것이 다르다

**화면에는 전체 합계만 보여준다.** 난이도별 전적도 저장은 하지만 표시하지 않는다.

| | 저장 | 표시 |
|---|:---:|:---:|
| 승 / 패 / 무 합계 | ✅ | ✅ |
| 승률 | ✅ | ✅ |
| 최고 연승 | ✅ | ✅ |
| 판당 평균 제거 | ✅ | ✅ |
| 최고 점수 · 해금 난이도 | ✅ | ✅ |
| **난이도별 승/패/무 (Lv.1~10)** | ✅ | ❌ |

난이도별을 저장하는 이유는 하나다. [Difficulty.md](Difficulty.md) 6장이 **연승 보너스
수치는 실제 연승 분포를 보고 정해야 한다**고 못 박아 두었는데, 그 데이터를 만드는 것이
이 기록이다. 배팅(승부수) 수치를 정할 때도 같은 데이터가 필요하다.

지금 화면에 안 내보내는 것은 순전히 화면을 단순하게 두려는 결정이다. 나중에 보여주고
싶어지면 `StatsData`에 이미 값이 들어 있으니 표시만 붙이면 된다.

---

## 2. 집계 규칙

**무승부는 연승을 끊는다.** "연승"을 글자 그대로 연속 승리로 본 결정이다.
[StatsDataTests.cs](../Dice_Battle_Game/Assets/Tests/EditMode/StatsDataTests.cs)의
`A_Draw_Breaks_The_Streak`에 못 박혀 있다. **나중에 연승 보너스를 붙일 때도 이 규칙을
그대로 따른다.**

**승률의 분모에는 무승부가 들어간다.** 전적을 "N승 N패 N무"로 따로 보여주므로 오해할
여지가 없다. 무승부를 빼면 1승 1무가 승률 100%가 되어 버린다.

**평균 제거는 "내가 제거한 주사위 개수"다.** 제거가 일어난 횟수가 아니다. 한 번에
세 개를 터뜨리면 3으로 센다. 집계는 [GameController.cs](../Dice_Battle_Game/Assets/Scripts/UI/GameController.cs)의
`DoPrimary`에서 `PlaceResult.RemovedCount`를 더한다. 제거는 기본 배치에서만 일어나므로
`DoExtra`에는 같은 집계가 없다.

**중도 포기한 판은 기록하지 않는다.** 뒤로가기로 나가면 `MatchFinished`가 발생하지 않아
점수 정산도 전적 집계도 일어나지 않는다. 둘이 항상 같이 움직인다.

**난이도는 그 판을 시작한 값으로 집계한다.** 점수 정산과 같은 기준이다. 어긋나면
낮은 난이도로 둔 판이 높은 난이도 전적에 들어간다.

---

## 3. 저장 방식

키 하나(`dicebattle.stats`)에 **JSON 한 덩어리**로 저장한다.

점수와 달리 항목이 계속 늘어날 자리라 키를 하나씩 늘리면 금방 지저분해진다.
JSON이면 항목을 추가해도 키가 늘지 않고, 낡은 저장본은 없는 필드가 0으로 남을 뿐
깨지지 않는다.

```
dicebattle.score    현재 점수      (PlayerProgress)
dicebattle.highest  최고 점수      (PlayerProgress)
dicebattle.stats    전적 JSON      (PlayerStats)   ← 이번에 추가
```

### 주의: JsonUtility의 함정

`JsonUtility`는 **프로퍼티를 저장하지 않는다.** 그래서 `StatsData`의 필드는 전부
public이고, `TotalMatches` 같은 계산값만 프로퍼티다.

그리고 JSON에 배열이 없으면 **null을 그대로 남기고, 길이가 달라도 맞춰 주지 않는다.**
`StatsData.Repair()`가 읽은 직후와 쓰기 전에 배열 길이와 음수를 보정한다. 난이도 수가
바뀌어도 기존 저장본이 깨지지 않는 것은 이 함수 덕분이다.

저장본이 손상되어 파싱에 실패하면 **전적만 초기화하고 게임은 그대로 진행한다.**
전적을 잃는 것보다 게임이 멈추는 쪽이 훨씬 나쁘다.

---

## 4. 기존 사용자

비공개 테스트 중인 기존 테스터는 `dicebattle.stats` 키가 없으므로 **0판부터 시작한다.**
이미 둔 판을 복원할 방법은 없다. 점수와 해금은 그대로 유지되므로 진행도를 잃지는 않는다.

전적 화면의 최고 점수·해금 난이도는 전적과 무관하게 `PlayerProgress`에서 읽으므로
첫 실행에도 제대로 나온다.

---

## 5. 확인용 도구

**DiceBattle → 진행도(디버그)** 창 아래쪽에 전적 항목이 붙어 있다.

- 현재 전적을 그대로 보여준다
- **전적 30판 채우기 / 300판** — 화면이 네 자리 판수에서도 깨지지 않는지 확인용.
  승 40 / 패 55 / 무 5 비율로 채운다
- **전적 초기화**

수십 판을 직접 둬서 화면을 확인할 수는 없다.

---

## 6. 관련 파일

- [StatsData.cs](../Dice_Battle_Game/Assets/Scripts/Core/StatsData.cs) — 집계 규칙(순수 C#, 테스트 대상)
- [PlayerStats.cs](../Dice_Battle_Game/Assets/Scripts/UI/PlayerStats.cs) — 저장/로드
- [StatsView.cs](../Dice_Battle_Game/Assets/Scripts/UI/StatsView.cs) — 화면
- [StatsDataTests.cs](../Dice_Battle_Game/Assets/Tests/EditMode/StatsDataTests.cs) — 13개
