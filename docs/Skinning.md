# UI 스킨(스프라이트) 적용 가이드 — 상세판

현재 UI는 코드로 생성되지만, **스프라이트를 꽂으면 코드 수정 없이 실제 게임 룩**으로 바뀌도록
스킨(Skin) 시스템을 두었다. 스프라이트를 지정하지 않은 슬롯은 자동으로 지금의 단색으로 표시된다.
→ 아트가 준비되는 대로 이 문서만 따라 하면 된다.

---

## 0. 동작 원리 (먼저 이해)
- `UiSkin`은 스프라이트/폰트 "슬롯"을 담는 에셋(ScriptableObject)이다.
- 게임 시작 시 `GameBootstrap`이 이 스킨을 `UiSkin.Active`로 설정한다.
- 각 UI 요소(셀·라인·버튼·패널·배경·주사위 눈)는 그릴 때:
  - 해당 슬롯에 **스프라이트가 있으면 그 스프라이트**를,
  - **없으면 기존 단색(UiTheme의 색)** 을 쓴다.
- 스프라이트에는 `UiTheme`의 색이 **틴트(tint)** 로 곱해진다(필드의 파랑/빨강 구분 유지).

---

## 1. 스프라이트 임포트 설정 (아트 파일 준비)
PNG(투명 배경 권장) 파일을 `Assets/Art/` 등에 넣고, 각 파일을 선택 후 Inspector에서:
1. **Texture Type = `Sprite (2D and UI)`**
2. **Sprite Mode = `Single`**
3. Pixels Per Unit: 기본 100 그대로 무방(UI는 크게 상관없음)
4. Filter Mode: `Bilinear`(일반) 또는 `Point`(픽셀아트)
5. Compression: 품질 우선이면 `None`/`High`
6. **9-slice가 필요한 것(패널·버튼·셀·라인 배경)** 은 **Sprite Editor**를 열어
   `Border`(L/T/R/B)를 드래그로 지정 → 늘려도 모서리가 안 뭉개진다.
   (주사위 눈·배경 이미지는 9-slice 불필요)

### 권장 크기(현재 레이아웃 기준, `UiTheme` 값)
| 슬롯 | 대상 | 권장 원본 크기 | 9-slice |
|------|------|----------------|---------|
| Screen Background | 전체 배경 | 1920×1080 (가로) | ✕ |
| Field Panel | 필드 프레임(3행) | ~640×900, 테두리형 | ○ |
| Center Panel | 중앙 패널 | ~400×900 | ○ |
| Line Normal / Line Highlight | 라인 행 배경 | ~600×170 | ○ |
| Cell Empty / Filled / Special | 칸 배경 | 150×150 (또는 256×256) | ○ |
| **Dice Faces ×6** | 주사위 눈 1~6 | 150×150 (또는 256×256), 투명 배경 | ✕ |
| Tray | 주사위 굴림판(획득 주사위 배경) | ~280×280, 안쪽이 파인 판 형태 | ○ |
| Button | 버튼 배경 | ~400×130, 테두리형 | ○ |

> 셀/주사위는 정사각형 권장. 셀 실제 표시 크기는 `UiTheme.CellSize`(현재 150)이며,
> 원본을 더 크게(256) 만들어도 자동 축소되어 선명하다.

---

## 2. 스킨 에셋 만들기
1. Project 창에서 우클릭 → **Create → DiceBattle → UI Skin**
2. 생성된 `UiSkin` 에셋을 `Assets/Art/` 등에 둔다(이름 자유).

## 3. 슬롯에 스프라이트 연결
스킨 에셋을 선택하고 Inspector에서 각 슬롯에 스프라이트를 드래그:

| 슬롯 | 넣는 것 | 비고 |
|------|---------|------|
| Font | 폰트(.ttf/.otf → Unity가 Font로 임포트) | 비우면 내장 폰트 |
| Screen Background | 배경 스프라이트 | 메뉴·보드 공통 |
| Field Panel | 필드 프레임 | 파랑/빨강은 틴트로 자동 구분 |
| Center Panel | 중앙 패널 | |
| Line Normal / Line Highlight | 평상시 / 선택된 라인 행 | 서로 다른 스프라이트 권장 |
| Cell Empty / Cell Filled / Cell Special | 빈칸 / 일반 주사위칸 / 특수칸 배경 | |
| **Dice Faces** | 크기 6으로 설정 후 **Element 0=눈1 … Element 5=눈6** | **6개 모두 채워야** 숫자 대신 표시 |
| Tray | 주사위 굴림판 이미지 | 획득 주사위가 이 판 위(중앙)에서 굴러감 |
| Button | 버튼 배경 | 모든 버튼 공통 |

## 4. 스킨을 게임에 연결
- 씬에서 `GameBootstrap` 컴포넌트가 붙은 GameObject 선택
- Inspector의 **Skin** 슬롯에 만든 UiSkin 에셋을 드래그
- Play → 스프라이트가 적용된 화면 확인

## 5. 검증 체크
- [ ] 메뉴/보드 배경이 스프라이트로 바뀜
- [ ] 주사위 눈이 숫자 → 스프라이트로 바뀜(6개 다 넣었을 때)
- [ ] 특수 주사위칸이 구분되어 보임
- [ ] 버튼/라인/셀이 스프라이트로 보임
- [ ] 선택 가능한 라인이 강조(Highlight)됨

---

## 6. 자주 겪는 문제 (Troubleshooting)
- **스프라이트가 어둡게/색이 이상하게 나온다**: 스프라이트에 `UiTheme`의 색이 틴트로 곱해진다.
  스프라이트 원본 색을 그대로 쓰고 싶으면 `Assets/Scripts/UI/UiTheme.cs`에서 해당 색을
  흰색(`Color.white`)에 가깝게 바꾼다. (예: 버튼 스프라이트가 이미 파란색이면 `UiTheme.Button`을 흰색으로)
- **9-slice 모서리가 뭉개진다**: Sprite Editor에서 Border를 지정하지 않았기 때문. Border 설정 필요.
- **주사위 눈이 여전히 숫자다**: Dice Faces 배열이 6개 미만이거나 일부 비어 있음. 6개 모두 채워야 함.
- **아무 것도 안 바뀐다**: `GameBootstrap`의 Skin 슬롯에 에셋이 연결되지 않았음.
- **주사위 눈 스프라이트가 잘린다**: 정사각형 비율 권장. 셀은 정사각형이라 가로세로 다르면 늘어남.

## 7. 색상/치수만 조정
- 색/폰트 크기/셀 크기 등 기본값은 `Assets/Scripts/UI/UiTheme.cs` 상수에서 변경.
- 레이아웃 구조 변경이 필요하면 `Assets/Scripts/UI/`의 각 View 스크립트를 수정.

## 8. 향후 확장
- 스킨 구조를 유지한 채 프리팹 기반 뷰나 TextMeshPro로 확장 가능.
- 여러 테마(예: 라이트/다크, 시즌 스킨)를 여러 UiSkin 에셋으로 만들어 교체할 수 있다.
