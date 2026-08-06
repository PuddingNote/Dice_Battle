using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiceBattle.UI
{
    /// <summary>주사위 스프라이트 세트 구분(플레이어=초록 / AI=빨강 등).</summary>
    public enum DiceSide { Player, Ai }

    /// <summary>
    /// UI 스킨(아트 교체용). 스프라이트/폰트 슬롯을 담는다.
    /// - 슬롯이 비어 있으면(null) 뷰는 기존 단색으로 렌더(현재 동작 유지).
    /// - 스프라이트를 지정하면 로직 수정 없이 해당 요소가 스프라이트로 바뀐다.
    /// 에디터 Project 창에서 우클릭 → Create → DiceBattle → UI Skin 으로 에셋 생성 후
    /// GameBootstrap 컴포넌트의 Skin 슬롯에 연결한다.
    /// </summary>
    [CreateAssetMenu(fileName = "UiSkin", menuName = "DiceBattle/UI Skin")]
    public sealed class UiSkin : ScriptableObject
    {
        /// <summary>현재 적용 중인 스킨(부트스트랩이 설정). null이면 단색 폴백.</summary>
        public static UiSkin Active;

        [Header("전역")]
        [Tooltip("권장: TMP 폰트 에셋(Font Asset Creator로 생성, Atlas Population = Dynamic)")]
        public TMP_FontAsset fontAsset;

        [Tooltip("대체: TTF 원본. fontAsset이 비어 있으면 이걸로 런타임에 동적 TMP 폰트를 만든다.")]
        public Font font;

        public Sprite screenBackground;

        [Header("패널")]
        [Tooltip("필드 패널(플레이어/상대 공통 프레임, 색으로 구분)")]
        public Sprite fieldPanel;
        public Sprite centerPanel;

        [Header("라인 행")]
        public Sprite lineNormal;
        public Sprite lineHighlight;

        [Header("셀")]
        public Sprite cellEmpty;
        public Sprite cellFilled;
        public Sprite cellSpecial;

        [Header("플레이어 주사위 눈 1~6 (6개 모두 지정 시 숫자 대신 사용)")]
        public Sprite[] playerDiceFaces;

        [Header("AI(상대) 주사위 눈 1~6")]
        public Sprite[] aiDiceFaces;

        [Header("버튼")]
        public Sprite button;

        [Tooltip("리롤 버튼 안쪽 아이콘(원형 화살표). 비우면 임시로 '리롤' 문구를 표시한다.")]
        public Sprite rerollIcon;

        [Tooltip("설정 버튼 안쪽 아이콘(톱니바퀴). 비우면 임시로 '설정' 문구를 표시한다.")]
        public Sprite settingsIcon;

        [Tooltip("설명서 페이지 넘김 화살표(오른쪽 방향 1장). 왼쪽은 좌우 반전해 재사용한다.")]
        public Sprite pageArrowIcon;

        [Header("창(설정/설명서)")]
        [Tooltip("창 배경. 비우면 코드로 만든 둥근 사각형을 쓴다.")]
        public Sprite windowPanel;

        [Header("볼륨 슬라이더")]
        public Sprite sliderTrack;
        public Sprite sliderFill;
        public Sprite sliderHandle;

        [Header("주사위 트레이(굴림판)")]
        public Sprite tray;

        [Header("승자 라인 표식")]
        [Tooltip("선택 사항. 비워두면 기본 표식인 ★ 를 사용한다(현재 채택된 표시).")]
        public Sprite winStamp;

        [Header("범용 둥근 패널")]
        [Tooltip("출석 칸 같은 둥근 배경에 쓰는 9-슬라이스 스프라이트. " +
                 "DiceBattle → 내장 UI 스프라이트 추출 메뉴가 자동으로 채워 준다. " +
                 "비우면 코드로 만든 둥근 사각형으로 대체된다.")]
        public Sprite roundedPanel;

        public Sprite DiceFace(DiceSide side, int value)
        {
            var arr = side == DiceSide.Ai ? aiDiceFaces : playerDiceFaces;
            if (arr != null && arr.Length == 6 && value >= 1 && value <= 6)
                return arr[value - 1];
            return null;
        }

        // --- 정적 안전 접근자 (Active가 null이면 null 반환) ---
        public static Sprite ScreenBackground => Active != null ? Active.screenBackground : null;
        public static Sprite FieldPanel => Active != null ? Active.fieldPanel : null;
        public static Sprite CenterPanel => Active != null ? Active.centerPanel : null;
        public static Sprite LineNormal => Active != null ? Active.lineNormal : null;
        public static Sprite LineHighlight => Active != null ? Active.lineHighlight : null;
        public static Sprite CellEmpty => Active != null ? Active.cellEmpty : null;
        public static Sprite CellFilled => Active != null ? Active.cellFilled : null;
        public static Sprite CellSpecial => Active != null ? Active.cellSpecial : null;
        public static Sprite Button => Active != null ? Active.button : null;
        public static Sprite RerollIcon => Active != null ? Active.rerollIcon : null;
        public static Sprite SettingsIcon => Active != null ? Active.settingsIcon : null;
        public static Sprite PageArrowIcon => Active != null ? Active.pageArrowIcon : null;
        public static Sprite WindowPanel => Active != null ? Active.windowPanel : null;
        public static Sprite SliderTrack => Active != null ? Active.sliderTrack : null;
        public static Sprite SliderFill => Active != null ? Active.sliderFill : null;
        public static Sprite SliderHandle => Active != null ? Active.sliderHandle : null;
        public static Sprite Tray => Active != null ? Active.tray : null;
        public static Sprite WinStamp => Active != null ? Active.winStamp : null;
        public static Sprite RoundedPanel => Active != null ? Active.roundedPanel : null;
        public static Sprite Face(DiceSide side, int value) => Active != null ? Active.DiceFace(side, value) : null;
        public static Font ActiveFont => Active != null ? Active.font : null;
        public static TMP_FontAsset ActiveFontAsset => Active != null ? Active.fontAsset : null;

        /// <summary>
        /// 이미지에 스프라이트를 적용한다.
        /// 스프라이트가 있으면 색은 흰색(원본색 그대로)으로 두고, 없으면 지정 색(단색)을 쓴다.
        /// (선택 강조 등 의도적 틴트는 호출부에서 색을 별도로 덮어쓴다.)
        /// </summary>
        public static void Apply(Image img, Sprite sprite, Color color)
        {
            if (img == null) return;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white; // 이미지 원본색 사용(틴트 없음)
            }
            else
            {
                img.sprite = null;
                img.type = Image.Type.Simple;
                img.color = color;
            }
        }
    }
}
