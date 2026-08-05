using UnityEngine;
using UnityEngine.InputSystem;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 화면 흐름 관리: 메인 메뉴 ↔ 난이도 선택 ↔ 게임.
    /// 난이도는 해금한 것 중에서 플레이어가 직접 고르고, 매 판 결과로 점수·해금이 갱신된다.
    /// 설정 버튼(우측 상단)은 두 화면 공통이라 여기서 만들어 항상 띄워 둔다.
    /// 모바일 뒤로가기 버튼(=Escape)도 여기서 처리한다.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        private const PlayerId Human = PlayerId.One;

        /// <summary>
        /// 지금 보고 있는 화면. 뒤로가기 동작이 여기서 갈린다.
        /// (이름을 Screen으로 두면 UnityEngine.Screen을 가려 버린다.)
        /// </summary>
        private enum ScreenId { Menu, DifficultySelect, Game }

        private MenuView _menu;
        private DifficultySelectView _select;
        private BoardView _board;
        private GameController _controller;
        private ConfirmDialogView _dialog;
        private SettingsPanelView _settings;
        private ManualView _manual;
        private CreditsView _credits;
        private StatsView _stats;

        private ScreenId _screen = ScreenId.Menu;

        /// <summary>
        /// 직전 판의 정산 결과. "계속하기"가 난이도 선택으로 갈지 바로 재시작할지를 정한다.
        /// 기본값(전부 0)은 해금 없음으로 읽히므로 첫 판 전에도 안전하다.
        /// </summary>
        private ProgressUpdate _lastResult;

        /// <summary>해금 문구 색. 특수 주사위와 같은 금색이라 "얻었다"는 신호가 겹쳐 읽힌다.</summary>
        private const string UnlockColorHex = "#FFDB59";

        public void Init(RectTransform canvasRoot, DifficultyConfig difficulty)
        {
            // 점수·해금 판정에 쓸 난이도 표를 먼저 정한다.
            // 화면을 만들기 전에 해둬야 메뉴가 해금 상태를 제대로 표시한다.
            PlayerProgress.Configure(difficulty != null ? difficulty.CreateTable() : null);

            var boardGo = new GameObject("BoardView");
            boardGo.transform.SetParent(transform, false);
            _board = boardGo.AddComponent<BoardView>();
            _board.Build(canvasRoot, Human);

            var controllerGo = new GameObject("GameController");
            controllerGo.transform.SetParent(transform, false);
            _controller = controllerGo.AddComponent<GameController>();
            _controller.Init(_board, difficulty);
            _controller.MenuRequested += ShowMenu;
            _controller.ContinueRequested += OnContinue;
            _controller.MatchFinished += OnMatchFinished;
            _controller.AdRerollRequested += OnAdRerollRequested;

            // 메뉴는 보드 위에 오도록 나중에 생성.
            var menuGo = new GameObject("MenuView");
            menuGo.transform.SetParent(transform, false);
            _menu = menuGo.AddComponent<MenuView>();
            _menu.Build(canvasRoot);
            _menu.StartRequested += ShowDifficultySelect;
            _menu.StatsRequested += () => _stats.Open();
            _menu.ManualRequested += () => _manual.Open();

            var selectGo = new GameObject("DifficultySelectView");
            selectGo.transform.SetParent(transform, false);
            _select = selectGo.AddComponent<DifficultySelectView>();
            _select.Build(canvasRoot);
            _select.StartRequested += StartGame;
            _select.BackRequested += ShowMenu;

            // 설정 버튼은 메인 화면·게임 화면 위에 항상 떠 있어야 하므로 둘 다 만든 뒤에 생성한다.
            CreateSettingsButton(canvasRoot);
            _settings = new SettingsPanelView(canvasRoot);
            // 설명서·크레딧은 설정 창 위에 겹쳐 열린다(닫으면 설정 창으로 돌아옴).
            _manual = new ManualView(canvasRoot);
            _settings.ManualRequested += () => _manual.Open();
            _credits = new CreditsView(canvasRoot);
            _settings.CreditsRequested += () => _credits.Open();

            // 전적은 메인 메뉴에서만 열리므로 설정 창과 겹칠 일이 없다.
            _stats = new StatsView(canvasRoot);

            // 뒤로가기 다이얼로그는 항상 최상단에 오도록 마지막에 생성.
            _dialog = new ConfirmDialogView(canvasRoot);

            ShowMenu();
        }

        /// <summary>우측 상단 고정 위치의 설정 버튼(두 화면 공통).</summary>
        private void CreateSettingsButton(RectTransform canvasRoot)
        {
            var button = UiFactory.CreateIconButton("SettingsButton", canvasRoot,
                UiSkin.SettingsIcon, "설정", UiTheme.IconButtonSize, UiTheme.IconButtonInset, 30);

            var rt = button.Rect;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-UiTheme.IconButtonMarginX, -UiTheme.IconButtonMarginY);

            button.Button.onClick.AddListener(() => _settings.Open());
        }

        private void ShowMenu()
        {
            _controller.AbortMatch(); // 진행 중이던 판/연출 중단
            _screen = ScreenId.Menu;
            _board.SetVisible(false);
            _select.SetVisible(false);
            _menu.SetScore(PlayerProgress.Score, PlayerProgress.MaxUnlockedLevel);
            _menu.SetVisible(true);
        }

        /// <summary>메인 메뉴에서 들어온 경우. 고를 수 있는 가장 높은 난이도가 잡혀 있다.</summary>
        private void ShowDifficultySelect()
            => OpenDifficultySelect(PlayerProgress.MaxUnlockedLevel, newFrom: 0, newTo: 0);

        /// <summary>
        /// 해금 직후. 새로 열린 난이도를 강조하고 <b>기본 선택까지 그쪽으로 옮긴다</b> —
        /// 방금 얻은 것을 바로 해보는 게 자연스럽고, 어느 카드가 새 것인지도 분명해진다.
        /// </summary>
        private void ShowDifficultySelectAfterUnlock(ProgressUpdate update)
            => OpenDifficultySelect(update.UnlockedAfter,
                newFrom: update.UnlockedBefore + 1, newTo: update.UnlockedAfter);

        /// <summary>해금 상태는 매 판 바뀌므로 열 때마다 다시 그린다.</summary>
        private void OpenDifficultySelect(int selectedLevel, int newFrom, int newTo)
        {
            _controller.AbortMatch();
            _screen = ScreenId.DifficultySelect;
            _board.SetVisible(false);
            _menu.SetVisible(false);
            _select.Open(PlayerProgress.Difficulties, PlayerProgress.Score,
                PlayerProgress.HighestScore, selectedLevel, newFrom, newTo);
        }

        /// <summary>
        /// 결과 화면의 "계속하기".
        /// 새 난이도가 열렸을 때만 고를 기회를 준다. 아무것도 안 열렸는데 매번
        /// 선택 화면을 거치게 하면 연달아 할 때 탭만 늘어난다.
        /// </summary>
        private void OnContinue()
        {
            if (_lastResult.HasNewUnlock)
            {
                ShowDifficultySelectAfterUnlock(_lastResult);
                return;
            }
            StartGame(_controller.Level);
        }

        private void StartGame(int level)
        {
            _screen = ScreenId.Game;
            _menu.SetVisible(false);
            _select.SetVisible(false);
            _board.SetVisible(true);
            _controller.StartMatch(level);
        }

        // ---- 모바일 뒤로가기 ----

        private void Update()
        {
            if (BackPressed()) OnBackPressed();
        }

        /// <summary>안드로이드 뒤로가기 버튼은 Escape 키로 전달된다.</summary>
        private static bool BackPressed()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        }

        private void OnBackPressed()
        {
            // 강제 업데이트 창이 떠 있으면 게임으로 돌아갈 길이 없다.
            // 이 상태에서 다이얼로그를 열면 차단 창 위로 올라와 버리므로, 뒤로가기는 종료로만 동작한다.
            if (VersionGate.IsBlocking)
            {
                AppExit.Quit();
                return;
            }

            // 이미 열려 있으면 뒤로가기는 '닫기'로 동작(위에 떠 있는 것부터).
            if (_dialog.IsOpen)
            {
                _dialog.Close();
                return;
            }
            if (_manual.IsOpen)
            {
                _manual.Close();
                return;
            }
            if (_credits.IsOpen)
            {
                _credits.Close();
                return;
            }
            if (_stats.IsOpen)
            {
                _stats.Close();
                return;
            }
            if (_settings.IsOpen)
            {
                _settings.Close();
                return;
            }

            if (_screen == ScreenId.Menu)
            {
                _dialog.Open("게임을 종료할까요?", "뒤로가기", "게임 종료", AppExit.Quit);
                return;
            }

            // 난이도 선택은 아직 아무것도 시작하지 않은 화면이라 확인 없이 되돌린다.
            // 결과 화면을 거쳐 왔더라도 그 판은 이미 정산이 끝났으므로 메인으로 보낸다.
            if (_screen == ScreenId.DifficultySelect)
            {
                ShowMenu();
                return;
            }

            // 승부가 끝난 뒤(결과 화면)에는 "사라집니다" 경고가 맞지 않으므로 문구를 나눈다.
            string message = _controller.IsMatchActive
                ? "메인 메뉴로 돌아갈까요?\n진행 중인 판은 사라집니다."
                : "메인 메뉴로 돌아갈까요?";
            _dialog.Open(message, "뒤로가기", "메인 메뉴로", ShowMenu);
        }

        // ---- 광고 리롤 ----

        /// <summary>
        /// 기본 리롤을 소진한 뒤 리롤 버튼을 눌렀을 때.
        /// 버튼을 누르자마자 광고가 뜨면 당황스러우므로 확인을 먼저 받는다.
        /// </summary>
        private void OnAdRerollRequested()
        {
            _dialog.Open("리롤을 모두 사용했습니다.\n광고를 보고 한 번 더 굴릴까요?",
                "취소", "광고 보기",
                () => AdManager.ShowRewarded(_controller.GrantAdReroll, OnAdRerollUnavailable));
        }

        /// <summary>
        /// 광고가 뜨지 않았거나 도중에 닫은 경우.
        /// 보상은 주지 않되 왜 안 됐는지는 알려준다(아무 반응이 없으면 고장으로 보인다).
        /// </summary>
        private void OnAdRerollUnavailable()
        {
            _dialog.Open("광고를 불러오지 못했습니다.\n잠시 후 다시 시도해 주세요.",
                "닫기", "확인", null);
        }

        private void OnMatchFinished(MatchOutcome outcome)
        {
            PlayerMatchResult result = RankSystem.ResultFor(outcome, Human);

            // 정산은 반드시 "그 판을 시작한 난이도"로 한다. 지금 해금된 난이도로 하면
            // 낮은 난이도를 골라 놓고 높은 난이도의 점수를 받게 된다.
            ProgressUpdate update = PlayerProgress.ApplyResult(result, _controller.Level);
            _lastResult = update; // "계속하기"가 어디로 갈지 이 값으로 갈린다

            // 전적도 같은 난이도 기준으로 누적한다. 점수 정산과 어긋나면 안 된다.
            PlayerStats.ApplyMatch(result, _controller.Level, _controller.HumanRemovedThisMatch);

            string sign = update.Delta > 0 ? "+" : "";
            string scoreLine =
                $"Lv.{_controller.Level}   점수 {sign}{update.Delta}  →  {update.Score}";

            // 결과 문구는 이미 9줄 가까이 되어 아래 버튼과 여유가 없다.
            // 색으로 충분히 구분되므로 빈 줄을 넣지 않는다.
            if (update.HasNewUnlock)
                scoreLine += $"\n<color={UnlockColorHex}>{UnlockedRange(update)} 해금!</color>";

            _board.ShowResult(outcome, scoreLine);
        }

        /// <summary>
        /// 한 판에 두 단계가 동시에 열릴 수도 있으므로 구간으로 적는다.
        /// 지금 수치로는 일어나지 않지만, 밸런스를 조정하다 승점이 커지면 생길 수 있다.
        /// </summary>
        private static string UnlockedRange(ProgressUpdate update)
        {
            int from = update.UnlockedBefore + 1;
            int to = update.UnlockedAfter;
            return from == to ? $"Lv.{to}" : $"Lv.{from}~{to}";
        }
    }
}
