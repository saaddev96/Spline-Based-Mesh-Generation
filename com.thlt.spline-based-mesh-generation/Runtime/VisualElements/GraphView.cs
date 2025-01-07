using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


namespace THLT.SplineMeshGeneration.Scripts.VisualElements
{
    [UxmlElement]
    public partial class GraphView : VisualElement, INotifyValueChanged<int>
    {
 
        private GraphViewController _graphViewController;
        private Stack<Label> _labelStack;
        private List<Label> _labels;
        private  bool _isDrawing ; 

        /// <summary>
        /// <value> <c>value</c> represent the grid Size of the graphic interface </value>
        /// </summary>
        [UxmlAttribute]
        public int value
        {
            get
            {
                _graphViewController??= new GraphViewController(this);
                return _graphViewController.GridSize;
            }
            set
            {
                _graphViewController ??= new GraphViewController(this);
                _graphViewController.GridSize = value;
                DrawCoords();
                MarkDirtyRepaint();
            }
        }

        public GraphViewController GraphViewController => _graphViewController;
        public GraphView()
        {
            _graphViewController = new GraphViewController(this);
            _labelStack = new Stack<Label>();
            _labels = new List<Label>();
            DrawCoords();
            generateVisualContent += GenerateVisualContent;
            RegisterCallback<GeometryChangedEvent>(x => MarkDirtyRepaint());
            RegisterCallback<MouseDownEvent>(m =>
            {
                if (m.button == 0)
                {
                    _isDrawing = true;
                    _graphViewController.HandleGraphPointerDown(m.mousePosition);
                }
            });
            RegisterCallback<MouseUpEvent>(m =>
            {
                if (m.button == 0)
                {
                    _isDrawing = false;
                }
            });
            RegisterCallback<MouseMoveEvent>(m =>
            {
                if (_isDrawing)
                {
                    _graphViewController.HandleGraphPointerDrag(m.mousePosition);
                }
            });
            RegisterCallback<KeyDownEvent>(k =>
            {
                if (k.keyCode == KeyCode.C)
                {
                    _graphViewController.ClearShape(_graphViewController.CurrentShape);
                }
            });
            RegisterCallback<WheelEvent>(w =>
            {
                _graphViewController.HandleGraphScrolling(w.delta.y);
                _graphViewController.UpdateShape();
            });
        }


        public void DrawCoords()
        {
            ReleaseLabels();
            for (float i = _graphViewController.HalfSize; i >= -_graphViewController.HalfSize; i--)
            {
                Vector2 xLineCoord = new(i, 0);
                Vector2 yLineCoord = new(0, i);
                Vector3 xGraphCoord = _graphViewController.GridPositionToMouse(xLineCoord);
                Vector3 yGraphCoord = _graphViewController.GridPositionToMouse(yLineCoord);
                var xLabel = GetOrCreateLabel($"{xLineCoord.x}", xGraphCoord.y + _graphViewController.LabelOffset,
                    xGraphCoord.x + _graphViewController.LabelOffset);
                var yLabel = GetOrCreateLabel($"{yLineCoord.y}", yGraphCoord.y + _graphViewController.LabelOffset,
                    yGraphCoord.x + _graphViewController.LabelOffset);
                _labels.Add(xLabel);
                _labels.Add(yLabel);
                Add(xLabel);
                Add(yLabel);
            }
        }

       
        Label GetOrCreateLabel(string labelText, float top, float left)
        {
            Label label;
            if (_labelStack?.Count == 0)
            {
                label = new Label();
            }
            else
            {
                label = _labelStack?.Pop() ?? new Label();
            }

            label.text = labelText;
            label.style.position = Position.Absolute;
            label.style.top = top;
            label.style.left = left;
            label.style.fontSize = _graphViewController.FontSize;
            label.style.color = Color.white;
            return label;
        }
        void ReleaseLabels()
        {
            foreach (var label in _labels)
            {
                _labelStack.Push(label);
                Remove(label);
            }
            _labels.Clear();
        }
       
      
        void GenerateVisualContent(MeshGenerationContext context)
        {
            var painter2D = context.painter2D;
            painter2D.lineWidth = 1.0f;
            painter2D.lineJoin = LineJoin.Miter;
            painter2D.strokeColor = _graphViewController.GridColor;
            for (int x = _graphViewController.BlockSize * _graphViewController.GridSize; x >= 0; x -= _graphViewController.BlockSize)
            {
                painter2D.BeginPath();
                painter2D.MoveTo(new Vector2(0, x));
                painter2D.LineTo(new Vector2(_graphViewController.BlockSize * _graphViewController.GridSize, x));
                painter2D.MoveTo(new Vector2(x, 0));
                painter2D.LineTo(new Vector2(x, _graphViewController.BlockSize * _graphViewController.GridSize));
                painter2D.Stroke();
                painter2D.ClosePath();
            }
        
            painter2D.lineWidth = 2.0f;
            painter2D.strokeColor = _graphViewController.IndicatorsColor;
            painter2D.BeginPath();
            painter2D.MoveTo(_graphViewController.GridPositionToMouse(new Vector2(0, _graphViewController.HalfSize)));
            painter2D.LineTo(_graphViewController.GridPositionToMouse(new Vector2(0, -_graphViewController.HalfSize)));
            painter2D.MoveTo(_graphViewController.GridPositionToMouse(new Vector2(-_graphViewController.HalfSize, 0)));
            painter2D.LineTo(_graphViewController.GridPositionToMouse(new Vector2(_graphViewController.HalfSize, 0)));
            painter2D.Stroke();
            painter2D.ClosePath();
        }
        
        public void SetValueWithoutNotify(int newValue)
        {
            _graphViewController.GridSize = newValue;
        }
        
        ~GraphView()
        {
            generateVisualContent -= GenerateVisualContent;
        }
        
    }
}