# Android 빌드 세팅 참고사항 (Dice Battle)

Unity 6000.3.12f1 / URP 모바일 템플릿 기준. 실제 값 변경은 Unity Editor의
`Edit > Project Settings` 및 `File > Build Profiles`에서 수행한다.

## 1. 플랫폼 전환
- `File > Build Profiles`(구 Build Settings)에서 **Android** 선택 후
  `Switch Platform`. 최초 전환 시 텍스처 재임포트로 시간이 걸린다.

## 2. Player Settings 핵심 항목
| 항목 | 권장값 | 비고 |
|------|--------|------|
| Company Name | (팀/개발자명) | 현재 `DefaultCompany` |
| Product Name | Dice Battle | 현재 `Dice_Battle_Game` |
| Package Name (applicationIdentifier) | `com.<company>.dicebattle` | **현재 비어 있음 → 반드시 설정** |
| Scripting Backend | **IL2CPP** | 구글플레이 64비트 필수 요건 |
| Target Architectures | **ARM64** (ARMv7 선택) | Play 스토어는 ARM64 필수 |
| Minimum API Level | API 25 (Android 7.0) | 현재값 유지 가능 |
| Target API Level | **Automatic (highest installed)** 또는 Play 최신 요구치 | 현재 `0`(미설정) → 설정 필요 |
| Api Compatibility Level | .NET Standard 2.1 | 순수 로직 호환 |

> Play 스토어 신규 앱은 Target API Level을 매년 상향하는 정책이 있으므로,
> 출시 시점의 구글 요구 최소 Target API를 확인해 맞춘다.

## 3. 화면/입력
- **Orientation**: 세로 고정(Portrait) 권장 (3x3 마주보는 보드가 세로에 적합).
  `Resolution and Presentation > Default Orientation = Portrait`.
- 입력: Input System 1.19.0 사용. Active Input Handling이
  `Input System Package (New)` 또는 `Both`인지 확인.

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
- [ ] Package Name 설정 완료
- [ ] IL2CPP + ARM64 확인
- [ ] Portrait 고정 확인
- [ ] 콘솔 에러/경고 0
- [ ] EditMode 테스트 전부 통과 (`Window > General > Test Runner`)
- [ ] 실기기 터치 반응성 확인
