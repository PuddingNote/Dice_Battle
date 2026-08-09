using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 튜토리얼 안내 화면: 떠 있는 문구 패널 + 눌러야 할 곳을 감싸는 테두리 + 건너뛰기 버튼.
    ///
    /// 두 가지 상태가 있고, 차이는 <b>탭을 삼키느냐</b>다.
    ///   읽기(Read)   전체 화면이 탭을 받는다. 보드를 눌러도 아무 일이 없고, 어디를 눌러도 다음으로 간다.
    ///   행동(Action) 패널은 문구만 보여주고 탭은 그대로 보드로 통과시킨다.
    /// 이걸 구분하지 않으면 설명을 읽는 도중에 보드가 눌리거나, 반대로 하라는 대로 눌렀는데
    /// 안내 패널이 먹어 버리는 일이 생긴다.
    /// </summary>
    public sealed class TutorialGuideView
    {
        /// <summary>테두리를 이루는 네 변.</summary>
        private const int RingSideCount = 4;

        private readonly RectTransform _root;
        private readonly Image _tapCatcher;
        private readonly RectTransform _panel;
        private readonly TMP_Text _text;
        private readonly TMP_Text _hint;
        private readonly RectTransform _ring;
        private readonly Image[] _ringSides = new Image[RingSideCount];
        private readonly Button _skipButton;

        private RectTransform _ringTarget;
        private float _pulseTime;

        /// <summary>매 프레임 대상 위치를 다시 재므로, 그때마다 배열을 새로 만들지 않는다.</summary>
        private readonly Vector3[] _corners = new Vector3[4];

        /// <summary>읽기 단계에서 화면을 탭했을 때.</summary>
        public event Action Tapped;

        /// <summary>건너뛰기 버튼을 눌렀을 때. 확인 창은 밖에서 띄운다.</summary>
        public event Action SkipRequested;

        public TutorialGuideView(RectTransform parent)
        {
            var root = UiFactory.CreateStretchPanel("TutorialGuide", parent, new Color(0, 0, 0, 0));
            UiFactory.IgnoreLayout(root.gameObject);
            UiFactory.Stretch(root.rectTransform);
            root.raycastTarget = false;
            _root = root.rectTransform;

            // 읽기 단계에서만 켜지는 투명 탭 수집판. 패널보다 먼저 만들어 뒤에 깔린다.
            _tapCatcher = UiFactory.CreatePanel("TapCatcher", _root, new Color(0, 0, 0, 0.35f));
            UiFactory.Stretch(_tapCatcher.rectTransform);
            var catcherButton = _tapCatcher.gameObject.AddComponent<Button>();
            catcherButton.transition = Selectable.Transition.None;
            catcherButton.onClick.AddListener(() => Tapped?.Invoke());
            _tapCatcher.gameObject.SetActive(false);

            _ring = BuildRing(_root);
            BuildRingSides();
            _panel = BuildPanel(_root, out _text, out _hint);
            _skipButton = BuildSkipButton(_root);

            _root.gameObject.SetActive(false);
        }

        /// <summary>
        /// 둥근 모서리를 입히되 색은 유지한다.
        /// <see cref="UiFactory.ApplyRounded"/>는 스킨 스프라이트가 있으면 원본색을 쓰려고
        /// 색을 흰색으로 덮는데, 여기서는 테두리와 본체를 색으로 구분해야 한다.
        /// </summary>
        private static void RoundedTinted(Image img, Color color)
        {
            img.sprite = UiSkin.RoundedPanel != null ? UiSkin.RoundedPanel : UiSprites.RoundedRect;
            img.type = Image.Type.Sliced;
            img.color = color;
        }

        private RectTransform BuildPanel(RectTransform parent, out TMP_Text text, out TMP_Text hint)
        {
            var edge = UiFactory.CreatePanel("Panel", parent, UiTheme.TutorialPanelEdge);
            RoundedTinted(edge, UiTheme.TutorialPanelEdge);
            // 패널은 어떤 상태에서도 탭을 가로채지 않는다. 읽기 단계에서는 뒤에 깔린
            // 수집판이 받고, 행동 단계에서는 그대로 보드까지 내려가야 한다.
            edge.raycastTarget = false;
            var rt = edge.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(UiTheme.TutorialPanelWidth, UiTheme.TutorialPanelHeight);

            var body = UiFactory.CreatePanel("Body", rt, UiTheme.TutorialPanel);
            RoundedTinted(body, UiTheme.TutorialPanel);
            UiFactory.Stretch(body.rectTransform);
            body.rectTransform.offsetMin = new Vector2(6f, 6f);
            body.rectTransform.offsetMax = new Vector2(-6f, -6f);
            body.raycastTarget = false;

            text = UiFactory.CreateText("Text", body.transform, "",
                UiTheme.TutorialTextFontSize, UiTheme.Label);
            UiFactory.SetWrap(text, true);
            UiFactory.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(40f, 44f);
            text.rectTransform.offsetMax = new Vector2(-40f, -20f);

            hint = UiFactory.CreateText("Hint", body.transform, "화면을 탭하면 계속",
                UiTheme.TutorialHintFontSize, UiTheme.LabelDim);
            var hrt = hint.rectTransform;
            hrt.anchorMin = new Vector2(0.5f, 0f);
            hrt.anchorMax = new Vector2(0.5f, 0f);
            hrt.pivot = new Vector2(0.5f, 0f);
            hrt.sizeDelta = new Vector2(UiTheme.TutorialPanelWidth, 40f);
            hrt.anchoredPosition = new Vector2(0f, 16f);

            return rt;
        }

        /// <summary>
        /// 네 변으로 이루어진 빈 테두리. 안을 채우지 않으므로 주사위 눈이 그대로 보인다.
        /// </summary>
        private static RectTransform BuildRing(RectTransform parent)
        {
            var ring = UiFactory.CreateRect("Ring", parent);
            ring.anchorMin = new Vector2(0.5f, 0.5f);
            ring.anchorMax = new Vector2(0.5f, 0.5f);
            ring.pivot = new Vector2(0.5f, 0.5f);
            ring.gameObject.SetActive(false);
            return ring;
        }

        /// <summary>테두리 네 변을 만든다(생성자에서 <see cref="_ring"/>이 준비된 뒤 호출).</summary>
        private void BuildRingSides()
        {
            float t = UiTheme.TutorialRingThickness;
            for (int i = 0; i < RingSideCount; i++)
            {
                var side = UiFactory.CreatePanel($"Side{i}", _ring, UiTheme.TutorialRing);
                side.raycastTarget = false;
                _ringSides[i] = side;
            }

            // 위/아래는 가로로 꽉, 좌/우는 세로로 꽉.
            Band(_ringSides[0].rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), t);
            Band(_ringSides[1].rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), t);
            Column(_ringSides[2].rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), t);
            Column(_ringSides[3].rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), t);
        }

        private static void Band(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, float thickness)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.sizeDelta = new Vector2(0f, thickness);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void Column(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, float thickness)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.sizeDelta = new Vector2(thickness, 0f);
            rt.anchoredPosition = Vector2.zero;
        }

        private Button BuildSkipButton(RectTransform parent)
        {
            var button = UiFactory.CreateButton("SkipButton", parent, UiTheme.TutorialSkipButton);
            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(UiTheme.TutorialSkipButtonWidth, UiTheme.TutorialSkipButtonHeight);
            rt.anchoredPosition = new Vector2(-UiTheme.IconButtonMarginX, -UiTheme.IconButtonMarginY);

            var label = UiFactory.CreateText("Label", rt, "건너뛰기",
                UiTheme.TutorialSkipFontSize, Color.white);
            UiFactory.Stretch(label.rectTransform);

            button.onClick.AddListener(() => SkipRequested?.Invoke());
            return button;
        }

        public void SetVisible(bool visible)
        {
            _root.gameObject.SetActive(visible);
            if (visible)
            {
                _root.SetAsLastSibling();
                return;
            }

            HidePanel();
            HideRing();
        }

        /// <summary>
        /// 문구를 띄운다.
        /// </summary>
        /// <param name="blocking">
        /// true면 화면 전체가 탭을 삼킨다(읽기 단계). false면 탭이 보드로 통과한다(행동 단계).
        /// </param>
        public void Show(string message, TutorialAnchor anchor, bool blocking)
        {
            _text.text = message;
            _panel.anchoredPosition = new Vector2(0f, AnchorY(anchor));
            _panel.gameObject.SetActive(true);
            _hint.gameObject.SetActive(blocking);
            _tapCatcher.gameObject.SetActive(blocking);

            // 읽기 단계에서는 탭 수집판이 패널을 덮지 않도록 패널을 위로 올린다.
            if (blocking) _panel.SetAsLastSibling();
            _skipButton.transform.SetAsLastSibling();
        }

        /// <summary>
        /// 문구를 내린다.
        ///
        /// <b>탭 수집판도 반드시 같이 내린다.</b> 그 판은 화면을 어둡게 하는 것이자 탭을
        /// 삼키는 것이라, 문구만 지우고 남겨 두면 화면이 어두운 채로 굳고 보드를 눌러도
        /// 반응하지 않는다. 켜는 곳(<see cref="Show"/>)이 한 곳이면 끄는 곳도 한 곳이어야 한다.
        /// </summary>
        public void HidePanel()
        {
            _panel.gameObject.SetActive(false);
            _tapCatcher.gameObject.SetActive(false);
        }

        private static float AnchorY(TutorialAnchor anchor)
        {
            switch (anchor)
            {
                case TutorialAnchor.Top: return UiTheme.TutorialPanelTopY;
                case TutorialAnchor.Bottom: return UiTheme.TutorialPanelBottomY;
                default: return UiTheme.TutorialPanelCenterY;
            }
        }

        // ---- 강조 테두리 ----

        /// <summary>지정한 사각형을 감싸는 테두리를 켠다. null이면 끈다.</summary>
        public void HighlightTarget(RectTransform target)
        {
            _ringTarget = target;
            if (target == null)
            {
                HideRing();
                return;
            }

            _ring.gameObject.SetActive(true);
            _ring.SetAsLastSibling();
            _skipButton.transform.SetAsLastSibling();
            _pulseTime = 0f;
            FitRing();
        }

        public void HideRing()
        {
            _ringTarget = null;
            _ring.gameObject.SetActive(false);
        }

        /// <summary>
        /// 매 프레임 호출한다(<see cref="TutorialController"/>의 Update).
        /// 테두리를 대상에 다시 맞추고 밝기를 오르내리게 한다. 대상이 트레이 주사위처럼
        /// 움직이는 것일 수 있어 한 번만 맞춰 두면 어긋난다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_ringTarget == null || !_ring.gameObject.activeSelf) return;

            FitRing();

            _pulseTime += deltaTime;
            float phase = Mathf.PingPong(_pulseTime / UiTheme.TutorialRingPulseSeconds, 1f);
            float alpha = Mathf.Lerp(UiTheme.TutorialRingMinAlpha, 1f, phase);
            for (int i = 0; i < RingSideCount; i++)
            {
                if (_ringSides[i] == null) continue;
                Color c = UiTheme.TutorialRing;
                c.a = alpha;
                _ringSides[i].color = c;
            }
        }

        /// <summary>대상의 화면상 사각형을 안내 레이어 좌표로 옮겨 테두리 크기를 맞춘다.</summary>
        private void FitRing()
        {
            _ringTarget.GetWorldCorners(_corners);

            Vector3 min = _root.InverseTransformPoint(_corners[0]); // 좌하
            Vector3 max = _root.InverseTransformPoint(_corners[2]); // 우상

            float pad = UiTheme.TutorialRingPadding;
            _ring.anchoredPosition = new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
            _ring.sizeDelta = new Vector2(max.x - min.x + pad * 2f, max.y - min.y + pad * 2f);
        }
    }
}
