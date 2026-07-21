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
        public static readonly Color LineHighlight = new Color(0.30f, 0.70f, 0.35f, 0.45f);
        public static readonly Color LineDisabled = new Color(1f, 1f, 1f, 0.03f);

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

        // 결과 오버레이 (검정 50% 반투명 — 게임 화면이 뒤로 비침)
        public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.5f);
        public static readonly Color Button = new Color(0.25f, 0.45f, 0.85f, 1f);

        // 치수(레퍼런스 해상도 1920x1080 가로형 기준)
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;
        public const float CellSize = 150f;
        public const float CellSpacing = 14f;
        public const float FieldWidth = 640f;   // 한 필드(3칸 + 점수) 열 너비
        public const int RootPadding = 28;      // 보드 루트 좌우 여백
        public const int RootSpacing = 12;      // 필드/중앙 사이 간격
        public const int DiceFontSize = 72;
        public const int ScoreFontSize = 40;
        public const int StatusFontSize = 40;
    }
}
