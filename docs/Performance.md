# 성능 · 발열 · 용량

모바일에서 발열과 배터리를 좌우하는 설정, 그리고 그 값이 왜 그렇게 되어 있는지 정리한다.

---

## 1. 이 게임은 3D를 하나도 그리지 않는다

가장 먼저 알아야 할 사실이다. 화면에 보이는 것은 **전부 `ScreenSpaceOverlay` uGUI**다.

- 캔버스는 [GameBootstrap.cs](../Dice_Battle_Game/Assets/Scripts/UI/GameBootstrap.cs)의
  `CreateCanvas()`가 만드는 단 하나뿐이고 `RenderMode.ScreenSpaceOverlay`다.
- 그 첫 자식이 화면 전체를 덮는 불투명한 검은 `Letterbox` 판이다.
- 씬에는 렌더러가 **하나도** 없다. 런타임에 만드는 것도 없다.

`ScreenSpaceOverlay` 캔버스는 카메라를 거치지 않고 모든 렌더링이 끝난 뒤에 그려진다.
**즉 카메라가 그린 결과는 단 1픽셀도 화면에 나오지 않는다.**

그래서 카메라·라이팅·포스트프로세싱 관련 설정은 전부 꺼도 화면이 달라지지 않는다.
반대로 켜 두면 보이지도 않는 것을 매 프레임 그리느라 GPU와 배터리만 쓴다.

> ⚠️ **나중에 3D나 월드 스페이스 요소를 넣는다면** 이 문서의 2·3절을 되돌려야 한다.
> 그 전까지는 전부 꺼 두는 것이 맞다.

---

## 2. 카메라 (`SampleScene.unity`)

| 설정 | 값 | 이유 |
|---|---|---|
| Clear Flags | **Solid Color** (검정) | Skybox는 매 프레임 풀스크린 셰이더를 돌린다 |
| Culling Mask | **Nothing** | 그릴 것이 없다. 컬링 자체를 건너뛴다 |
| HDR | **끔** | 켜면 RGBA16F 중간 렌더타겟이 강제로 생긴다 |
| MSAA | **끔** | |
| Post Processing | **끔** | 아래 3절 참고 |
| Occlusion Culling | **끔** | 렌더러가 없어 계산할 것이 없다 |
| Render Shadows | **끔** | |

`Directional Light`와 `Global Volume` 게임오브젝트는 **비활성(`m_IsActive: 0`)** 으로
두었다. 지우지 않은 것은 나중에 3D를 넣을 때 되살리기 쉽게 하기 위해서다.

**카메라 자체를 지우거나 끄지는 않았다.** 카메라가 없으면 백버퍼가 클리어되지 않아
타일 기반 모바일 GPU에서 이전 프레임 잔상이 남을 수 있다. 단색 클리어는 타일
렌더러에서 사실상 공짜(로드 액션 하나)이므로 그대로 두는 편이 안전하다.

---

## 3. 무엇이 낭비되고 있었나

개선 전 씬은 매 프레임 아래를 전부 실행하고 있었다. 결과는 검은 판에 100% 가려졌다.

- 풀스크린 스카이박스 셰이더
- HDR 중간 렌더타겟 (RGBA16F)
- **Bloom, 고품질 필터링 켜짐** — HDR 타겟을 여러 단계로 다운/업샘플
- Tonemapping, Vignette
- Directional Light 소프트 섀도우
- Render Scale 0.8 → 중간 텍스처 + 최종 블릿 강제

모바일 발열의 주된 원인은 연산량이 아니라 **메모리 대역폭**이고, HDR 블룸 체인은
대역폭을 가장 많이 먹는 작업이다. 보이지도 않는 것에 쓰고 있었다.

---

## 4. URP 에셋 (`Mobile_RPAsset.asset`)

3D를 그리지 않으므로 라이팅·그림자 기능을 전부 껐다. 화면에는 영향이 없고,
**컴파일되는 셰이더 배리언트가 줄어 빌드 용량과 셰이더 로딩 시간도 함께 준다.**

껀 것: HDR, 메인/추가 라이트, 모든 그림자, 혼합 라이팅, 라이트 쿠키, 라이트 레이어,
리플렉션 프로브 블렌딩/박스 프로젝션, LOD 크로스페이드, 터레인 홀.

`Render Scale`은 0.8 → **1.0**으로 되돌렸다. Overlay UI는 렌더 스케일의 영향을
받지 않으므로 0.8은 화질 이득이 전혀 없었고, 오히려 중간 렌더타겟을 강제로 만들어
백버퍼 직행 렌더링을 막고 있었다.

---

## 5. 프레임레이트

`Application.targetFrameRate = 60` ([GameBootstrap.cs](../Dice_Battle_Game/Assets/Scripts/UI/GameBootstrap.cs)).
안드로이드는 `QualitySettings`의 vSync를 무시하고 이 값만 본다.

턴제 게임이라 **입력 대기 중에는 화면이 완전히 정지**한다. 이때 30fps로 낮추면
체감 저하 없이 배터리를 아낄 수 있다. 잠금 지점도 이미 깔끔하게 잡혀 있다 —
[GameController.cs](../Dice_Battle_Game/Assets/Scripts/UI/GameController.cs)의
`LockInput()`(연출 시작)과 `SetHumanInput()`(입력 대기)이 정확한 경계고,
스크롤 UI가 아예 없어 저프레임이 티 날 곳도 없다.

**지금은 적용하지 않았다.** 60 고정을 유지하기로 했다. 나중에 필요하면 위 두
지점에서 `Application.targetFrameRate`만 바꾸면 된다.

---

## 6. 오디오

| 파일 | 설정 | 이유 |
|---|---|---|
| `bgm.wav` | Vorbis, q0.5, **Compressed In Memory**, 스테레오 | |
| SFX 전부 | Vorbis, q0.6, Decompress On Load, 모노 | 짧아서 재생 시 디코딩 비용 없음 |

BGM은 **Streaming → Compressed In Memory**로 바꿨다. 스트리밍은 재생 내내 디스크
I/O가 계속 돌아 배터리를 쓴다. 3분짜리 루프 BGM은 빌드에서 3~4MB 정도라
메모리에 압축 상태로 올려두는 편이 낫다. `preloadAudioData`도 켜서 첫 재생이
끊기지 않게 했다.

품질은 0.6 → 0.5. 되돌리려면 `bgm.wav.meta`의 `quality` 한 줄만 고치면 된다.

**모노 변환은 하지 않았다.** 용량 이득이 1.7MB 남짓인데 이어폰으로 들으면 차이가
난다. 원본 `bgm.wav`는 30MB 무압축이지만 빌드에는 Vorbis로 들어가므로 APK 용량과는
무관하다. 저장소 용량이 신경 쓰이면 원본을 ogg로 바꿔 두면 된다.

---

## 7. 텍스처

**소스 PNG 크기와 빌드에 들어가는 크기는 전혀 다르다.** 빌드에서는 ETC2로 압축되어
가로×세로 픽셀 수에만 비례한다(대략 1바이트/픽셀). 512×512 한 장이 PNG로는 12KB여도
빌드에서는 256KB를 차지한다.

주사위 12장과 아이콘 2개는 소스가 512×512인데, 실제로는 전부
`UiTheme.CellSize = 150`(아이콘은 110~130)으로 그려진다. 1920×1080 기준 좌표라
`LetterboxScaler`가 최대로 키워도 고DPI 기기에서 300px 남짓이다. **512는 과했다.**

| 텍스처 | maxTextureSize | 렌더 크기 |
|---|---|---|
| 주사위 12장 | **256** | 150 (최대 ~300) |
| `gear_icon`, `refresh_icon` | **256** | 110 ~ 130 |
| `wood_table_bg_rings` | 2048 (원본 1920×1080) | 전체 화면 |
| `line_box`, `roll_tray`, `button_bg` | 2048 (원본 그대로) | 가로로 늘여 쓰는 9-슬라이스 |

512 → 256은 픽셀 수가 1/4이 되므로 그 14장의 빌드 용량도 1/4이 된다.
되돌리려면 각 `.meta`의 `maxTextureSize`만 바꾸면 된다.

배경 텍스처가 가장 크다(ETC2로 약 2MB). **크런치 압축**을 켜면 배포 용량이 크게
줄지만 나뭇결에 아티팩트가 생길 수 있고 로딩 시 CPU를 쓴다. 화질을 지키는 쪽을
택해 켜지 않았다.

---

## 8. 패키지

코드가 실제로 쓰는 것은 `UnityEngine`, uGUI, TextMeshPro, Input System,
`UnityWebRequest`뿐이다. 물리·애니메이션·파티클·NavMesh·Timeline 사용처는 0건이다.

제거한 최상위 패키지: Visual Scripting, Timeline, AI Navigation,
Multiplayer Center, Version Control(collab-proxy).

이 중 **Visual Scripting이 가장 크다.** 리플렉션 기반이라 코드 스트리핑이 잘 듣지
않는 런타임 어셈블리를 통째로 싣는다.

내장 모듈은 `stripEngineCode`가 이미 켜져 있어 IL2CPP가 안 쓰는 것을 대부분
알아서 걷어낸다. **그래서 모듈 목록을 손보는 이득은 크지 않다.** 명백한 말단
모듈만 지웠고, 다른 패키지가 의존할 수 있는 것(physics, animation, director,
imageconversion 등)은 남겼다. 깨질 위험 대비 이득이 없다.

`packages-lock.json`은 유니티가 프로젝트를 열 때 다시 만든다.

---

## 9. 유니티 내장 UI 스프라이트는 코드로 쓰면 안 된다

`UISprite`, `Background` 같은 내장 UI 스프라이트는 `unity_builtin_extra`에 들어 있어
**에디터에서만** 읽힌다. `Resources.GetBuiltinResource`로 코드에서 참조하면 에디터에서는
멀쩡하다가 **빌드에서 null이 된다.** 실기기에서만 모서리가 각지게 나오는 식으로
조용히 깨지므로 알아채기도 늦다.

이 프로젝트는 프리팹이 없어 인스펙터로 참조를 걸어 둘 수도 없다.

그래서 **DiceBattle → 내장 UI 스프라이트 추출** 메뉴를 두었다. 에디터에서 픽셀을
그대로 떠서 `Assets/Arts/ui_rounded_panel.png`로 저장하고 `UiSkin.roundedPanel`에
자동으로 연결한다. 저장된 뒤로는 평범한 프로젝트 에셋이라 빌드에 정상적으로 들어간다.

**9-슬라이스 경계와 PPU도 원본 값을 그대로 옮긴다.** 이 값이 틀리면 모서리가 늘어나
전혀 다른 모양이 되므로, 눈대중으로 비슷한 그림을 만드는 것으로는 대체할 수 없다.

같은 이유로 [UiSprites.cs](../Dice_Battle_Game/Assets/Scripts/UI/UiSprites.cs)가 둥근
사각형을 코드로 생성한다. 스킨이 비어 있을 때의 폴백이다.

---

## 10. 그 밖

- `accelerometerFrequency: 0` — 가속도 센서를 쓰지 않는데 초당 60회 폴링하고 있었다.
- `stripEngineCode: 1`, IL2CPP, ARM64, Target SDK 36 — 이미 잘 잡혀 있다.
- 셰이더 배리언트 스트리핑(`UniversalRenderPipelineGlobalSettings.asset`)도 이미 전부
  켜져 있다. 손댈 것이 없다.
- `GraphicsSettings.asset`의 `m_AlwaysIncludedShaders`는 유니티 기본값 그대로 두었다.
  이득이 수십 KB 수준인데 `UI/Default`를 잘못 빼면 UI가 통째로 안 그려진다.
- `Assets/Plugins/iOS` 5MB는 안드로이드 빌드에 들어가지 않는다. 저장소만 차지한다.
- 템플릿 잔재였던 `Assets/TutorialInfo/`와 `Assets/Readme.asset`은 지웠다.

---

## 11. 관련 파일

- [SampleScene.unity](../Dice_Battle_Game/Assets/Scenes/SampleScene.unity) — 카메라, 라이트, 볼륨
- [Mobile_RPAsset.asset](../Dice_Battle_Game/Assets/Settings/Mobile_RPAsset.asset) — URP 모바일 설정
- [manifest.json](../Dice_Battle_Game/Packages/manifest.json) — 패키지 목록
- [GameBootstrap.cs](../Dice_Battle_Game/Assets/Scripts/UI/GameBootstrap.cs) — 프레임레이트, 캔버스 생성
