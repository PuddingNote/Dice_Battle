using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 하단 가로 트레이(굴림판) + 그 위의 주사위.
    /// 새 주사위를 얻으면: 가운데에서 굴림 → 확정 → 해당 턴 방향(좌=나 / 우=상대)으로 부드럽게 이동.
    /// 코루틴은 외부 MonoBehaviour(runner)에서 구동한다.
    /// </summary>
    public sealed class TrayView
    {
        private readonly MonoBehaviour _runner;
        private readonly CellView _die;
        private readonly RectTransform _dieRect;
        private readonly float _slideX;

        private readonly System.Random _rng = new System.Random();
        private Dice _last;
        private Coroutine _co;

        public TrayView(Transform parent, MonoBehaviour runner)
        {
            _runner = runner;

            var tray = UiFactory.CreatePanel("Tray", parent, UiTheme.Tray);
            UiSkin.Apply(tray, UiSkin.Tray, UiTheme.Tray);
            UiFactory.SetSize(tray.gameObject, UiTheme.TrayWidth, UiTheme.TrayHeight);
            var frame = tray.gameObject.AddComponent<Outline>();
            frame.effectColor = UiTheme.TrayFrame;
            frame.effectDistance = new Vector2(6f, -6f);

            // 주사위는 레이아웃이 아닌 수동 위치로 두어 좌/우 이동 연출이 가능하게 한다.
            _die = new CellView(tray.transform);
            _dieRect = _die.Rect;
            _dieRect.anchorMin = new Vector2(0.5f, 0.5f);
            _dieRect.anchorMax = new Vector2(0.5f, 0.5f);
            _dieRect.pivot = new Vector2(0.5f, 0.5f);
            _dieRect.sizeDelta = new Vector2(UiTheme.CellSize, UiTheme.CellSize);
            _dieRect.anchoredPosition = Vector2.zero;

            _slideX = UiTheme.TrayWidth * 0.5f - UiTheme.CellSize * 0.5f - 60f;
            _die.SetEmpty();
        }

        /// <summary>손패 주사위를 반영. towardLeft=true면 내 턴(좌측 이동), false면 상대 턴(우측).</summary>
        public void ShowPending(Dice die, bool towardLeft)
        {
            if (die == null)
            {
                _last = null;
                if (_co != null) { _runner.StopCoroutine(_co); _co = null; }
                _die.SetEmpty();
                _dieRect.anchoredPosition = Vector2.zero;
                _dieRect.localScale = Vector3.one;
                return;
            }

            if (!ReferenceEquals(die, _last))
            {
                _last = die;
                if (_co != null) _runner.StopCoroutine(_co);
                _co = _runner.StartCoroutine(RollAndSlide(die.Value, die.IsSpecial, towardLeft));
            }
        }

        private IEnumerator RollAndSlide(int finalValue, bool special, bool towardLeft)
        {
            _dieRect.anchoredPosition = Vector2.zero;

            // 1) 가운데에서 굴림
            const int flicks = 8;
            for (int i = 0; i < flicks; i++)
            {
                _die.SetDie(_rng.Next(1, 7), false);
                float s = 1f + 0.12f * Mathf.Sin((i + 1) / (float)flicks * Mathf.PI);
                _dieRect.localScale = new Vector3(s, s, 1f);
                yield return new WaitForSeconds(0.045f);
            }

            // 2) 확정
            _die.SetDie(finalValue, special);
            _dieRect.localScale = Vector3.one;
            yield return new WaitForSeconds(0.3f);

            // 3) 해당 턴 방향으로 부드럽게 이동
            Vector2 from = Vector2.zero;
            Vector2 to = new Vector2(towardLeft ? -_slideX : _slideX, 0f);
            float t = 0f;
            const float dur = 0.4f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                k = 1f - (1f - k) * (1f - k); // ease-out
                _dieRect.anchoredPosition = Vector2.Lerp(from, to, k);
                yield return null;
            }
            _dieRect.anchoredPosition = to;
            _co = null;
        }
    }
}
