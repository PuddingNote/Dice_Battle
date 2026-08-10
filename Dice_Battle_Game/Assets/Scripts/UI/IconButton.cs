using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiceBattle.UI
{
    /// <summary>
    /// 이미지 아이콘이 들어가는 정사각형 버튼 묶음(리롤/설정/페이지 넘김 등).
    /// 스킨에 아이콘 스프라이트가 없으면 대체 문구를 대신 보여준다.
    /// </summary>
    public sealed class IconButton
    {
        public Button Button { get; }
        public Image Icon { get; }
        public TMP_Text Label { get; }

        public RectTransform Rect => (RectTransform)Button.transform;

        public IconButton(Button button, Image icon, TMP_Text label)
        {
            Button = button;
            Icon = icon;
            Label = label;
        }

        /// <summary>누를 수 있는지 설정. 비활성일 때는 아이콘/문구도 함께 흐려진다.</summary>
        public void SetInteractable(bool on)
        {
            Button.interactable = on;

            // 아이콘·문구는 버튼의 targetGraphic이 아니라 자동으로 흐려지지 않으므로 직접 처리.
            Color tint = on ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            if (Icon != null) Icon.color = tint;
            if (Label != null) Label.color = tint;
        }

        /// <summary>
        /// 클릭 가능 여부(Button.interactable)는 건드리지 않고 흐림 정도만 바꾼다.
        /// 상대 진영 상태 표시처럼 <b>애초에 누를 수 없는</b> 버튼의 "썼음/남음" 표시에 쓴다 —
        /// SetInteractable을 쓰면 그때마다 클릭 가능 여부까지 같이 바뀌어 버린다.
        /// </summary>
        public void SetDimmed(bool dimmed)
        {
            Color tint = dimmed ? new Color(1f, 1f, 1f, 0.3f) : Color.white;
            if (Icon != null) Icon.color = tint;
            if (Label != null) Label.color = tint;
        }

        /// <summary>아이콘을 좌우 반전한다(오른쪽 화살표 이미지를 왼쪽 화살표로 재사용).</summary>
        public void FlipIcon()
        {
            if (Icon == null) return;
            var s = Icon.rectTransform.localScale;
            Icon.rectTransform.localScale = new Vector3(-s.x, s.y, s.z);
        }
    }
}
