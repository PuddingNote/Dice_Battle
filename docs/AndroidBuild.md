# Android 빌드 세팅 참고사항 (Dice Battle)

Unity 6000.3.12f1 / URP 모바일 템플릿 기준. 실제 값 변경은 Unity Editor의
`Edit > Project Settings` 및 `File > Build Profiles`에서 수행한다.

## 1. 플랫폼 전환
- `File > Build Profiles`(구 Build Settings)에서 **Android** 선택 후
  `Switch Platform`. 최초 전환 시 텍스처 재임포트로 시간이 걸린다.

## 2. Player Settings 감사 결과 (2026-07-27 기준 현재 프로젝트 값)
| 항목 | 현재 값 | 상태 | 비고 |
|------|---------|------|------|
| Company Name | `onePixel` | ✅ | |
| Product Name | `Dice Battle` | ✅ | |
| **Package Name** | `com.onepixel.dicebattle` | ✅ | 전부 소문자(관례). **출시 후 변경 불가이므로 확정된 값** |
| Scripting Backend (Android) | **IL2CPP** | ✅ | 유지 |
| Target Architectures | **ARM64** | ✅ | 유지 (Play 필수) |
| Minimum API Level | API 25 | ✅ | 유지 |
| Target API Level | `0` = Automatic | ▲ | 출시 직전 Play 요구치(API 35) 충족 확인 |
| Api Compatibility Level | .NET Standard 2.1 | ✅ | 유지 |
| Active Input Handling | Input System(New) | ✅ | UI 입력 모듈과 일치 |
| Strip Engine Code | 켜짐 | ✅ | 용량 절감 |
| Version / Version Code | `0.1.0` / `1` | ▲ | 릴리스마다 versionCode 증가 필수 |
| Unity 스플래시 | 켜짐 | ▲ | Personal 라이선스는 제거 불가 |

> ✅ 이미 올바름 / ▲ 확인 권장 / ❌ 변경 필요.

## 3. 화면/입력
- **Orientation (설정 완료)**: `Default Orientation = Auto Rotation` +
  **Portrait / Portrait Upside Down 해제**, Landscape Left/Right 만 허용.
  가로 고정이면서 양쪽 방향 회전은 되는 상태.
- **Render Outside Safe Area = 켜짐**: 노치/카메라홀 영역까지 렌더한다.
  가로모드에선 좌우 끝을 덮으므로, 레터박스 검은 여백이 흡수하는지
  **실기기(특히 21:9 기기)에서 확인**할 것. 문제가 있으면 이 옵션을 끈다.
- **해상도 대응**: `LetterboxScaler`가 1920x1080 컨텐츠를 균등 스케일하고
  남는 영역은 검은 배경으로 채운다(코드 처리). 에디터 Game 뷰도 가로 비율로 확인.
- **입력 (정상)**: Active Input Handling = Input System Package(New).

## 3-1. 빌드 씬
- `EditorBuildSettings`에 `Assets/Scenes/SampleScene.unity` 1개가 **enabled** 로 등록됨. ✅

## 4. 그래픽 / URP
- Mobile 렌더러 에셋(`Assets/Settings/Mobile_RPAsset`, `Mobile_Renderer`)이
  Android Quality 레벨에 연결되어 있는지 `Project Settings > Quality`에서 확인.
- 2D 보드게임이므로 후처리/그림자 최소화로 발열·배터리 관리.

## 5. 빌드 산출물
- 개발/테스트: **APK** (`.apk`)
- 스토어 배포: **AAB** (`.aab`) — `Build App Bundle (Google Play)` 체크.
- 서명: 배포용은 keystore 생성 후
  `Player Settings > Publishing Settings`에서 서명 구성.
  keystore/비밀번호는 **git에 커밋하지 말 것** (.gitignore로 관리).

## 6. 최소 QA 체크리스트 (빌드 전)
- [x] Package Name 변경 (`com.onepixel.dicebattle`)
- [x] Company Name / Product Name 변경
- [x] Orientation 가로 고정 (Portrait 해제)
- [x] IL2CPP + ARM64
- [x] .NET Standard 2.1 / New Input System
- [x] 빌드 씬 등록 (SampleScene)
- [ ] 플랫폼을 Android 로 Switch Platform
- [ ] 콘솔 에러/경고 0
- [ ] EditMode 테스트 전부 통과 (`Window > General > Test Runner`)
- [ ] 실기기 터치 반응성 확인
- [ ] 실기기 노치/세이프에어리어 확인 (3절 참고)
- [ ] 한글 폰트 정상 출력 확인 (빌드 후 TMP Dynamic 아틀라스 동작)

## 7. 규칙/밸런스 검증 현황 (순수 로직)
- EditMode + dotnet 검증 러너로 규칙/AI/점수 로직 106개 항목 통과.
- 엣지 케이스(한쪽 필드 먼저 참=턴 스킵, 다중 제거, 상호 소멸, 특수 면역,
  무승부 라인, 배치 불가 예외 등) 자동 테스트로 커버.
- 자동대전 통계: 평균 27수/판·제거 4.2회/판 → 예상 플레이타임 2~4분(목표 2~10분 내).
