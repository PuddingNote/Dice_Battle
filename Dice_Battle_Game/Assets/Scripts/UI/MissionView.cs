using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 일일 미션 창(모달). 메인 메뉴의 "일일 미션" 버튼으로 연다.
    ///
    /// 보상은 <b>최대 해금 난이도에 비례</b>하므로, 난이도가 오르면 같은 미션의 보상도
    /// 같이 오른다. 열 때마다 다시 읽어 그린다.
    /// </summary>
    public sealed class MissionView
    {
        private readonly GameObject _root;
        private readonly TMP_Text _body;
        private readonly Row[] _rows;

        /// <summary>보상을 받아 잔액이 바뀌었을 때. 메뉴가 코인 표시를 갱신한다.</summary>
        public event Action Claimed;

        public bool IsOpen => _root.activeSelf;

        public MissionView(RectTransform parent)
        {
            var backdrop = UiFactory.CreateStretchPanel("MissionPanel", parent, UiTheme.Backdrop);
            UiFactory.IgnoreLayout(backdrop.gameObject);
            UiFactory.Stretch(backdrop.rectTransform);
            _root = backdrop.gameObject;

            var window = UiFactory.CreateWindow("Window", backdrop.transform,
                UiTheme.MissionWindowWidth, UiTheme.MissionWindowHeight);
            var layout = UiFactory.AddVerticalLayout(window.gameObject, UiTheme.MissionRowSpacing,
                new RectOffset(70, 70, 30, 30));
            layout.childForceExpandHeight = false;

            var title = UiFactory.CreateText("Title", window, "일일 미션",
                UiTheme.MissionTitleFontSize, UiTheme.Label);
            UiFactory.SetPreferredHeight(title.gameObject, 80f);

            _body = UiFactory.CreateText("Body", window, "",
                UiTheme.MissionBodyFontSize, UiTheme.LabelDim);
            UiFactory.SetPreferredHeight(_body.gameObject, 52f);

            _rows = new Row[MissionRules.DailyCount];
            for (int i = 0; i < _rows.Length; i++)
                _rows[i] = new Row(window, i, OnClaim);

            var spacer = UiFactory.CreateRect("Spacer", window);
            UiFactory.SetFlexibleHeight(spacer.gameObject, 1f);

            var buttonRow = UiFactory.CreateRect("ButtonRow", window);
            UiFactory.AddHorizontalLayout(buttonRow.gameObject, 0, new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(buttonRow.gameObject, 100f);

            var closeButton = UiFactory.CreateButton("CloseButton", buttonRow.transform, UiTheme.Button);
            UiFactory.SetSize(closeButton.gameObject, 280f, 96f);
            var closeLabel = UiFactory.CreateText("Label", closeButton.transform, "닫기",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(closeLabel.rectTransform);
            closeButton.onClick.AddListener(Close);

            _root.SetActive(false);
        }

        public void Open()
        {
            Refresh();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        public void Close() => _root.SetActive(false);

        private void OnClaim(int index)
        {
            int reward = PlayerMissions.Claim(index);
            if (reward <= 0) return;

            Refresh();
            Claimed?.Invoke();
        }

        private void Refresh()
        {
            _body.text = "매일 0시 초기화   ·   보유 코인 " +
                         $"<color={UiTheme.CoinColorHex}>{PlayerWallet.Coins:N0}</color>";

            for (int i = 0; i < _rows.Length; i++) _rows[i].Refresh(i);
        }

        /// <summary>미션 한 줄. "설명 / 진행도" 왼쪽, 보상 버튼 오른쪽.</summary>
        private sealed class Row
        {
            private readonly TMP_Text _label;
            private readonly Button _button;
            private readonly TMP_Text _buttonLabel;

            public Row(RectTransform parent, int index, Action<int> onClaim)
            {
                var row = UiFactory.CreateRect($"Mission{index}", parent);
                UiFactory.AddHorizontalLayout(row.gameObject, 20, new RectOffset(0, 0, 0, 0));
                UiFactory.SetPreferredHeight(row.gameObject, UiTheme.MissionRowHeight);

                _label = UiFactory.CreateText("Label", row, "",
                    UiTheme.MissionLabelFontSize, UiTheme.Label, TextAnchor.MiddleLeft);
                UiFactory.SetFlexible(_label.gameObject);

                _button = UiFactory.CreateButton("Claim", row, UiTheme.Button);
                UiFactory.SetSize(_button.gameObject,
                    UiTheme.MissionButtonWidth, UiTheme.MissionButtonHeight);
                _buttonLabel = UiFactory.CreateText("Label", _button.transform, "",
                    UiTheme.MissionButtonFontSize, Color.white);
                UiFactory.Stretch(_buttonLabel.rectTransform);
                _button.onClick.AddListener(() => onClaim(index));

                // 색은 코드로 직접 다루므로 유니티 기본 색조 전환을 끈다.
                // 켜 두면 interactable=false일 때 지정한 색이 덮어써진다.
                _button.transition = Selectable.Transition.None;
            }

            public void Refresh(int slot)
            {
                int progress = PlayerMissions.Progress(slot);
                int target = PlayerMissions.Target(slot);
                int reward = PlayerMissions.Reward(slot);

                _label.text = $"{Describe(PlayerMissions.MissionAt(slot))}\n" +
                              $"<size={UiTheme.MissionProgressFontSize}>" +
                              $"<color=#A6ABB5>{progress} / {target}</color></size>";

                bool claimed = PlayerMissions.IsClaimed(slot);
                bool canClaim = PlayerMissions.CanClaim(slot);

                _button.interactable = canClaim;
                _buttonLabel.text = claimed ? "완료" : $"{reward}";

                var image = _button.targetGraphic as Image;
                if (image != null)
                {
                    image.color = canClaim ? UiTheme.Button
                        : claimed ? UiTheme.ToggleOff : UiTheme.CenterPanel;
                }

                _buttonLabel.color = canClaim ? Color.white
                    : claimed ? UiTheme.LabelDim : UiTheme.Coin;
            }

            /// <summary>미션 문구. 목표 수치는 Core가 들고 있고 표현만 여기서 만든다.</summary>
            private static string Describe(MissionRules.Mission mission)
            {
                switch (mission.Kind)
                {
                    case MissionKind.PlayMatches:
                        return $"{mission.Target}판 플레이하기";
                    case MissionKind.RemoveDice:
                        return $"상대 주사위 {mission.Target}개 제거하기";
                    case MissionKind.WinLines:
                        return $"라인 {mission.Target}개 이기기";
                    case MissionKind.UseReroll:
                        return $"리롤 {mission.Target}회 사용하기";
                    case MissionKind.PlaceExtra:
                        return $"특수 주사위 {mission.Target}개 배치하기";
                    case MissionKind.PlaceOnOpponent:
                        return $"상대 필드에 {mission.Target}개 배치하기";
                    default:
                        return $"한 판에서 주사위 {MissionRules.BigRemovalThreshold}개 이상 제거하기";
                }
            }
        }
    }
}
