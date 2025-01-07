using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace THLT.SplineMeshGeneration.Scripts.VisualElements
{
    [UxmlElement]
    public partial class LineDrawer : VisualElement
    {
        public enum LineCapType
        {
            Arrow,
            Circle
        }
        private Vector2 _start;
        private Vector2 _end;
        private Color _color = Color.white;
        private float _thickness = 1f;
        private GraphViewController _targetGraphController;
        private LineCapType _lineCapType = LineCapType.Circle;
        [UxmlAttribute]
        public Vector2 Start
        {
            get=>_start;
            set
            {
               
                _start = value;
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public Vector2 End
        {
            get=>_end;
            set
            {
            
                _end = value;
                MarkDirtyRepaint();
            }
        }
        [UxmlAttribute]
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                MarkDirtyRepaint();
            }
        }
        [UxmlAttribute]
        public float Thickness
        {
            get => _thickness;
            set
            {
                _thickness = value;
                MarkDirtyRepaint();
            }
        }

        public LineDrawer()
        {
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<GeometryChangedEvent>(x => MarkDirtyRepaint());
        }
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var startMousePos = _targetGraphController.GridPositionToMouse(_start);
            var endMousePos = _targetGraphController.GridPositionToMouse(_end);
            if (_targetGraphController == null) throw new ArgumentNullException(nameof(_targetGraphController));
            var painter = mgc.painter2D;
            painter.strokeColor = _color;
            painter.lineWidth = 2;
            painter.fillColor = Color.white;
            switch (_lineCapType)
            {
                case LineCapType.Circle:
                    painter.BeginPath();
                    painter.MoveTo(startMousePos+(endMousePos-startMousePos).normalized*5);
                    painter.LineTo(endMousePos-(endMousePos-startMousePos).normalized*5);
                    painter.Stroke();
                    painter.ClosePath();
                    // painter.BeginPath();
                    // painter.Arc(startMousePos,5,0,360);
                    // painter.Fill();
                    // painter.Stroke();
                    // painter.BeginPath();
                    // painter.Arc(endMousePos,5,0,360);
                    // painter.Fill();
                    // painter.Stroke();
                    // painter.ClosePath();
                    break;
                case LineCapType.Arrow:
                    painter.BeginPath();
                    painter.MoveTo(startMousePos+(endMousePos-startMousePos).normalized*5);
                    painter.LineTo(endMousePos);
                    var dir = (endMousePos - startMousePos).normalized;
                    painter.MoveTo(endMousePos+new Vector2(-dir.y,dir.x).normalized*5f-dir*10f);
                    painter.LineTo(endMousePos);
                    painter.MoveTo(endMousePos-new Vector2(-dir.y,dir.x).normalized*5f-dir*10f);
                    painter.LineTo(endMousePos);
                    painter.Stroke();
                    painter.ClosePath();
                    break;
            }
          
        }
        ~LineDrawer()
        {
            generateVisualContent -= OnGenerateVisualContent;
        }
        public class Line
        {
            private Vector2 _start;
            private Vector2 _end;
            private Color _color = Color.white;
            private float _thickness = 1f;
            private GraphViewController _graphController;
            private LineCapType _capType;
            public Line(GraphViewController graphController)
            {
                _graphController = graphController;
            }
            public Line Create(Vector2 start, Vector2 end)
            {
                _start = start;
                _end = end;
                return this;
            }

            public Line SetStyle(Color color, float thickness,LineCapType lineCapType = LineCapType.Circle)
            {
                _color = color;
                _thickness = thickness;
                _capType = lineCapType;
                return this;
            }

            public LineDrawer Draw()
            {
                var line = new LineDrawer
                {
                    _targetGraphController = _graphController,
                    _start = _start,
                    _end = _end,
                    _color = _color,
                    _thickness = _thickness,
                    _lineCapType =_capType
                };
                return line;
            }
        }
    }
}
