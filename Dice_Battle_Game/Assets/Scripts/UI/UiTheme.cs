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
        public const int MenuStatsFontSize = 52;    // "전적"
        public const int MenuMissionFontSize = 46;  // "일일 미션" — 글자가 많아 한 단계 작게
        // 모든 메뉴 버튼이 같은 폭이다. 가장 긴 문구("게임 설명서")가 다 들어가면서
        // 그보다 과하게 넓지 않은 값 — 늘 이 값 하나만 조절하면 전부 같이 바뀐다.
        public const float MenuButtonWidth = 420f;
        public const float MenuButtonHeight = 120f;
        public const int MenuButtonGap = 40;
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

        // ---- 친선대전 로비 ----
        // 설명 텍스트 전용 크기 — DifficultySummaryFontSize와 값을 공유하면 나중에 그쪽만
        // 바꾸고 싶을 때 여기도 같이 바뀌어 버린다.
        public const int FriendlyDescFontSize = 50;
        public const float FriendlyEntryButtonWidth = 440f;
        public const float FriendlyEntryButtonHeight = 110f;
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
        // 아래 버튼이 네 개(2x2)로 늘어 한 줄 분량이 더 필요하다.
        // 여백 80 + 제목 90 + 소리 두 줄 200 + 간격 150 + 버튼 두 줄 220 = 740. 남는 80이 여유.
        public const float SettingsWindowHeight = 820f;
        public const int SettingsTitleFontSize = 60;
        public const int SettingsLabelFontSize = 44;
        public const float SliderTrackHeight = 34f;
        public const float SliderHandleSize = 58f;
        // 아래 버튼 공통 폭. 한 줄에 둘씩 들어간다.
        // 2칸 + 간격 1칸(30)이 창 안쪽 폭(1180 - 좌우 여백 60씩 = 1060)에 들어가야 한다.
        public const float SettingsFooterButtonWidth = 480f; // "튜토리얼 다시 보기"가 다 들어가는 선에서 최소로
        public const float SettingsFooterButtonHeight = 110f;
        public const int SettingsFooterGap = 30;

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

        // ---- 코인 ----
        // 코인 액수는 점수와 섞이지 않게 금색으로 뽑는다(특수 주사위와 같은 색).
        public static readonly Color Coin = new Color(1f, 0.86f, 0.35f, 1f);
        public const string CoinColorHex = "#FFDB59";

        // ---- 출석 창 ----
        public const float AttendanceWindowWidth = 1420f;
        public const float AttendanceWindowHeight = 620f;
        public const int AttendanceTitleFontSize = 58;
        public const int AttendanceBodyFontSize = 38;
        // 7칸을 한 줄로. 1420 - 좌우 여백 120 = 1300, (7 x 170) + (6 x 18) = 1298
        public const float AttendanceCellWidth = 170f;
        public const float AttendanceCellHeight = 200f;
        public const int AttendanceCellGap = 18;
        public const int AttendanceDayFontSize = 30;
        public const int AttendanceRewardFontSize = 44;
        // 이미 받은 칸. 아직 안 받은 칸(LineNormal, 알파 15/255)보다 더 흐리다.
        public static readonly Color AttendanceCellClaimed = new Color(1f, 1f, 1f, 5f / 255f);
        // 셀 모서리 곡률(Image의 Pixels Per Unit Multiplier). 값이 작을수록 더 둥글다.
        // 0.18은 내장 UISprite를 뜬 스킨 스프라이트 기준으로 맞춘 값이다.
        // 스킨이 비어 코드 생성 스프라이트로 대체되면 곡률이 달라 보일 수 있다.
        public const float AttendanceCellRoundness = 0.18f;

        // ---- 일일 미션 창 ----
        public const float MissionWindowWidth = 1360f;
        public const float MissionWindowHeight = 760f;
        public const int MissionTitleFontSize = 58;
        public const int MissionBodyFontSize = 38;
        public const float MissionRowHeight = 120f;
        public const int MissionRowSpacing = 16;
        public const int MissionLabelFontSize = 42;
        public const int MissionProgressFontSize = 32;
        public const float MissionButtonWidth = 200f;
        public const float MissionButtonHeight = 96f;
        public const int MissionButtonFontSize = 40;

        // ---- 결과 화면 보호권 버튼 ----
        public const float ResultButtonWidth = 400f;
        public const float ResultButtonHeight = 130f;
        // 버튼이 셋으로 늘 수 있어 줄 폭을 넉넉히 잡는다(400 x 3 + 간격 80 = 1280).
        public const float ResultButtonRowWidth = 1300f;
        public static readonly Color ProtectButton = new Color(0.72f, 0.52f, 0.16f, 1f);

        // ---- 전적 창 ----
        public const float StatsWindowWidth = 1300f;
        // 항목 6줄 + 제목 + 버튼이 창 안에 다 들어가야 하는 높이다.
        // 여백 72 + 제목 90 + 줄 456 + 버튼줄 100 + 간격 112 = 830. 남는 50은 버튼 위 여백.
        public const float StatsWindowHeight = 880f;
        public const int StatsTitleFontSize = 58;
        // 라벨은 값보다 한 단계 작고 흐리게 둬서 값이 먼저 읽히게 한다.
        public const int StatsLabelFontSize = 40;
        public const int StatsValueFontSize = 44;
        public const float StatsRowHeight = 76f;
        public const int StatsRowSpacing = 14;

        // ---- 튜토리얼 ----
        //
        // 안내 패널의 세로 위치. 보드가 화면을 꽉 채우므로 어디에 두든 무언가를 가리게 된다.
        // 그래서 "가리지 않을 곳"이 아니라 "지금 설명하는 대상을 피할 곳"을 고른다.
        // 세 값은 각각 첫째 줄 / 가운데 줄 / 셋째 줄 높이이며, 어느 쪽도 트레이를 덮지 않는다
        // (트레이는 y ≈ -294 아래). 줄 높이는 세로 레이아웃에서 계산된 값이라
        // TrayHeight나 TopBar 높이를 바꾸면 여기도 같이 옮겨야 한다.
        public const float TutorialPanelTopY = 310f;
        public const float TutorialPanelCenterY = 70f;
        public const float TutorialPanelBottomY = -190f;
        public const float TutorialPanelWidth = 1180f;
        public const float TutorialPanelHeight = 200f;
        public const int TutorialTextFontSize = 42;
        public const int TutorialHintFontSize = 28;
        public static readonly Color TutorialPanel = new Color(0.08f, 0.09f, 0.12f, 0.94f);
        public static readonly Color TutorialPanelEdge = new Color(0.62f, 0.42f, 0.18f, 1f);

        // 눌러야 할 곳을 감싸는 테두리. 채우지 않고 네 변만 그린다 —
        // 반투명한 판을 덮으면 정작 봐야 할 주사위 눈이 흐려진다.
        public static readonly Color TutorialRing = new Color(1f, 0.86f, 0.35f, 1f);
        public const float TutorialRingThickness = 8f;
        public const float TutorialRingPadding = 10f;   // 대상 바깥으로 얼마나 벌릴지
        public const float TutorialRingPulseSeconds = 0.9f;
        public const float TutorialRingMinAlpha = 0.30f;

        public const float TutorialSkipButtonWidth = 230f;
        public const float TutorialSkipButtonHeight = 92f;
        public const int TutorialSkipFontSize = 36;
        public static readonly Color TutorialSkipButton = new Color(0.24f, 0.25f, 0.30f, 1f);

        // ---- 튜토리얼 완료 화면 ----
        public const float TutorialDoneWindowWidth = 1200f;
        public const float TutorialDoneWindowHeight = 660f;
        public const int TutorialDoneTitleFontSize = 68;
        public const int TutorialDoneBodyFontSize = 42;
        public const float TutorialDoneButtonWidth = 480f;
        public const float TutorialDoneButtonHeight = 130f;

        // ---- 크레딧 창 ----
        public const float CreditsWindowWidth = 1300f;
        public const float CreditsWindowHeight = 760f;
        public const int CreditsTitleFontSize = 58;
        // 링크가 길어 본문은 설명서보다 작게 잡았다.
        public const int CreditsBodyFontSize = 34;
    }
}
