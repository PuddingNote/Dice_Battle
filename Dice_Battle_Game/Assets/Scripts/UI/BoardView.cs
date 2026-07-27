using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 보드 전체 뷰(가로형).
    /// 상단: 난이도/상태 / 중앙: 3줄(내 라인 [점수 ◀▶ 점수] 상대 라인) / 하단: 가로 주사위 트레이.
    /// 런타임에 코드로 UI를 구성한다.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        private PlayerId _humanId;
        private PlayerId _aiId;

        private readonly MatchRowView[] _rows = new MatchRowView[Field.LineCount];
        private TrayView _tray;
        private TMP_Text _levelText;
        private TMP_Text _statusText;

        private GameObject _root;
        private RectTransform _fxLayer;
        private GameObject _resultOverlay;
        private TMP_Text _resultText;
        private Button _restartButton;
        private Button _menuButton;

        public event Action<PlayerId, int> LineClicked;
        public event Action RestartClicked;
        public event Action MenuClicked;

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
        }

        public void Build(RectTransform root, PlayerId humanId)
        {
            _humanId = humanId;
            _aiId = humanId.Other();

            var bg = UiFactory.CreateStretchPanel("BoardRoot", root, UiTheme.Background);
            UiSkin.Apply(bg, UiSkin.ScreenBackground, UiTheme.Background);
            _root = bg.gameObject;
            UiFactory.AddVerticalLayout(bg.gameObject, 12, new RectOffset(24, 24, 12, 12));

            BuildTopBar(bg.transform);
            BuildRows(bg.transform);
            BuildTrayArea(bg.transform);

            // 배치/제거 이동 연출용 FX 레이어(레이아웃 영향 없음, 게임 위·결과창 아래).
            var fx = UiFactory.CreateStretchPanel("FxLayer", bg.rectTransform, new Color(0, 0, 0, 0));
            UiFactory.IgnoreLayout(fx.gameObject);
            UiFactory.Stretch(fx.rectTransform);
            fx.raycastTarget = false;
            _fxLayer = fx.rectTransform;

            BuildResultOverlay(bg.rectTransform);
        }

        private void BuildTopBar(Transform parent)
        {
            var top = UiFactory.CreateRect("TopBar", parent);
            UiFactory.AddHorizontalLayout(top.gameObject, 16, new RectOffset(12, 12, 0, 0));
            UiFactory.SetPreferredHeight(top.gameObject, 96f);

            _levelText = UiFactory.CreateText("Level", top.transform, "",
                UiTheme.ScoreFontSize, UiTheme.WinText, TextAnchor.MiddleLeft);
            UiFactory.SetSize(_levelText.gameObject, 320f, 92f);

            _statusText = UiFactory.CreateText("Status", top.transform, "",
                36, UiTheme.Label, TextAnchor.MiddleRight);
            UiFactory.SetWrap(_statusText, true);
            UiFactory.SetFlexible(_statusText.gameObject);
        }

        private void BuildRows(Transform parent)
        {
            var rows = UiFactory.CreateRect("Rows", parent);
            var v = UiFactory.AddVerticalLayout(rows.gameObject, 14, new RectOffset(0, 0, 0, 0));
            v.childForceExpandHeight = true;
            UiFactory.SetFlexible(rows.gameObject);

            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i] = new MatchRowView(rows.transform, i, _humanId, _aiId, this);
                _rows[i].Clicked += (f, idx) => LineClicked?.Invoke(f, idx);
            }
        }

        private void BuildTrayArea(Transform parent)
        {
            var trayRow = UiFactory.CreateRect("TrayRow", parent);
            UiFactory.AddHorizontalLayout(trayRow.gameObject, 0, new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(trayRow.gameObject, UiTheme.TrayHeight + 16f);
            _tray = new TrayView(trayRow.transform, this);
        }

        private void BuildResultOverlay(RectTransform root)
        {
            var overlay = UiFactory.CreateStretchPanel("ResultOverlay", root, UiTheme.Overlay);
            UiFactory.IgnoreLayout(overlay.gameObject);
            UiFactory.Stretch(overlay.rectTransform);
            UiFactory.AddVerticalLayout(overlay.gameObject, 40, new RectOffset(60, 60, 120, 120));
            _resultOverlay = overlay.gameObject;

            _resultText = UiFactory.CreateText("ResultText", overlay.transform, "",
                56, UiTheme.Label, TextAnchor.MiddleCenter);
            UiFactory.SetWrap(_resultText, true);
            UiFactory.SetSize(_resultText.gameObject, 900f, 640f);

            var buttonRow = UiFactory.CreateRect("Buttons", overlay.transform);
            UiFactory.AddHorizontalLayout(buttonRow.gameObject, 40, new RectOffset(0, 0, 0, 0));
            UiFactory.SetSize(buttonRow.gameObject, 900f, 150f);

            _restartButton = UiFactory.CreateButton("RestartButton", buttonRow.transform, UiTheme.Button);
            UiFactory.SetSize(_restartButton.gameObject, 400f, 130f);
            var restartText = UiFactory.CreateText("Label", _restartButton.transform, "다시 하기",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(restartText.rectTransform);
            _restartButton.onClick.AddListener(() => RestartClicked?.Invoke());

            _menuButton = UiFactory.CreateButton("MenuButton", buttonRow.transform, UiTheme.CenterPanel);
            UiFactory.SetSize(_menuButton.gameObject, 400f, 130f);
            var menuText = UiFactory.CreateText("Label", _menuButton.transform, "메뉴로",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(menuText.rectTransform);
            _menuButton.onClick.AddListener(() => MenuClicked?.Invoke());

            _resultOverlay.SetActive(false);
        }

        // ---- 렌더링 ----

        public void Render(GameState state)
        {
            for (int i = 0; i < _rows.Length; i++)
                _rows[i].Render(state);
        }

        /// <summary>트레이 주사위 갱신(굴림/이동). 배치 연출과 겹치지 않도록 별도 호출.</summary>
        public void RefreshTray(GameState state)
        {
            _tray.ShowPending(state.PendingDice, state.CurrentPlayer == _humanId);
        }

        public void SetStatus(string message) => _statusText.text = message;

        public void SetLevelInfo(int level) => _levelText.text = $"AI 난이도  Lv{level}";

        // ---- 하이라이트 ----

        public void ClearHighlights()
        {
            for (int i = 0; i < _rows.Length; i++)
                _rows[i].ClearHighlights();
        }

        public void HighlightPrimary(GameState state)
        {
            ClearHighlights();
            int v = state.PendingDice != null ? state.PendingDice.Value : 0;
            for (int i = 0; i < _rows.Length; i++)
            {
                bool mySpace = state.Field(_humanId)[i].HasSpace;
                if (mySpace) _rows[i].SetSelectable(_humanId, true);

                // 제거 가능(내 라인 공간 + 상대 같은 라인에 제거 가능한 동일 값)하면 상대 라인도 강조.
                if (mySpace && state.Field(_aiId)[i].HasRemovableValue(v))
                    _rows[i].SetSelectable(_aiId, true);
            }
        }

        public void HighlightExtra(GameState state)
        {
            ClearHighlights();
            for (int i = 0; i < _rows.Length; i++)
            {
                if (state.Field(_humanId)[i].HasSpace) _rows[i].SetSelectable(_humanId, true);
                if (state.Field(_aiId)[i].HasSpace) _rows[i].SetSelectable(_aiId, true);
            }
        }

        // ---- 연출(트레이 → 라인 이동) ----

        /// <summary>
        /// 트레이 주사위가 삽입 위치(insertIndex)로 날아가 배치되는 연출.
        /// 같은 값 그룹화로 뒤로 밀리는 기존 주사위들(shift*)은 동시에 옆 칸으로 이동.
        /// </summary>
        public IEnumerator PlaceGroupedFxRoutine(PlayerId field, int line, int insertIndex,
            int value, bool special, DiceSide side,
            int[] shiftValues, DiceSide[] shiftSides, bool[] shiftSpecial)
        {
            var row = _rows[line];
            int shiftCount = shiftValues.Length;

            // 새 주사위 칸 + 밀려나는 칸들을 비우고(도착 전), 밀리는 주사위는 유령으로 대체.
            for (int i = insertIndex; i <= insertIndex + shiftCount; i++)
                row.Cell(field, i).SetEmpty();

            var ghosts = new CellView[shiftCount];
            for (int k = 0; k < shiftCount; k++)
                ghosts[k] = SpawnFx(row.Cell(field, insertIndex + k).Rect.position,
                    shiftValues[k], shiftSpecial[k], shiftSides[k]);

            Vector3 start = _tray.DieWorldPosition;
            _tray.HideDie();
            var cur = SpawnFx(start, value, special, side);

            // 새 주사위 + 밀리는 주사위들을 같은 시간에 동시에 이동.
            var rects = new List<RectTransform>();
            var from = new List<Vector3>();
            var to = new List<Vector3>();
            rects.Add(cur.Rect); from.Add(start); to.Add(row.Cell(field, insertIndex).Rect.position);
            for (int k = 0; k < shiftCount; k++)
            {
                rects.Add(ghosts[k].Rect);
                from.Add(ghosts[k].Rect.position);
                to.Add(row.Cell(field, insertIndex + k + 1).Rect.position);
            }
            yield return MoveMany(rects, from, to, 0.3f, easeOut: true);

            // 실제 칸으로 확정
            row.Cell(field, insertIndex).SetDie(value, special, side);
            for (int k = 0; k < shiftCount; k++)
                row.Cell(field, insertIndex + k + 1).SetDie(shiftValues[k], shiftSpecial[k], shiftSides[k]);

            DestroyFx(cur);
            for (int k = 0; k < shiftCount; k++) DestroyFx(ghosts[k]);
        }

        /// <summary>
        /// 제거(알까기) 연출. 상대 라인 전체를 유령 주사위로 재현해 제어한다.
        /// 트레이에서 1초 부들부들 → 가속 돌진 → 끝이 맞닿는 순간 등속으로 양쪽 튕김 →
        /// 뒤 주사위가 있으면 빈자리로 부드럽게 당겨옴.
        /// </summary>
        public IEnumerator RemovalFxRoutine(
            PlayerId placerField, int line, int placingValue, bool placingSpecial, DiceSide placerSide,
            int placerCellIndex, bool placerSurvives,
            PlayerId oppField, int[] preValues, DiceSide[] preSides, bool[] preSpecial, bool[] preRemoved)
        {
            var row = _rows[line];
            int preCount = preValues.Length;

            Vector3 start = _tray.DieWorldPosition;
            _tray.HideDie();

            // 상대 라인의 실제 칸을 숨기고 유령 주사위로 대체(연출 제어).
            // 특수 여부도 그대로 반영(특수 주사위는 연출 내내 금색 유지).
            var ghosts = new CellView[preCount];
            for (int i = 0; i < preCount; i++)
            {
                row.Cell(oppField, i).SetEmpty();
                ghosts[i] = SpawnFx(row.Cell(oppField, i).Rect.position, preValues[i], preSpecial[i], preSides[i]);
            }

            var cur = SpawnFx(start, placingValue, placingSpecial, placerSide);

            // 1) 트레이에서 1초 부들부들
            yield return Shake(cur.Rect, 1.0f, 8f);

            // 첫 타겟 방향으로 가속 돌진, 끝이 맞닿는 지점에서 정지
            int targetIdx = 0;
            for (int i = 0; i < preCount; i++) if (preRemoved[i]) { targetIdx = i; break; }
            Vector3 targetPos = ghosts[targetIdx].Rect.position;
            Vector3 approach = targetPos - cur.Rect.position;
            approach = approach.sqrMagnitude < 0.01f ? Vector3.up : approach.normalized;
            Vector3 edgeStop = targetPos - approach * UiTheme.CellSize;

            // 2) 가속 이동
            yield return MoveAccel(cur.Rect, cur.Rect.position, edgeStop, 0.4f);

            // 3+4) 충돌 즉시 등속으로 튕김. 둘 다 앞쪽으로 날아가되 서로 반대 옆으로 벌어짐(Y자).
            const float knockDur = 0.45f;
            const float knockDist = 2600f;
            const float splay = 0.75f; // Y자 벌어짐 정도
            Vector3 perpDir = new Vector3(-approach.y, approach.x, 0f).normalized;

            var flyR = new List<RectTransform>();
            var flyA = new List<Vector3>();
            var flyB = new List<Vector3>();
            int rc = 0;
            for (int i = 0; i < preCount; i++)
            {
                if (!preRemoved[i]) continue;
                // 타겟들: 앞쪽 + 한쪽 옆(여러 개면 부채꼴로 더 벌어짐)
                Vector3 dir = (approach - perpDir * (splay + rc * 0.35f)).normalized;
                rc++;
                Vector3 gp = ghosts[i].Rect.position;
                flyR.Add(ghosts[i].Rect); flyA.Add(gp);
                flyB.Add(gp + dir * knockDist);
            }
            if (!placerSurvives)
            {
                // 현재 주사위: 앞쪽 + 반대쪽 옆 → 타겟과 Y자로 갈라짐
                Vector3 dir = (approach + perpDir * splay).normalized;
                flyR.Add(cur.Rect); flyA.Add(cur.Rect.position);
                flyB.Add(cur.Rect.position + dir * knockDist);
            }
            yield return MoveMany(flyR, flyA, flyB, knockDur, easeOut: false); // 등속

            for (int i = 0; i < preCount; i++) if (preRemoved[i]) DestroyFx(ghosts[i]);
            if (!placerSurvives) { DestroyFx(cur); cur = null; }

            // 5) 남은 주사위를 빈자리로 부드럽게 당겨옴(뒤 주사위 없어도 기본 텀)
            var slideR = new List<RectTransform>();
            var slideA = new List<Vector3>();
            var slideB = new List<Vector3>();
            int newIdx = 0;
            for (int i = 0; i < preCount; i++)
            {
                if (preRemoved[i]) continue;
                slideR.Add(ghosts[i].Rect);
                slideA.Add(ghosts[i].Rect.position);
                slideB.Add(row.Cell(oppField, newIdx).Rect.position);
                newIdx++;
            }
            if (slideR.Count > 0)
                yield return MoveMany(slideR, slideA, slideB, 0.3f, easeOut: true);
            else
                yield return new WaitForSeconds(0.3f);

            for (int i = 0; i < preCount; i++) if (!preRemoved[i]) DestroyFx(ghosts[i]);

            // 특수 생존 주사위는 본인 칸으로 이동해 배치
            if (placerSurvives && cur != null)
            {
                var ownCell = row.Cell(placerField, placerCellIndex);
                ownCell.SetEmpty();
                yield return MoveWorld(cur.Rect, cur.Rect.position, ownCell.Rect.position, 0.25f);
                ownCell.SetDie(placingValue, placingSpecial, placerSide);
                DestroyFx(cur);
            }
        }

        /// <summary>진행 중이던 이동 연출 잔여물 정리(재시작 시).</summary>
        public void ClearFx()
        {
            if (_fxLayer == null) return;
            for (int i = _fxLayer.childCount - 1; i >= 0; i--)
                Destroy(_fxLayer.GetChild(i).gameObject);
        }

        private CellView SpawnFx(Vector3 worldPos, int value, bool special, DiceSide side)
        {
            var fx = new CellView(_fxLayer);
            var rt = fx.Rect;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(UiTheme.CellSize, UiTheme.CellSize);
            rt.position = worldPos;
            fx.SetDie(value, special, side);
            return fx;
        }

        private static void DestroyFx(CellView fx)
        {
            if (fx != null) Destroy(fx.Rect.gameObject);
        }

        private static IEnumerator MoveWorld(RectTransform rt, Vector3 from, Vector3 to, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                k = 1f - (1f - k) * (1f - k); // ease-out
                rt.position = Vector3.Lerp(from, to, k);
                yield return null;
            }
            rt.position = to;
        }

        private static IEnumerator Shake(RectTransform rt, float dur, float amplitude)
        {
            Vector3 basePos = rt.position;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                rt.position = basePos + new Vector3(
                    UnityEngine.Random.Range(-amplitude, amplitude),
                    UnityEngine.Random.Range(-amplitude, amplitude), 0f);
                yield return null;
            }
            rt.position = basePos;
        }

        // 가속 이동(점점 빨라짐).
        private static IEnumerator MoveAccel(RectTransform rt, Vector3 from, Vector3 to, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                k = k * k; // ease-in
                rt.position = Vector3.Lerp(from, to, k);
                yield return null;
            }
            rt.position = to;
        }

        // 여러 개를 동시에 이동. easeOut=false면 등속(충돌 튕김), true면 감속(당겨오기).
        private static IEnumerator MoveMany(List<RectTransform> rects, List<Vector3> from, List<Vector3> to,
            float dur, bool easeOut)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                if (easeOut) k = 1f - (1f - k) * (1f - k);
                for (int i = 0; i < rects.Count; i++)
                    rects[i].position = Vector3.Lerp(from[i], to[i], k);
                yield return null;
            }
            for (int i = 0; i < rects.Count; i++)
                rects[i].position = to[i];
        }

        // ---- 결과 ----

        public void ShowResult(MatchOutcome outcome, string scoreLine)
        {
            string headline;
            if (outcome.IsDraw) headline = "무승부";
            else if (outcome.Winner == _humanId) headline = "승리!";
            else headline = "패배";

            string lines = "";
            for (int i = 0; i < outcome.Lines.Count; i++)
            {
                string r = outcome.Lines[i] == LineResult.Draw ? "무"
                    : (LineResultToPlayer(outcome.Lines[i]) == _humanId ? "승" : "패");
                lines += $"라인 {i + 1}: {r}\n";
            }

            _resultText.text = $"{headline}\n\n{lines}\n{scoreLine}";
            _resultOverlay.SetActive(true);
        }

        private static PlayerId LineResultToPlayer(LineResult r)
            => r == LineResult.PlayerOne ? PlayerId.One : PlayerId.Two;

        public void HideResult() => _resultOverlay.SetActive(false);
    }
}
