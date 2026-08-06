using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 출석 보상 창(모달). 7일 순환이며 하루에 한 번 받는다.
    ///
    /// 메인 메뉴에 들어올 때 받을 것이 있으면 저절로 열린다. 하루 한 번뿐이라
    /// 성가시지 않고, 버튼을 따로 만들면 존재를 모르는 사람이 생긴다.
    ///
    /// <b>며칠 빠져도 순환 위치는 되돌아가지 않는다.</b> 돌아온 사람을 벌할 이유가 없다.
    /// </summary>
    public sealed class AttendanceView
    {
        private readonly GameObject _root;
        private readonly TMP_Text _body;
        private readonly Cell[] _cells;

        /// <summary>보상을 받아 잔액이 바뀌었을 때. 메뉴가 코인 표시를 갱신한다.</summary>
        public event Action Claimed;

        public bool IsOpen => _root.activeSelf;

        public AttendanceView(RectTransform parent)
        {
            var backdrop = UiFactory.CreateStretchPanel("AttendancePanel", parent, UiTheme.Backdrop);
            UiFactory.IgnoreLayout(backdrop.gameObject);
            UiFactory.Stretch(backdrop.rectTransform);
            _root = backdrop.gameObject;

            var window = UiFactory.CreateWindow("Window", backdrop.transform,
                UiTheme.AttendanceWindowWidth, UiTheme.AttendanceWindowHeight);
            var layout = UiFactory.AddVerticalLayout(window.gameObject, 18,
                new RectOffset(60, 60, 30, 30));
            layout.childForceExpandHeight = false;

            var title = UiFactory.CreateText("Title", window, "출석 보상",
                UiTheme.AttendanceTitleFontSize, UiTheme.Label);
            UiFactory.SetPreferredHeight(title.gameObject, 80f);

            _body = UiFactory.CreateText("Body", window, "",
                UiTheme.AttendanceBodyFontSize, UiTheme.LabelDim);
            UiFactory.SetPreferredHeight(_body.gameObject, 56f);

            var row = UiFactory.CreateRect("Cells", window);
            UiFactory.AddHorizontalLayout(row.gameObject, UiTheme.AttendanceCellGap,
                new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(row.gameObject, UiTheme.AttendanceCellHeight);

            _cells = new Cell[CoinRules.AttendanceCycleLength];
            for (int i = 0; i < _cells.Length; i++)
                _cells[i] = new Cell(row, i);

            var buttonRow = UiFactory.CreateRect("ButtonRow", window);
            UiFactory.AddHorizontalLayout(buttonRow.gameObject, 0, new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(buttonRow.gameObject, 120f);

            var claimButton = UiFactory.CreateButton("ClaimButton", buttonRow.transform, UiTheme.Button);
            UiFactory.SetSize(claimButton.gameObject, 360f, 110f);
            var claimLabel = UiFactory.CreateText("Label", claimButton.transform, "받기",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(claimLabel.rectTransform);
            claimButton.onClick.AddListener(OnClaim);

            _root.SetActive(false);
        }

        /// <summary>받을 것이 있을 때만 연다. 없으면 아무 일도 하지 않는다.</summary>
        public void OpenIfAvailable()
        {
            if (!PlayerWallet.CanClaimAttendance) return;
            Open();
        }

        public void Open()
        {
            Refresh();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        public void Close() => _root.SetActive(false);

        private void OnClaim()
        {
            int reward = PlayerWallet.ClaimAttendance();

            // 받을 것이 없었으면(이미 받은 날) 조용히 닫는다. 버튼 연타 대비.
            if (reward > 0) Claimed?.Invoke();
            Close();
        }

        private void Refresh()
        {
            int next = PlayerWallet.AttendanceIndex;

            _body.text = "매주 월요일 초기화   ·   보유 코인 " +
                         $"<color={UiTheme.CoinColorHex}>{PlayerWallet.Coins:N0}</color>";

            // 순환 위치 앞쪽은 이번 바퀴에서 이미 받은 칸이다.
            // 마지막 칸을 받으면 위치가 0으로 돌아가면서 전부 다시 밝아진다.
            for (int i = 0; i < _cells.Length; i++)
                _cells[i].Refresh(isNext: i == next, claimed: i < next);
        }

        /// <summary>순환 한 칸. "N일차 / 보상" 두 줄.</summary>
        private sealed class Cell
        {
            private readonly Image _background;
            private readonly Image _border;

            public Cell(RectTransform parent, int index)
            {
                var background = UiFactory.CreatePanel($"Day{index + 1}", parent, UiTheme.LineNormal);
                _background = background;
                UiFactory.SetSize(background.gameObject,
                    UiTheme.AttendanceCellWidth, UiTheme.AttendanceCellHeight);

                // 각진 사각형이면 둥근 초록 테두리가 모서리에서 삐져나온다.
                // 배경도 같은 곡률로 둥글게 깎는다.
                //
                // 스킨 스프라이트는 유니티 내장 UISprite를 그대로 뜬 것이다
                // (DiceBattle → 내장 UI 스프라이트 추출). 내장 리소스를 코드로 직접
                // 참조하면 에디터에서만 되고 빌드에서 null이 되므로 반드시 에셋으로 둔다.
                background.sprite = UiSkin.RoundedPanel != null
                    ? UiSkin.RoundedPanel
                    : UiSprites.RoundedRect;
                background.type = Image.Type.Sliced;
                background.pixelsPerUnitMultiplier = UiTheme.AttendanceCellRoundness;

                _border = CreateHighlight(background.transform);

                var day = UiFactory.CreateText("Day", background.transform,
                    $"{index + 1}일차", UiTheme.AttendanceDayFontSize, UiTheme.LabelDim);
                var dayRect = day.rectTransform;
                dayRect.anchorMin = new Vector2(0f, 0.62f);
                dayRect.anchorMax = new Vector2(1f, 0.92f);
                dayRect.offsetMin = Vector2.zero;
                dayRect.offsetMax = Vector2.zero;

                var reward = UiFactory.CreateText("Reward", background.transform,
                    $"{CoinRules.AttendanceReward(index)}", UiTheme.AttendanceRewardFontSize,
                    UiTheme.Coin);
                var rewardRect = reward.rectTransform;
                rewardRect.anchorMin = new Vector2(0f, 0.14f);
                rewardRect.anchorMax = new Vector2(1f, 0.58f);
                rewardRect.offsetMin = Vector2.zero;
                rewardRect.offsetMax = Vector2.zero;
            }

            public void Refresh(bool isNext, bool claimed)
            {
                _border.enabled = isNext;
                // 이미 받은 칸은 더 죽여서 "여기까지 왔다"가 한눈에 보이게 한다.
                _background.color = claimed ? UiTheme.AttendanceCellClaimed : UiTheme.LineNormal;
            }

            /// <summary>
            /// 오늘 받을 칸을 감싸는 초록 테두리.
            /// 라인 박스·리롤 트레이·난이도 카드와 <b>같은 스프라이트에 같은 초록</b>이라
            /// "여기"라는 신호가 게임 안 어디서든 같은 의미로 읽힌다.
            /// </summary>
            private static Image CreateHighlight(Transform cell)
            {
                var img = UiFactory.CreatePanel("Highlight", cell, UiTheme.LineHighlightSolid);
                img.raycastTarget = false;
                UiFactory.IgnoreLayout(img.gameObject);
                UiFactory.Stretch(img.rectTransform);

                if (UiSkin.LineNormal != null)
                {
                    img.sprite = UiSkin.LineNormal;
                    img.type = Image.Type.Sliced;
                    img.color = UiTheme.LineHighlightSolid;
                }
                else
                {
                    // 테두리 이미지가 없을 때의 폴백. 반투명 초록이 칸 전체에 덮인다.
                    img.sprite = UiSprites.RoundedRect;
                    img.type = Image.Type.Sliced;
                    img.color = UiTheme.LineHighlight;
                }

                img.enabled = false;
                return img;
            }
        }
    }
}
