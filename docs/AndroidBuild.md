# Android 빌드 세팅 참고사항 (Dice Battle)

Unity 6000.3.12f1 / URP 모바일 템플릿 기준. 실제 값 변경은 Unity Editor의
`Edit > Project Settings` 및 `File > Build Profiles`에서 수행한다.

## 1. 플랫폼 전환
- `File > Build Profiles`(구 Build Settings)에서 **Android** 선택 후
  `Switch Platform`. 최초 전환 시 텍스처 재임포트로 시간이 걸린다.

## 2. Player Settings 감사 결과 (2026-07 기준 현재 프로젝트 값)
| 항목 | 현재 값 | 상태 | 조치 |
|------|---------|------|------|
| Company Name | `DefaultCompany` | ⚠️ | 팀/개발자명으로 변경 |
| Product Name | `Dice_Battle_Game` | ▲ | 선택(예: Dice Battle) |
| **Package Name** | `com.UnityTechnologies.com.unity.template.urpblank` | ❌ | **템플릿 기본값 → 반드시 `com.<company>.dicebattle` 로 변경** |
| Scripting Backend (Android) | **IL2CPP** | ✅ | 유지 |
| Target Architectures | **ARM64** | ✅ | 유지 (Play 필수) |
| Minimum API Level | API 25 | ✅ | 유지 |
| Target API Level | `0` = Automatic | ▲ | 출시 시 Play 요구 최소치 확인 |
| Api Compatibility Level | .NET Standard 2.1 | ✅ | 유지 |
| Active Input Handling | Input System(New) | ✅ | 유지 (UI 입력 모듈과 일치) |

> ✅ 이미 올바름 / ▲ 확인 권장 / ⚠️·❌ 변경 필요.
> **가장 중요: Package Name이 URP 템플릿 기본값이라 반드시 바꿔야 스토어 등록/설치가 정상.**

## 3. 화면/입력
- **Orientation (변경 필요)**: 현재 `Default Orientation = Auto Rotation` 이고
  **Portrait 포함 4방향 모두 허용** 상태. UI가 가로형이므로 세로를 막아야 한다.
  - `Player > Resolution and Presentation`에서
    **Default Orientation = Landscape Left** 로 고정,
    또는 Auto Rotation 유지 시 **Portrait / Portrait Upside Down 체크 해제**,
    Landscape Left/Right만 허용.
  - 에디터 Game 뷰도 가로 종횡비(예: 1920x1080)로 확인.
- **입력 (정상)**: Active Input Handling = Input System Package(New). UI가 새 Input System
  기반이라 일치. (Both 여도 무방)

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
- [ ] **Package Name 변경** (템플릿 기본값 → 실제 값) — 최우선
- [ ] Company Name 변경
- [ ] **Orientation 가로 고정**(Portrait 해제)
- [x] IL2CPP + ARM64 (이미 설정됨)
- [x] .NET Standard 2.1 / New Input System (이미 설정됨)
- [ ] 콘솔 에러/경고 0
- [ ] EditMode 테스트 전부 통과 (`Window > General > Test Runner`)
- [ ] 실기기 터치 반응성 확인

## 7. 규칙/밸런스 검증 현황 (순수 로직)
- EditMode + dotnet 검증 러너로 규칙/AI/점수 로직 106개 항목 통과.
- 엣지 케이스(한쪽 필드 먼저 참=턴 스킵, 다중 제거, 상호 소멸, 특수 면역,
  무승부 라인, 배치 불가 예외 등) 자동 테스트로 커버.
- 자동대전 통계: 평균 27수/판·제거 4.2회/판 → 예상 플레이타임 2~4분(목표 2~10분 내).
