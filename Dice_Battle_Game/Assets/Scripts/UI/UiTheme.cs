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
        public const int DiceFontSize = 72;
        public const int ScoreFontSize = 40;
        public const int StatusFontSize = 40;
    }
}
