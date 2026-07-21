using System;
using UnityEngine;
using UnityEngine.UI;
using DiceBattle.Core;

namespace DiceBattle.UI
{
    /// <summary>
    /// 한 라인(3칸 + 점수 라벨). 전체 행이 하나의 탭 버튼이며,
    /// 클릭 시 (필드, 라인인덱스)를 알린다.
    /// </summary>
    public sealed class LineView
    {
        public PlayerId Field { get; }
        public int Index { get; }

        private readonly Button _button;
        private readonly Image _bg;
        private Text _scoreLabel; // 생성자 내 헬퍼 메서드에서 할당되므로 readonly 불가
        private readonly CellView[] _cells = new CellView[Line.Capacity];

        public event Action<PlayerId, int> Clicked;

        /// <param name="scoreFirst">true면 [점수][칸들] 순서(점수가 왼쪽=내측), false면 [칸들][점수] 순서.</param>
        public LineView(Transform parent, PlayerId field, int index, bool scoreFirst)
        {
            Field = field;
            Index = index;

            _button = UiFactory.CreateButton($"Line_{field}_{index}", parent, UiTheme.LineNormal);
            _bg = _button.GetComponent<Image>();
            UiFactory.SetSize(_button.gameObject,
                UiTheme.CellSize * 3 + UiTheme.CellSpacing * 2 + 120f,
                UiTheme.CellSize + UiTheme.CellSpacing);

            var row = _button.gameObject;
            UiFactory.AddHorizontalLayout(row, (int)UiTheme.CellSpacing, new RectOffset(16, 16, 8, 8));

            // 점수/칸 순서를 내측(중앙) 기준으로 배치.
            // scoreFirst=true  → 우측 필드(AI): [점수][칸0..2]  (칸은 왼쪽=내측부터 채움)
            // scoreFirst=false → 좌측 필드(나): [칸2..0][점수]  (칸은 오른쪽=내측부터 채움)
            if (scoreFirst)
            {
                CreateScoreLabel(row.transform);
                CreateCells(row.transform, innerLeft: true);
            }
            else
            {
                CreateCells(row.transform, innerLeft: false);
                CreateScoreLabel(row.transform);
            }

            _button.onClick.AddListener(() => Clicked?.Invoke(Field, Index));
            ClearHighlight();
        }

        private void CreateScoreLabel(Transform parent)
        {
            _scoreLabel = UiFactory.CreateText("Score", parent, "0", UiTheme.ScoreFontSize, UiTheme.Label);
            UiFactory.SetSize(_scoreLabel.gameObject, 96f, UiTheme.CellSize);
        }

        /// <summary>
        /// 칸(CellView)을 생성한다. _cells[0]이 항상 "내측(중앙에 가까운) 칸"이 되도록 배치해
        /// 주사위가 내측부터 채워지게 한다.
        /// </summary>
        private void CreateCells(Transform parent, bool innerLeft)
        {
            if (innerLeft)
            {
                // 내측이 왼쪽: 왼쪽부터 index 0,1,2
                for (int i = 0; i < _cells.Length; i++)
                    _cells[i] = new CellView(parent);
            }
            else
            {
                // 내측이 오른쪽: 시각적으로 왼쪽부터 만들되 index는 2,1,0 (오른쪽이 index 0)
                for (int i = _cells.Length - 1; i >= 0; i--)
                    _cells[i] = new CellView(parent);
            }
        }

        /// <summary>모델 라인 상태를 그대로 반영.</summary>
        public void Render(Line line)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                if (i < line.Count)
                {
                    var d = line.Dice[i];
                    _cells[i].SetDie(d.Value, d.IsSpecial);
                }
                else
                {
                    _cells[i].SetEmpty();
                }
            }
            _scoreLabel.text = line.Score().ToString();
        }

        public CellView Cell(int i) => _cells[i];

        /// <summary>연출용 배경 그래픽(제거 플래시 등).</summary>
        public Image Background => _bg;

        /// <summary>탭 가능 여부 + 강조 표시.</summary>
        public void SetSelectable(bool selectable)
        {
            _button.interactable = selectable;
            if (selectable)
                UiSkin.Apply(_bg, UiSkin.LineHighlight, UiTheme.LineHighlight);
            else
                UiSkin.Apply(_bg, UiSkin.LineNormal, UiTheme.LineNormal);
        }

        public void ClearHighlight()
        {
            _button.interactable = false;
            UiSkin.Apply(_bg, UiSkin.LineNormal, UiTheme.LineNormal);
        }
    }
}
