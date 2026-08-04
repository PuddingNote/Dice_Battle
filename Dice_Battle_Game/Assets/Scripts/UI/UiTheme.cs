using UnityEngine;

namespace DiceBattle.UI
{
    /// <summary>색상/치수 등 UI 상수 모음. 이후 폴리싱 단계에서 값만 조정한다.</summary>
    public static class UiTheme
    {
        // 배경/패널
        public static readonly Color Background = new Color(0.10f, 0.11f, 0.14f, 1f);
        public static readonly Color OpponentPanel = new Color(0.28f, 0.13f, 0.15f, 1f);
        public static readonly Color PlayerPanel = new Color(0.12f, 0.17f, 0.28f, 1f);
        public static readonly Color CenterPanel = new Color(0.14f, 0.15f, 0.18f, 1f);

        // 라인 행
        public static readonly Color LineNormal = new Color(1f, 1f, 1f, 0.06f);
        public static readonly Color LineHighlight = new Color(0.30f, 0.70f, 0.35f, 0.55f);
        // 스프라이트(라인 박스)에 곱해도 또렷한 불투명 초록 강조 틴트.
        public static readonly Color LineHighlightSolid = new Color(0.55f, 0.95f, 0.60f, 1f);
        public static readonly Color LineDisabled = new Color(1f, 1f, 1f, 0.03f);
        // 좌=내 라인(푸른 계열), 우=상대 라인(붉은 계열) — 편 구분용 배경 틴트
        public static readonly Color MyLine = new Color(0.20f, 0.30f, 0.46f, 0.55f);
        public static readonly Color OppLine = new Color(0.46f, 0.22f, 0.24f, 0.55f);

        // 셀
        public static readonly Color CellEmpty = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color CellFilled = new Color(0.93f, 0.94f, 0.97f, 1f);
        public static readonly Color CellSpecial = new Color(1f, 0.86f, 0.35f, 1f); // 특수 주사위 배경(금색)
        public static readonly Color CellSpecialOutline = new Color(0.85f, 0.6f, 0.05f, 1f);

        // 텍스트
        public static readonly Color DiceText = new Color(0.10f, 0.11f, 0.14f, 1f);
        public static readonly Color DiceTextOnSpecial = new Color(0.20f, 0.12f, 0f, 1f);
        public static readonly Color Label = new Color(0.90f, 0.92f, 0.96f, 1f);
        public static readonly Color LabelDim = new Color(0.65f, 0.68f, 0.74f, 1f);
        public static readonly Color WinText = new Color(0.45f, 0.85f, 0.5f, 1f);
        public static readonly Color LoseText = new Color(0.9f, 0.45f, 0.45f, 1f);

        // 주사위 트레이(굴림판)
        public static readonly Color Tray = new Color(0.16f, 0.22f, 0.15f, 1f);
        public static readonly Color TrayFrame = new Color(0.32f, 0.42f, 0.27f, 1f);

        // 결과 오버레이 (검정, alpha 250/255 ≈ 거의 불투명)
        public static readonly Color Overlay = new Color32(0, 0, 0, 250);
        public static readonly Color Button = new Color(0.25f, 0.45f, 0.85f, 1f);

        // 창(모달) 뒤를 덮는 반투명 막 — 뒤쪽 화면이 비쳐 보인다.
        public static readonly Color Backdrop = new Color32(0, 0, 0, 190);
        // 창 본체/테두리 (나무 느낌의 갈색 계열)
        public static readonly Color WindowPanel = new Color(0.19f, 0.13f, 0.09f, 1f);
        public static readonly Color WindowBorder = new Color(0.46f, 0.32f, 0.19f, 1f);

        // 슬라이더(볼륨)
        public static readonly Color SliderTrack = new Color(0.14f, 0.10f, 0.06f, 1f);
        public static readonly Color SliderFill = new Color(0.85f, 0.62f, 0.25f, 1f);
        public static readonly Color SliderHandle = new Color(0.97f, 0.87f, 0.62f, 1f);
        // ON/OFF 토글 버튼 색
        public static readonly Color ToggleOn = new Color(0.62f, 0.42f, 0.18f, 1f);
        public static readonly Color ToggleOff = new Color(0.24f, 0.20f, 0.17f, 1f);

        // 치수(레퍼런스 해상도 1920x1080 가로형 기준)
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;
        public const float CellSize = 150f;
        public const float CellSpacing = 14f;
        public const float FieldWidth = 640f;   // 한 필드(3칸 + 점수) 열 너비
        public const float LineBoxWidth = 500f;  // 라인 박스(3칸 컨테이너) 폭 — 주사위보다 여유
        public const float LineBoxHeight = 180f; // 라인 박스 높이
        public const int RootPadding = 28;      // 보드 루트 좌우 여백
        public const int RootSpacing = 12;      // 필드/중앙 사이 간격
        public const float TrayWidth = 1040f;   // 하단 가로 트레이 폭
        public const float TrayHeight = 230f;    // 하단 가로 트레이 높이
        // 리롤 선택 중 뜨는 트레이 강조 테두리를 트레이 안쪽으로 얼마나 들일지.
        // 트레이 스프라이트에 여백이 있어 0이면 테두리가 살짝 떠 보인다. (0으로 하는게 오히려 더 괜찮음)
        public const float TrayHighlightInset = 0f;

        // 리롤 버튼(트레이 좌측, 정사각형)
        public const float RerollButtonSize = 130f;  // 버튼 한 변
        public const float RerollButtonX = 260f;     // 화면 좌측 끝에서 버튼 중심까지
        public const float RerollIconInset = 22f;    // 버튼 안쪽 아이콘 여백
        // 리롤 선택 시 두 주사위 사이 간격(중심 간 거리)
        public const float RerollPairGap = CellSize + 30f;
        public const int DiceFontSize = 72;
        public const int ScoreFontSize = 40;
        public const int StatusFontSize = 40;
        // 상단 중앙 표시("점수 (Lv.난이도)") 글자 크기 — 이 값만 바꾸면 된다.
        public const int HeaderFontSize = 60;

        // 우측 상단 아이콘 버튼(설정) — 메인 화면/게임 화면 공통 위치
        public const float IconButtonSize = 110f;      // 버튼 한 변
        public const float IconButtonMarginX = 126f;   // 화면 오른쪽 끝에서 버튼까지 여백
        public const float IconButtonMarginY = 26f;    // 화면 위쪽 끝에서 버튼까지 여백
        public const float IconButtonInset = 20f;      // 버튼 안쪽 아이콘 여백

        // ---- 메인 메뉴 (글자 크기/버튼 크기는 여기서 조절) ----
        public const int MenuTitleFontSize = 92;    // "다이스 배틀"
        public const int MenuScoreFontSize = 52;    // "점수 80 · Lv.1"
        public const int MenuStartFontSize = 52;    // "게임 시작"
        public const int MenuManualFontSize = 52;   // "게임 설명서"
        public const float MenuStartButtonWidth = 540f;
        public const float MenuStartButtonHeight = 120f;
        public const float MenuManualButtonWidth = 540f;
        public const float MenuManualButtonHeight = 120f;
        public const int MenuBottomPadding = 90;    // 시작 버튼이 얼마나 아래에 붙는지
        // 남는 세로 공간을 제목 위(Head)와 제목~점수 사이(Title)가 이 비율로 나눠 갖는다.
        // Title 쪽을 키우면 제목이 위로 올라가고, Head 쪽을 키우면 제목이 아래로 내려온다.
        public const float MenuHeadSpacerWeight = 1f;
        public const float MenuTitleSpacerWeight = 1f;

        // ---- 난이도 선택 화면 ----
        // 카드 10장을 5열 2행 그리드로 놓는다. 세로 스크롤을 쓰면 잠긴 난이도가
        // 화면 밖으로 나가 목표가 안 보이므로, 전부 한 화면에 들어가게 한다.
        public const int DifficultyTitleFontSize = 68;
        public const int DifficultySummaryFontSize = 40;
        public const int DifficultyLevelFontSize = 56;
        public const int DifficultyCardInfoFontSize = 32;
        public const float DifficultyCardWidth = 320f;
        public const float DifficultyCardHeight = 290f;
        public const int DifficultyCardSpacing = 24;   // 카드 사이 가로 간격
        public const int DifficultyRowSpacing = 20;    // 두 행 사이 간격
        public const float DifficultyStartButtonWidth = 480f;
        public const float DifficultyBackButtonWidth = 300f;
        public const float DifficultyFooterButtonHeight = 120f;

        // 카드 상태별 배경. 잠긴 카드는 눌러도 반응이 없으므로 한눈에 구분되어야 한다.
        public static readonly Color DifficultyCardLocked = new Color(0.15f, 0.16f, 0.19f, 1f);
        public static readonly Color DifficultyCardUnlocked = new Color(0.22f, 0.26f, 0.34f, 1f);
        public static readonly Color DifficultyCardSelected = new Color(0.30f, 0.55f, 0.95f, 1f);

        // ---- 설정 창 ----
        public const float SettingsWindowWidth = 1180f;
        public const float SettingsWindowHeight = 700f;
        public const int SettingsTitleFontSize = 60;
        public const int SettingsLabelFontSize = 44;
        public const float SliderTrackHeight = 34f;
        public const float SliderHandleSize = 58f;
        // 아래 줄 [게임 설명서] [크레딧] [닫기] 공통 폭.
        // 3칸 + 간격 2칸(30)이 창 안쪽 폭(창 너비 - 좌우 여백 60씩)에 들어가야 한다.
        public const float SettingsFooterButtonWidth = 330f;

        // ---- 게임 설명서 창 ----
        public const float ManualWindowWidth = 1480f;
        public const float ManualWindowHeight = 860f;
        public const int ManualTitleFontSize = 58;
        public const int ManualBodyFontSize = 40;
        public const float ManualArrowSize = 100f;

        // ---- 강제 업데이트 창 ----
        public const float UpdateWindowWidth = 1300f;
        public const float UpdateWindowHeight = 640f;
        public const int UpdateTitleFontSize = 62;
        public const int UpdateBodyFontSize = 42;
        public const float UpdateButtonWidth = 460f;
        public const float UpdateButtonHeight = 130f;

        // ---- 크레딧 창 ----
        public const float CreditsWindowWidth = 1300f;
        public const float CreditsWindowHeight = 760f;
        public const int CreditsTitleFontSize = 58;
        // 링크가 길어 본문은 설명서보다 작게 잡았다.
        public const int CreditsBodyFontSize = 34;
    }
}
