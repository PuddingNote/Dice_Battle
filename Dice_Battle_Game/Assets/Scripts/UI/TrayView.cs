using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 하단 가로 트레이(굴림판) + 그 위의 주사위.
    /// 새 주사위를 얻으면: 가운데에서 굴림 → 확정 → 해당 턴 방향(좌=나 / 우=상대)으로 부드럽게 이동.
    /// 리롤 시에는 주사위를 하나 더 굴려 첫 주사위 우측에 붙이고, 둘 중 하나를 클릭으로 고른다.
    /// 코루틴은 외부 MonoBehaviour(runner)에서 구동한다.
    /// </summary>
    public sealed class TrayView
    {
        private const int PairCount = 2;

        private readonly MonoBehaviour _runner;
        // [0] 항상 "현재 유효한" 주사위, [1] 리롤 후보.
        private readonly CellView[] _dice = new CellView[PairCount];
        private readonly Button[] _buttons = new Button[PairCount];
        private readonly Image _highlight;
        private readonly float _slideX;

        private readonly System.Random _rng = new System.Random();
        private Dice _last;
        // 리롤 후보. 이것을 고르면 _last가 된다.
        private Dice _candidate;
        private Coroutine _co;
        private bool _rolling;

        /// <summary>리롤 선택 중 주사위를 클릭했을 때(0=기존, 1=리롤 후보).</summary>
        public event Action<int> DiceClicked;

        public TrayView(Transform parent, MonoBehaviour runner)
        {
            _runner = runner;

            // 색은 흰색(기본값)으로 두어 스프라이트 원본색을 그대로 표시. Outline 없음.
            var tray = UiFactory.CreatePanel("Tray", parent, Color.white);
            UiSkin.Apply(tray, UiSkin.Tray, Color.white);
            UiFactory.SetSize(tray.gameObject, UiTheme.TrayWidth, UiTheme.TrayHeight);

            _highlight = CreateHighlight(tray.transform);

            // 주사위는 레이아웃이 아닌 수동 위치로 두어 좌/우 이동 연출이 가능하게 한다.
            for (int i = 0; i < PairCount; i++)
            {
                var cell = new CellView(tray.transform);
                var rt = cell.Rect;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(UiTheme.CellSize, UiTheme.CellSize);
                rt.anchoredPosition = Vector2.zero;

                var button = rt.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(AudioManager.PlayButton); // 리롤 주사위 선택도 클릭음
                button.onClick.AddListener(() => DiceClicked?.Invoke(IndexOf(cell)));
                _buttons[i] = button;

                _dice[i] = cell;
                cell.SetEmpty();
                cell.SetRaycast(false);
            }

            _slideX = UiTheme.TrayWidth * 0.5f - UiTheme.CellSize * 0.5f - 60f;
        }

        /// <summary>
        /// 리롤 선택 중임을 알리는 트레이 테두리.
        /// 라인 박스와 같은 스프라이트·같은 초록을 써서 "여기를 고르라"는 신호가 같은 의미로 읽히게 한다.
        /// 주사위보다 먼저 만들어 주사위 뒤에 깔린다.
        /// </summary>
        private static Image CreateHighlight(Transform tray)
        {
            var img = UiFactory.CreatePanel("Highlight", tray, UiTheme.LineHighlightSolid);
            img.raycastTarget = false;
            UiFactory.Stretch(img.rectTransform);
            float inset = UiTheme.TrayHighlightInset;
            img.rectTransform.offsetMin = new Vector2(inset, inset);
            img.rectTransform.offsetMax = new Vector2(-inset, -inset);

            if (UiSkin.LineNormal != null)
            {
                img.sprite = UiSkin.LineNormal;
                img.type = Image.Type.Sliced;
                img.color = UiTheme.LineHighlightSolid;
            }
            else
            {
                // 스킨이 없을 때의 폴백. 테두리 이미지가 없으니 반투명 초록으로 채운다.
                img.sprite = UiSprites.RoundedRect;
                img.type = Image.Type.Sliced;
                img.color = UiTheme.LineHighlight;
            }

            img.enabled = false;
            return img;
        }

        private RectTransform RectOf(int i) => _dice[i].Rect;

        /// <summary>
        /// 클릭한 주사위의 슬롯 번호를 <b>클릭 시점에</b> 다시 찾는다.
        /// <see cref="ResolvePickRoutine"/>이 선택 후 두 슬롯을 맞바꾸므로,
        /// 버튼을 만들 때의 번호를 캡처해 두면 그 다음 리롤부터 좌우가 뒤집힌 값이 전달된다
        /// (고른 주사위가 버려지는 버그).
        /// </summary>
        private int IndexOf(CellView cell)
        {
            for (int i = 0; i < PairCount; i++)
                if (ReferenceEquals(_dice[i], cell)) return i;
            return 0;
        }

        /// <summary>새 주사위가 굴러 제자리에 서는 중인가. 이 동안에는 입력을 받으면 안 된다.</summary>
        public bool IsRolling => _rolling;

        /// <summary>현재 트레이 주사위의 월드 위치(배치 연출 시작점).</summary>
        public Vector3 DieWorldPosition => RectOf(0).position;

        /// <summary>트레이 주사위를 즉시 숨긴다(배치 연출로 날아갈 때).</summary>
        public void HideDie()
        {
            StopAnim();
            _last = null;
            _candidate = null;
            SetPickable(false); // 트레이를 비우면 강조도 같이 꺼진다
            for (int i = 0; i < PairCount; i++) ResetDie(i);
        }

        private void StopAnim()
        {
            if (_co != null) { _runner.StopCoroutine(_co); _co = null; }
            _rolling = false;
            AudioManager.StopDiceShake(); // 굴림 도중 끊기면 소리도 같이 끊는다
        }

        private void ResetDie(int i)
        {
            _dice[i].SetEmpty();
            _dice[i].SetRaycast(false);
            var rt = RectOf(i);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        /// <summary>손패 주사위를 반영. towardLeft=true면 내 턴(좌측 이동), false면 상대 턴(우측).</summary>
        public void ShowPending(Dice die, bool towardLeft)
        {
            if (die == null)
            {
                HideDie();
                return;
            }

            if (!ReferenceEquals(die, _last))
            {
                _last = die;
                StopAnim();
                ResetDie(1);
                _rolling = true; // StartCoroutine 전에 세워야 같은 프레임의 입력도 막힌다
                _co = _runner.StartCoroutine(RollAndSlide(die.Value, die.IsSpecial, towardLeft));
            }
        }

        /// <summary>
        /// 리롤 후보를 트레이 가운데에서 굴려 기존 주사위 우측에 붙인다.
        /// 완료 후 두 주사위가 클릭 가능해진다(리롤은 플레이어 전용이라 항상 좌측 진영).
        /// </summary>
        public IEnumerator RollCandidateRoutine(Dice candidate)
        {
            _candidate = candidate;

            // 첫 주사위의 굴림 연출이 아직 돌고 있을 수 있으므로 먼저 최종 상태로 확정한다.
            FinalizeCurrent();

            var rt = RectOf(1);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            yield return RollFlicks(_dice[1], DiceSide.Player);

            _dice[1].SetDie(candidate.Value, candidate.IsSpecial, DiceSide.Player);
            rt.localScale = Vector3.one;
            yield return new WaitForSeconds(0.2f);

            yield return Move(rt, Vector2.zero, new Vector2(-_slideX + UiTheme.RerollPairGap, 0f), 0.35f);

            SetPickable(true);
        }

        /// <summary>리롤 선택 중 두 주사위의 클릭 가능 여부. 트레이 테두리 강조도 함께 켠다.</summary>
        public void SetPickable(bool on)
        {
            for (int i = 0; i < PairCount; i++)
            {
                _dice[i].SetRaycast(on);
                _buttons[i].interactable = on;
            }
            _highlight.enabled = on;
        }

        /// <summary>
        /// 선택 확정: 고르지 않은 주사위는 화면 아래로 직선 낙하해 사라지고,
        /// 고른 주사위는 평소 위치(좌측 슬롯)로 이동한다. 이후 인덱스 0이 유효 주사위가 된다.
        /// </summary>
        public IEnumerator ResolvePickRoutine(int picked)
        {
            SetPickable(false);

            int dropped = picked == 0 ? 1 : 0;
            var droppedRect = RectOf(dropped);
            var pickedRect = RectOf(picked);

            Vector2 dropFrom = droppedRect.anchoredPosition;
            Vector2 dropTo = new Vector2(dropFrom.x, -(UiTheme.TrayHeight * 0.5f + UiTheme.CellSize * 2f));
            Vector2 pickedFrom = pickedRect.anchoredPosition;
            Vector2 pickedTo = new Vector2(-_slideX, 0f);

            // 낙하와 좌측 정렬을 동시에 진행.
            float t = 0f;
            const float dur = 0.4f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                droppedRect.anchoredPosition = Vector2.Lerp(dropFrom, dropTo, k * k); // 가속 낙하
                pickedRect.anchoredPosition = Vector2.Lerp(pickedFrom, pickedTo, 1f - (1f - k) * (1f - k));
                yield return null;
            }
            pickedRect.anchoredPosition = pickedTo;

            ResetDie(dropped);

            // 유효 주사위가 항상 인덱스 0이 되도록 정리.
            if (picked == 1)
            {
                (_dice[0], _dice[1]) = (_dice[1], _dice[0]);
                (_buttons[0], _buttons[1]) = (_buttons[1], _buttons[0]);

                // 후보가 곧 현재 주사위다. 여기서 갱신하지 않으면 다음 리롤의
                // FinalizeCurrent가 슬롯 0을 리롤 전 값으로 되돌려 그린다.
                if (_candidate != null) _last = _candidate;
            }
            _candidate = null;
        }

        /// <summary>진행 중인 굴림 연출을 끊고 현재 주사위를 최종 상태(좌측 슬롯)로 스냅한다.</summary>
        private void FinalizeCurrent()
        {
            StopAnim();
            if (_last == null) return;

            _dice[0].SetDie(_last.Value, _last.IsSpecial, DiceSide.Player);
            var rt = RectOf(0);
            rt.anchoredPosition = new Vector2(-_slideX, 0f);
            rt.localScale = Vector3.one;
        }

        private IEnumerator RollAndSlide(int finalValue, bool special, bool towardLeft)
        {
            // 내 턴이면 플레이어(초록), 상대 턴이면 AI(빨강) 주사위 세트.
            DiceSide side = towardLeft ? DiceSide.Player : DiceSide.Ai;
            var rt = RectOf(0);
            rt.anchoredPosition = Vector2.zero;

            // 1) 가운데에서 굴림
            yield return RollFlicks(_dice[0], side);

            // 2) 확정
            _dice[0].SetDie(finalValue, special, side);
            rt.localScale = Vector3.one;
            yield return new WaitForSeconds(0.3f);

            // 3) 해당 턴 방향으로 부드럽게 이동
            yield return Move(rt, Vector2.zero, new Vector2(towardLeft ? -_slideX : _slideX, 0f), 0.4f);
            _co = null;
            _rolling = false;
        }

        private IEnumerator RollFlicks(CellView cell, DiceSide side)
        {
            AudioManager.PlayDiceShake();

            var rt = cell.Rect;
            const int flicks = 8;
            for (int i = 0; i < flicks; i++)
            {
                cell.SetDie(_rng.Next(1, 7), false, side);
                float s = 1f + 0.12f * Mathf.Sin((i + 1) / (float)flicks * Mathf.PI);
                rt.localScale = new Vector3(s, s, 1f);
                yield return new WaitForSeconds(0.045f);
            }

            // 눈이 확정되는 순간 굴림 소리를 끊는다(원본이 연출보다 길다).
            AudioManager.StopDiceShake();
        }

        private static IEnumerator Move(RectTransform rt, Vector2 from, Vector2 to, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                k = 1f - (1f - k) * (1f - k); // ease-out
                rt.anchoredPosition = Vector2.Lerp(from, to, k);
                yield return null;
            }
            rt.anchoredPosition = to;
        }
    }
}
