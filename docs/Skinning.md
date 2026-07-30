# UI 스킨(스프라이트) 적용 가이드

현재 UI는 코드로 생성되며, **스킨 에셋에 스프라이트를 꽂으면 코드 수정 없이** 실제 룩으로 바뀐다.
비워 둔 슬롯은 자동으로 기존 단색으로 표시된다.

준비된 아트는 `Assets/Arts/` 에 둔다(PNG, 투명 배경 권장).

---

## 0. 동작 원리
- `UiSkin`(ScriptableObject)이 스프라이트 슬롯을 담는다.
- 게임 시작 시 `GameBootstrap`이 이 스킨을 적용한다.
- 각 UI 요소는 슬롯 스프라이트가 있으면 그것을, 없으면 단색으로 그린다.

## 1. 스프라이트 임포트 설정
각 PNG 선택 후 Inspector:
1. **Texture Type = `Sprite (2D and UI)`**, Sprite Mode = `Single`
2. **라인 박스 / 트레이**처럼 늘어나는 배경은 **Sprite Editor에서 Border(9-slice)** 지정 → 모서리 안 뭉개짐
3. **주사위 눈**은 정사각형·투명 배경, 9-slice 불필요

### 권장 크기(현재 레이아웃)
| 종류 | 권장 크기 | 9-slice |
|------|-----------|---------|
| 주사위 눈(플레이어/AI, 각 1~6) | 150×150 또는 256×256, 정사각·투명 | ✕ |
| 라인 박스 | 약 480×150 (한 줄 3칸 컨테이너) | ○ |
| 주사위 트레이 | 약 1040×230 (하단 가로) | ○ |
| (선택) 전체 배경 | 1920×1080 | ✕ |
| (선택) 버튼 | 약 400×130 | ○ |

## 2. 스킨 에셋 만들기
- Project 창 우클릭 → **Create → DiceBattle → UI Skin** → `Assets/Arts/` 등에 저장

## 3. 슬롯에 스프라이트 연결 (준비하신 4종 매핑)
스킨 에셋 선택 후 Inspector에서:

| 준비한 스프라이트 | 스킨 슬롯 | 방법 |
|------------------|-----------|------|
| **플레이어 주사위 1~6** | `Player Dice Faces` | 배열 Size=6, **Element 0=눈1 … Element 5=눈6** |
| **AI(적) 주사위 1~6** | `Ai Dice Faces` | 배열 Size=6, 눈1~6 순서 |
| **라인 박스** | `Line Normal` | 드래그 1개 (선택 시엔 자동으로 초록 틴트) |
| **주사위 트레이** | `Tray` | 드래그 1개 |

> 나머지 슬롯(Screen Background, Center Panel, Field Panel, Cell*, Button)은
> 지금 레이아웃에선 필수가 아니다. 필요하면 나중에 채우면 된다.
> 폰트 슬롯은 아래 [3-1. 폰트](#3-1-폰트-tmp) 참고.

### 아이콘/창 슬롯 (비워두면 임시 표시로 동작)

| 슬롯 | 쓰이는 곳 | 비었을 때 |
|------|-----------|-----------|
| `Reroll Icon` | 트레이 왼쪽 리롤 버튼 | "리롤" 문구 |
| `Settings Icon` | 우측 상단 설정 버튼(톱니바퀴) | "설정" 문구 |
| `Page Arrow Icon` | 설명서 페이지 넘김 화살표 | ◀ ▶ 문자 |
| `Window Panel` | 설정/설명서 창 배경 | 코드로 만든 둥근 사각형 |
| `Slider Track` / `Slider Fill` / `Slider Handle` | 볼륨 슬라이더 | 둥근 트랙 + 원형 손잡이 |

- 아이콘은 **정사각 투명 PNG** 1장이면 된다(권장 128×128 이상). 버튼 안쪽에 여백을 두고 들어간다.
- `Page Arrow Icon`은 **오른쪽 방향 1장만** 넣으면 된다. 왼쪽 화살표는 좌우 반전해 재사용한다.
- `Window Panel`, `Slider Track/Fill`은 9-slice(Border) 설정을 해야 모서리가 늘어나지 않는다.

## 3-1. 폰트 (TMP)

모든 텍스트는 **TextMeshPro(TMP)** 를 쓴다. 폰트는 `UiSkin` 에서 한 곳으로 지정한다.

| 슬롯 | 타입 | 설명 |
|------|------|------|
| `Font Asset` | TMP_FontAsset | **권장.** Font Asset Creator로 미리 만든 SDF 폰트 |
| `Font` | Font (TTF) | 대체. `Font Asset`이 비면 **런타임에 동적 TMP 폰트를 자동 생성** |

### A. 가장 빠른 방법 (에디터 작업 없음)
1. `Assets/Fonts/ONE Mobile POP.ttf` 를 `UiSkin` 의 **`Font`** 슬롯에 드래그
2. 끝. 실행 시 `UiFactory.GetFontAsset()` 이 이 TTF로 **Dynamic** TMP 폰트 에셋을 만들어 쓴다.

### B. 폰트 에셋을 직접 만들기

> ⚠️ **Font Asset Creator를 쓰지 말 것.** 아래 우클릭 메뉴를 쓴다.
> 이유는 [한글이 깨질 때](#한글이-으로-깨질-때) 참고.

1. Project 창에서 `ONE Mobile POP.ttf` **선택**
2. **우클릭 → Create → TextMeshPro → Font Asset → SDF** (`Ctrl+Shift+F12`)
3. 생성된 에셋 선택 → Inspector에서 **`Multi Atlas Textures` 체크**
4. `UiSkin` 의 **`Font Asset`** 슬롯에 드래그

이 경로는 **Atlas Population Mode = Dynamic**, 원본 TTF 참조 연결, 빈 아틀라스(1024×1024)
상태로 만들어준다. 실제로 화면에 나오는 글자만 실행 중에 아틀라스에 구워지므로
한글 몇 자를 쓰든 문제가 없다.

### 한글이 □ 으로 깨질 때

**대부분의 원인: Static 아틀라스에 한글 전체를 구우려 한 것.**

한글 완성형은 **11,172자**다. 1024×1024 아틀라스에 Padding 9로 구우면 Auto Sizing이
1pt까지 줄여도 **2,400자 정도**밖에 안 들어가고 나머지는 조용히 버려진다.
그 상태로 실행하면 이런 경고가 뜬다:

```
The character with Unicode value 이 was not found in the
[ONE Mobile POP SDF] font asset or any potential fallbacks.
```

**→ 해결: 에셋을 지우고 위 B 절차(우클릭 → Create → TextMeshPro → Font Asset → SDF)로 다시 만든다.**

에셋 파일(.asset)을 텍스트 에디터로 열어 확인할 수 있는 지표:

| 필드 | 정상 | 문제 |
|------|------|------|
| `m_AtlasPopulationMode` | `1` (Dynamic) | `0` (Static) |
| `m_SourceFontFile` | TTF 참조 있음 | `{fileID: 0}` |
| `m_IsMultiAtlasTexturesEnabled` | `1` | `0` |

기타 원인:

| 원인 | 해결 |
|------|------|
| 폰트 슬롯이 비어 TMP 기본 폰트(LiberationSans, 한글 없음)를 씀 | `UiSkin` 의 `Font Asset` 또는 `Font` 지정 |
| TMP Essential Resources 미임포트 | **Window → TextMeshPro → Import TMP Essential Resources** |
| TTF의 `Include Font Data` 꺼짐 | Font 임포트 설정에서 켜기(Dynamic 생성에 필수) |

> 현재 `ONE Mobile POP.ttf` 는 한글 완성형 전체 + `★ ◀ ▶ · →` 를 모두 포함하고 있어
> (총 12,257 글자) 별도 폴백 폰트 없이 게임 내 모든 문자가 표시된다.

### (선택) 빌드 용량을 줄이고 싶다면
이 게임은 텍스트가 고정이라, 실제로 쓰는 글자만 Static으로 구울 수 있다.
Font Asset Creator에서 `Character Set = Custom Characters` 에 아래를 넣는다:

```
0123456789LvAI점수등급선공랜덤다이스배틀게임시작테스트난이도직접선택당신차례라인을해주위놓세요제거성공추가특별본상대에배치하십니다패무승부종료메뉴로 ★◀▶=·
```

글자 수가 100자 남짓이라 1024×1024 / 90pt 에 충분히 들어간다.
단, 나중에 문구를 추가하면 그 글자는 □ 가 되므로 개발 중에는 Dynamic 을 권장한다.

## 4. 스킨을 게임에 연결
- 씬의 `GameBootstrap` 컴포넌트의 **Skin** 슬롯에 만든 UiSkin 에셋을 드래그
- Play → 적용 확인

## 5. 표시 규칙 (중요)
- **주사위**: 칸이 채워지면 해당 편(플레이어/AI)의 주사위 눈 스프라이트가 표시된다.
  이때 칸 배경은 투명해져 **라인 박스가 뒤로 비친다**(둥근 주사위가 박스 위에 얹힘).
  - 6개를 모두 채워야 스프라이트로 바뀐다(하나라도 비면 숫자 폴백).
- **라인 박스**: 한 줄의 3칸을 감싸는 배경. 선택 가능한 라인은 초록으로 강조된다.
- **트레이**: 새 주사위는 트레이 가운데서 굴러 확정된 뒤, 내 턴이면 왼쪽 / 상대 턴이면 오른쪽으로 이동한다.
- **특수 주사위**: 별도 스프라이트가 없으면 주사위 눈에 금색 틴트로 표시된다.

## 6. 문제 해결
- **주사위가 여전히 숫자**: Player/Ai Dice Faces 배열이 6개 미만이거나 일부 비어 있음.
- **라인 박스 모서리 뭉개짐**: Sprite Editor에서 Border(9-slice) 미설정.
- **아무 것도 안 바뀜**: `GameBootstrap`의 Skin 슬롯 미연결.
- **색이 이상**: 스프라이트에 `UiTheme` 색이 틴트로 곱해진다. 원본색 그대로 쓰려면
  `Assets/Scripts/UI/UiTheme.cs`의 해당 색을 흰색에 가깝게 조정. (라인 박스는 스프라이트가
  있으면 자동으로 흰색 틴트가 적용된다.)
- **빈 칸에 옅은 사각형이 보임**: `UiTheme.CellEmpty` alpha를 0으로 낮추면 완전히 투명.

## 7. 색상/치수 조정
- 색/폰트 크기/셀·트레이 크기 등은 `Assets/Scripts/UI/UiTheme.cs` 상수에서 변경.
- 자주 만지는 값
  - `HeaderFontSize` — 게임 화면 상단 중앙 "점수 (Lv.난이도)"
  - `MenuTitleFontSize` / `MenuScoreFontSize` / `MenuStartFontSize` / `MenuManualFontSize` — 메인 메뉴 글자
  - `MenuStartButtonWidth/Height`, `MenuManualButtonWidth/Height` — 메뉴 버튼 크기
  - `MenuBottomPadding` — 메뉴 버튼이 화면 아래에 얼마나 붙는지
  - `IconButtonSize` / `IconButtonMarginX` / `IconButtonMarginY` — 우측 상단 설정 버튼 크기·여백
  - `MenuHeadSpacerWeight` / `MenuTitleSpacerWeight` — 메뉴에서 제목 위·아래 여백 비율
  - `SettingsWindowWidth/Height`, `ManualWindowWidth/Height`, `CreditsWindowWidth/Height` — 창 크기
  - `CreditsBodyFontSize` — 크레딧 본문(링크가 길어 설명서 본문보다 작게 잡혀 있다)
