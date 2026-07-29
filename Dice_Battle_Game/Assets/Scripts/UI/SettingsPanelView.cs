using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiceBattle.UI
{
    /// <summary>
    /// 설정 창(모달). BGM/효과음 각각의 ON·OFF와 볼륨 슬라이더를 제공한다.
    /// 값은 <see cref="SoundSettings"/>(PlayerPrefs)에 바로 반영된다.
    /// 메인 화면과 게임 화면 어디서나 같은 인스턴스를 연다.
    /// </summary>
    public sealed class SettingsPanelView
    {
        private readonly GameObject _root;

        private readonly Button _bgmToggle;
        private readonly TMP_Text _bgmToggleLabel;
        private readonly Image _bgmToggleImage;
        private readonly Slider _bgmSlider;

        private readonly Button _sfxToggle;
        private readonly TMP_Text _sfxToggleLabel;
        private readonly Image _sfxToggleImage;
        private readonly Slider _sfxSlider;

        public bool IsOpen => _root.activeSelf;

        /// <summary>설정 창 안의 "게임 설명서" 버튼을 눌렀을 때(게임 중에도 규칙을 볼 수 있게).</summary>
        public event Action ManualRequested;

        public SettingsPanelView(RectTransform parent)
        {
            var backdrop = UiFactory.CreateStretchPanel("SettingsPanel", parent, UiTheme.Backdrop);
            UiFactory.IgnoreLayout(backdrop.gameObject);
            UiFactory.Stretch(backdrop.rectTransform);
            _root = backdrop.gameObject;

            var window = UiFactory.CreateWindow("Window", backdrop.transform,
                UiTheme.SettingsWindowWidth, UiTheme.SettingsWindowHeight);
            var layout = UiFactory.AddVerticalLayout(window.gameObject, 30, new RectOffset(60, 60, 40, 40));
            layout.childForceExpandHeight = false;

            var title = UiFactory.CreateText("Title", window, "설정",
                UiTheme.SettingsTitleFontSize, UiTheme.Label);
            UiFactory.SetPreferredHeight(title.gameObject, 90f);

            BuildRow(window, "배경음악", out _bgmToggle, out _bgmToggleLabel, out _bgmToggleImage, out _bgmSlider);
            BuildRow(window, "효과음", out _sfxToggle, out _sfxToggleLabel, out _sfxToggleImage, out _sfxSlider);

            _bgmToggle.onClick.AddListener(() => { SoundSettings.BgmOn = !SoundSettings.BgmOn; Refresh(); });
            _sfxToggle.onClick.AddListener(() => { SoundSettings.SfxOn = !SoundSettings.SfxOn; Refresh(); });
            _bgmSlider.onValueChanged.AddListener(v => SoundSettings.BgmVolume = v);
            _sfxSlider.onValueChanged.AddListener(v => SoundSettings.SfxVolume = v);

            // 빈 공간을 넣어 닫기 버튼을 창 아래쪽에 붙인다.
            var spacer = UiFactory.CreateRect("Spacer", window);
            UiFactory.SetFlexibleHeight(spacer.gameObject, 1f);

            // 아래 줄: [게임 설명서] [닫기]
            var buttonRow = UiFactory.CreateRect("ButtonRow", window);
            UiFactory.AddHorizontalLayout(buttonRow.gameObject, 40, new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(buttonRow.gameObject, 120f);

            var manualButton = UiFactory.CreateButton("ManualButton", buttonRow.transform, UiTheme.CenterPanel);
            UiFactory.SetSize(manualButton.gameObject, 380f, 110f);
            var manualLabel = UiFactory.CreateText("Label", manualButton.transform, "게임 설명서",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(manualLabel.rectTransform);
            manualButton.onClick.AddListener(() => ManualRequested?.Invoke());

            var closeButton = UiFactory.CreateButton("CloseButton", buttonRow.transform, UiTheme.Button);
            UiFactory.SetSize(closeButton.gameObject, 360f, 110f);
            var closeLabel = UiFactory.CreateText("Label", closeButton.transform, "닫기",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(closeLabel.rectTransform);
            closeButton.onClick.AddListener(Close);

            _root.SetActive(false);
        }

        /// <summary>"[이름] [ON/OFF] [────●────]" 한 줄.</summary>
        private static void BuildRow(RectTransform parent, string name,
            out Button toggle, out TMP_Text toggleLabel, out Image toggleImage, out Slider slider)
        {
            var row = UiFactory.CreateRect($"Row_{name}", parent);
            UiFactory.AddHorizontalLayout(row.gameObject, 30, new RectOffset(0, 0, 0, 0));
            UiFactory.SetPreferredHeight(row.gameObject, 100f);

            var label = UiFactory.CreateText("Label", row, name,
                UiTheme.SettingsLabelFontSize, UiTheme.Label, TextAnchor.MiddleLeft);
            UiFactory.SetPreferredWidth(label.gameObject, 250f);

            toggle = UiFactory.CreateButton($"Toggle_{name}", row, UiTheme.ToggleOn);
            toggleImage = toggle.GetComponent<Image>();
            UiFactory.SetSize(toggle.gameObject, 170f, 84f);
            toggleLabel = UiFactory.CreateText("Label", toggle.transform, "ON",
                UiTheme.StatusFontSize, Color.white);
            UiFactory.Stretch(toggleLabel.rectTransform);

            slider = UiFactory.CreateSlider($"Slider_{name}", row);
            UiFactory.SetFlexible(slider.gameObject);
        }

        public void Open()
        {
            Refresh();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        public void Close()
        {
            SoundSettings.Flush(); // 드래그 중 미뤄둔 볼륨 값을 저장
            _root.SetActive(false);
        }

        /// <summary>현재 설정값을 UI에 반영한다.</summary>
        private void Refresh()
        {
            ApplyToggle(_bgmToggleImage, _bgmToggleLabel, SoundSettings.BgmOn);
            ApplyToggle(_sfxToggleImage, _sfxToggleLabel, SoundSettings.SfxOn);

            // 코드에서 값을 넣을 때는 onValueChanged를 다시 태우지 않는다.
            _bgmSlider.SetValueWithoutNotify(SoundSettings.BgmVolume);
            _sfxSlider.SetValueWithoutNotify(SoundSettings.SfxVolume);
        }

        private static void ApplyToggle(Image image, TMP_Text label, bool on)
        {
            label.text = on ? "ON" : "OFF";
            label.color = on ? Color.white : UiTheme.LabelDim;
            // 스킨 버튼 스프라이트가 있으면 원본색을 살리기 위해 틴트만 살짝 준다.
            image.color = UiSkin.Button != null
                ? (on ? Color.white : new Color(1f, 1f, 1f, 0.45f))
                : (on ? UiTheme.ToggleOn : UiTheme.ToggleOff);
        }
    }
}
