using UnityEngine;

namespace DiceBattle.UI
{
    /// <summary>
    /// 고정 디자인 해상도(1920x1080)를 유지한 채 화면에 맞춰 균등 스케일한다.
    /// 화면 비율이 다르면 남는 영역은 (뒤의 검은 배경으로) 레터박스/필러박스 처리된다.
    /// → 어떤 기기 비율에서도 레이아웃이 변형되지 않고 항상 동일하게 보인다.
    /// </summary>
    public sealed class LetterboxScaler : MonoBehaviour
    {
        private RectTransform _rt;
        private int _lastW;
        private int _lastH;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            _lastW = 0;
            _lastH = 0;
            Apply();
        }

        private void Update()
        {
            // 화면 크기(회전/멀티윈도우 등)가 바뀔 때만 갱신.
            if (Screen.width == _lastW && Screen.height == _lastH) return;
            Apply();
        }

        private void Apply()
        {
            _lastW = Screen.width;
            _lastH = Screen.height;

            float scale = Mathf.Min(
                _lastW / UiTheme.ReferenceWidth,
                _lastH / UiTheme.ReferenceHeight);
            if (scale <= 0f) scale = 1f;

            _rt.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
