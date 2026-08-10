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
    /// 상단: 점수(Lv.난이도) / 중앙: 3줄(내 라인 [점수 ◀▶ 점수] 상대 라인) / 하단: 가로 주사위 트레이.
    /// 런타임에 코드로 UI를 구성한다.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        private PlayerId _humanId;
        private PlayerId _aiId;

        private readonly MatchRowView[] _rows = new MatchRowView[Field.LineCount];
        private TrayView _tray;
        private TMP_Text _headerText;

        private GameObject _root;
        private RectTransform _fxLayer;
        private GameObject _resultOverlay;
        private TMP_Text _resultText;
        private Button _continueButton;
        private Button _menuButton;
        private Button _protectButton;
        private TMP_Text _protectLabel;

        /// <summary>결과 문구에서 점수 줄 앞부분(머리말 + 라인 목록). 점수만 다시 쓸 때 쓴다.</summary>
        private string _resultHead = "";

        private IconButton _reroll;

        /// <summary>
        /// 친선대전 전용: 상대 진영의 리롤 상태 표시(읽기 전용). AI 대전에서는 AI가 리롤을
        /// 쓰지 않으므로 평소엔 숨겨 둔다. 실제 사용 여부는 GameController가 원격 상태를
        /// 받아 <see cref="SetOpponentRerollUsed"/>로 갱신한다.
        /// </summary>
        private IconButton _oppReroll;

        public event Action<PlayerId, int> LineClicked;

        /// <summary>
        /// 결과 화면의 "계속하기". 다음에 무엇을 할지(같은 난이도로 재시작 / 난이도 선택)는
        /// 해금 여부를 아는 쪽에서 정하므로, 보드는 눌렸다는 사실만 알린다.
        /// </summary>
        public event Action ContinueClicked;

        public event Action MenuClicked;

        /// <summary>
        /// 결과 화면의 부가 버튼(패배 시 점수 보호, 승리 시 코인 2배).
        /// 무엇을 뜻하는지·쓸 수 있는지·확인 창은 모두 밖에서 다룬다.
        /// </summary>
        public event Action ExtraClicked;

        /// <summary>리롤 버튼을 눌렀을 때.</summary>
        public event Action RerollClicked;

        /// <summary>리롤 선택 중 트레이 주사위를 클릭했을 때(0=기존, 1=리롤).</summary>
        public event Action<int> TrayDiceClicked;

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

        /// <summary>상단 중앙에 "점수 (Lv.난이도)" 한 줄만 표시한다.</summary>
        private void BuildTopBar(Transform parent)
        {
            var top = UiFactory.CreateRect("TopBar", parent);
            UiFactory.AddHorizontalLayout(top.gameObject, 0, new RectOffset(12, 12, 0, 0));
            UiFactory.SetPreferredHeight(top.gameObject, 96f);

            _headerText = UiFactory.CreateText("Header", top.transform, "",
                UiTheme.HeaderFontSize, UiTheme.Label, TextAnchor.MiddleCenter);
            UiFactory.SetFlexible(_headerText.gameObject);
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
            _tray.DiceClicked += i => TrayDiceClicked?.Invoke(i);

            BuildRerollButton(trayRow);
            BuildOpponentRerollIndicator(trayRow);
        }

        /// <summary>트레이 좌측의 정사각형 리롤 버튼. 레이아웃에서 빼고 좌측에 직접 배치한다.</summary>
        private void BuildRerollButton(RectTransform trayRow)
        {
            _reroll = UiFactory.CreateIconButton("RerollButton", trayRow, UiSkin.RerollIcon, "리롤",
                UiTheme.RerollButtonSize, UiTheme.RerollIconInset, 34);
            UiFactory.IgnoreLayout(_reroll.Button.gameObject);

            var rt = _reroll.Rect;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(UiTheme.RerollButtonX, 0f);

            _reroll.Button.onClick.AddListener(() => RerollClicked?.Invoke());
            SetRerollInteractable(false);
        }

        /// <summary>
        /// 트레이 우측, 내 리롤 버튼과 좌우 대칭 위치의 상대 진영 표시.
        /// 눌러도 아무 일도 일어나지 않는다 — 상대가 리롤을 썼는지 보여주는 용도일 뿐,
        /// 내가 조작할 수 있는 버튼이 아니다(기획서 2-1). 평소(AI 대전)엔 숨겨 둔다.
        /// </summary>
        private void BuildOpponentRerollIndicator(RectTransform trayRow)
        {
            _oppReroll = UiFactory.CreateIconButton("OpponentRerollStatus", trayRow, UiSkin.RerollIcon, "리롤",
                UiTheme.RerollButtonSize, UiTheme.RerollIconInset, 34);
            UiFactory.IgnoreLayout(_oppReroll.Button.gameObject);

            var rt = _oppReroll.Rect;
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-UiTheme.RerollButtonX, 0f);

            _oppReroll.Button.interactable = false; // 상태 표시 전용, 클릭 리스너 없음
            _oppReroll.Button.gameObject.SetActive(false); // AI 대전에서는 숨김(친선대전 시작 시 켠다)
        }

        /// <summary>리롤 버튼 활성/비활성. 비활성일 때는 아이콘/문구도 함께 흐려진다.</summary>
        public void SetRerollInteractable(bool on)
        {
            if (_reroll != null) _reroll.SetInteractable(on);
        }

        /// <summary>
        /// 친선대전 시작/종료 시 상대 리롤 상태 표시를 켜고 끈다.
        /// AI 대전에서는 절대 호출되지 않으며(항상 숨김), 게임이 끝나면 꺼야 다음 AI 대전에
        /// 잔상처럼 남지 않는다.
        /// </summary>
        public void SetOpponentRerollVisible(bool visible)
        {
            if (_oppReroll == null) return;
            _oppReroll.Button.gameObject.SetActive(visible);
            if (visible) _oppReroll.SetDimmed(false); // 판 시작 시엔 항상 "아직 안 씀" 상태
        }

        /// <summary>상대가 이번 판의 리롤을 썼는지 표시(썼으면 흐리게).</summary>
        public void SetOpponentRerollUsed(bool used)
        {
            if (_oppReroll != null) _oppReroll.SetDimmed(used);
        }

        /// <summary>리롤 후보를 굴려 기존 주사위 우측에 붙이는 연출(완료까지 대기).</summary>
        public IEnumerator RollCandidateRoutine(Dice candidate)
            => _tray.RollCandidateRoutine(candidate);

        /// <summary>
        /// 트레이 주사위가 굴러 제자리에 설 때까지 대기한다.
        /// 이동 도중에 리롤이나 배치를 받으면 주사위가 순간이동한다.
        /// </summary>
        public IEnumerator WaitForTrayIdle()
        {
            while (_tray.IsRolling) yield return null;
        }

        /// <summary>선택 확정 연출: 안 고른 주사위는 아래로 사라지고 고른 주사위가 자리를 잡는다.</summary>
        public IEnumerator ResolvePickRoutine(int picked) => _tray.ResolvePickRoutine(picked);

        /// <summary>리롤 선택 중 트레이 주사위 클릭 가능 여부.</summary>
        public void SetTrayPickable(bool on) => _tray.SetPickable(on);

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
            UiFactory.SetSize(buttonRow.gameObject, UiTheme.ResultButtonRowWidth, 150f);

            // 패배 보호권. 졌고, 코인이 충분하고, 오늘 아직 안 썼을 때만 나타난다.
            // 가로 레이아웃이 가운데로 모아 주므로 숨기면 나머지 둘이 알아서 가운데에 선다.
            _protectButton = UiFactory.CreateButton("ProtectButton", buttonRow.transform,
                UiTheme.ProtectButton);
            UiFactory.SetSize(_protectButton.gameObject,
                UiTheme.ResultButtonWidth, UiTheme.ResultButtonHeight);
            _protectLabel = UiFactory.CreateText("Label", _protectButton.transform,
                "", UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(_protectLabel.rectTransform);
            _protectButton.onClick.AddListener(() => ExtraClicked?.Invoke());
            _protectButton.gameObject.SetActive(false);

            _continueButton = UiFactory.CreateButton("ContinueButton", buttonRow.transform, UiTheme.Button);
            UiFactory.SetSize(_continueButton.gameObject, 400f, 130f);
            var continueText = UiFactory.CreateText("Label", _continueButton.transform, "계속하기",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(continueText.rectTransform);
            _continueButton.onClick.AddListener(() => ContinueClicked?.Invoke());

            _menuButton = UiFactory.CreateButton("MenuButton", buttonRow.transform, UiTheme.CenterPanel);
            UiFactory.SetSize(_menuButton.gameObject, 400f, 130f);
            var menuText = UiFactory.CreateText("Label", _menuButton.transform, "메뉴로",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(menuText.rectTransform);
            _menuButton.onClick.AddListener(() => MenuClicked?.Invoke());

            _resultOverlay.SetActive(false);
        }

        // ---- 렌더링 ----

        /// <summary>
        /// 이 판에서 "나"에 해당하는 PlayerId를 다시 지정한다. AI 대전은 항상 PlayerId.One(고정),
        /// 친선대전은 방장이면 One, 참가자면 Two다.
        ///
        /// BoardView는 부팅 시 딱 한 번만 만들어져 앱 세션 내내 재사용되므로(이 게임의
        /// 화면 구조), 판마다 "나"가 달라질 수 있는 친선대전에서는 <b>매 판 시작 시</b>
        /// GameController가 이 메서드를 호출해야 한다 — 안 그러면 참가자(Two)로 들어간
        /// 판에서도 자기 필드가 오른쪽에 그려진다(퍼스펙티브 미러링 깨짐).
        ///
        /// 물리적 좌/우 UI 배치는 생성 시점에 이미 고정돼 있어(왼쪽 칸 묶음/오른쪽 칸 묶음),
        /// 이 메서드는 GameObject를 다시 만들지 않고 "어느 PlayerId의 데이터를 좌/우에
        /// 그릴지"만 다시 지정한다 — 그래서 매 판 호출해도 비용이 거의 없다.
        /// </summary>
        public void SetPerspective(PlayerId me)
        {
            _humanId = me;
            _aiId = me.Other();
            for (int i = 0; i < _rows.Length; i++)
                _rows[i].SetPerspective(_humanId, _aiId);
        }

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

        /// <summary>상단 중앙 표시 갱신: 플레이어 점수와 이번 판의 AI 난이도.</summary>
        public void SetHeader(int score, int level) => _headerText.text = $"{score} (Lv.{level})";

        /// <summary>상단 중앙에 임의 문구를 표시한다(튜토리얼에서 점수 대신 쓴다).</summary>
        public void SetHeaderText(string text) => _headerText.text = text;

        /// <summary>지정한 줄의 배경 사각형. 튜토리얼이 강조를 씌울 자리를 잡는 데 쓴다.</summary>
        public RectTransform LineRect(PlayerId field, int line)
        {
            if (line < 0 || line >= _rows.Length) return null;
            var bg = _rows[line].Background(field);
            return bg != null ? bg.rectTransform : null;
        }

        /// <summary>리롤 버튼의 사각형(튜토리얼 강조용).</summary>
        public RectTransform RerollRect => _reroll != null ? _reroll.Rect : null;

        /// <summary>트레이 주사위의 사각형(0=기존, 1=리롤 후보). 튜토리얼 강조용.</summary>
        public RectTransform TrayRect(int index) => _tray.RectAt(index);

        // ---- 하이라이트 ----

        public void ClearHighlights()
        {
            for (int i = 0; i < _rows.Length; i++)
                _rows[i].ClearHighlights();
        }

        /// <param name="gate">
        /// 튜토리얼이 특정 줄만 열어 둘 때의 필터. null이면 규칙이 허락하는 곳 전부.
        /// 클릭 차단만으로는 부족하다 — 눌러도 아무 일이 없는 줄이 밝게 빛나면 고장으로 보인다.
        /// </param>
        public void HighlightPrimary(GameState state, Func<PlayerId, int, bool> gate = null)
        {
            ClearHighlights();
            int v = state.PendingDice != null ? state.PendingDice.Value : 0;
            for (int i = 0; i < _rows.Length; i++)
            {
                bool mySpace = state.Field(_humanId)[i].HasSpace;
                if (mySpace && Allowed(gate, _humanId, i)) _rows[i].SetSelectable(_humanId, true);

                // 제거 가능(내 라인 공간 + 상대 같은 라인에 제거 가능한 동일 값)하면 상대 라인도 강조.
                if (mySpace && state.Field(_aiId)[i].HasRemovableValue(v) && Allowed(gate, _aiId, i))
                    _rows[i].SetSelectable(_aiId, true);
            }
        }

        public void HighlightExtra(GameState state, Func<PlayerId, int, bool> gate = null)
        {
            ClearHighlights();
            for (int i = 0; i < _rows.Length; i++)
            {
                if (state.Field(_humanId)[i].HasSpace && Allowed(gate, _humanId, i))
                    _rows[i].SetSelectable(_humanId, true);
                if (state.Field(_aiId)[i].HasSpace && Allowed(gate, _aiId, i))
                    _rows[i].SetSelectable(_aiId, true);
            }
        }

        private static bool Allowed(Func<PlayerId, int, bool> gate, PlayerId field, int line)
            => gate == null || gate(field, line);

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
            AudioManager.PlayDiceRemoveStart();
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
            AudioManager.PlayDiceRemoveHit();
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

        /// <summary>진행 중인 연출(배치/제거/트레이/도장)을 모두 중단하고 FX를 정리한다.</summary>
        public void AbortAnimations()
        {
            StopAllCoroutines();
            // 굴림 코루틴을 밖에서 끊으면 소리와 "굴리는 중" 표시가 그대로 남는다.
            // HideDie가 둘 다 정리하므로 반드시 StopAllCoroutines 뒤에 부른다.
            _tray.HideDie();
            ClearFx();
            SetRerollInteractable(false);
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

            // 머리말과 라인 목록은 보호권을 써도 그대로다. 뒤의 점수 줄만 갈아 끼울 수
            // 있도록 따로 들고 있는다. 점수 줄 안에 해금 문구가 붙어 줄바꿈이 늘어날 수
            // 있어서, 완성된 문자열을 나중에 잘라 내려 하면 엉뚱한 곳이 잘린다.
            _resultHead = $"{headline}\n\n{lines}\n";
            _resultText.text = _resultHead + scoreLine;

            SetExtraOffer(false); // 지난 판의 버튼이 남아 있지 않게
            SetContinueVisible(true); // 지난 판이 친선대전이라 숨겨져 있었을 수 있으니 매번 되돌린다
            _resultOverlay.SetActive(true);
            AudioManager.PlayRoundEnd();
        }

        /// <summary>
        /// "계속하기" 버튼을 보이거나 감춘다. 친선대전은 재대결 흐름이 없어 "계속하기"와
        /// "메뉴로"가 똑같이 메뉴로 돌아가므로, 버튼 하나가 의미 없이 중복된다 — 이럴 땐
        /// 아예 감춘다. 감추면 가로 레이아웃이 "메뉴로"를 가운데로 모아 준다.
        /// </summary>
        public void SetContinueVisible(bool visible)
        {
            if (_continueButton != null) _continueButton.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 결과 화면이 떠 있는 상태에서 점수 줄만 다시 쓴다.
        /// 보호권을 쓴 직후 되돌아온 점수를 반영하는 용도라, 결과음을 다시 내지 않는다.
        /// </summary>
        public void UpdateResultScoreLine(string scoreLine)
        {
            if (_resultOverlay == null || !_resultOverlay.activeSelf) return;
            _resultText.text = _resultHead + scoreLine;
        }

        /// <summary>
        /// 결과 화면의 부가 버튼을 보이거나 감춘다.
        /// 패배면 "점수 보호권 사용", 승리면 "광고 보고 코인 2배"로 쓰인다.
        /// 감추면 가로 레이아웃이 남은 두 버튼을 다시 가운데로 모은다.
        ///
        /// 가격·횟수 같은 숫자는 버튼에 적지 않는다. 좁은 버튼에 문구와 숫자를 같이 넣으면
        /// 읽기 어렵다. 자세한 내용은 누른 뒤 뜨는 확인 창에서 알려준다.
        /// </summary>
        public void SetExtraOffer(bool visible, string label = null)
        {
            if (_protectButton == null) return;

            if (visible && !string.IsNullOrEmpty(label) && _protectLabel != null)
                _protectLabel.text = label;

            _protectButton.gameObject.SetActive(visible);
        }

        private static PlayerId LineResultToPlayer(LineResult r)
            => r == LineResult.PlayerOne ? PlayerId.One : PlayerId.Two;

        public void HideResult() => _resultOverlay.SetActive(false);
    }
}
