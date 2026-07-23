using UnityEngine;
using UnityEngine.UI;

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

        [Header("주사위 트레이(굴림판)")]
        public Sprite tray;

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
        public static Sprite Tray => Active != null ? Active.tray : null;
        public static Sprite Face(DiceSide side, int value) => Active != null ? Active.DiceFace(side, value) : null;
        public static Font ActiveFont => Active != null ? Active.font : null;

        /// <summary>
        /// 이미지에 스프라이트를 적용한다. sprite가 null이면 단색, 있으면 스프라이트(9-slice)에 color를 틴트로 적용.
        /// </summary>
        public static void Apply(Image img, Sprite sprite, Color color)
        {
            if (img == null) return;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.color = color;
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
