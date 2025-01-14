using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace THLT.SplineMeshGeneration.Scripts.VisualElements
{
    [UxmlElement]
    public partial class CircleShape : VisualElement
    {

        
        private GraphViewController _targetGraphController;
        
        [UxmlAttribute] public Vector2 Point { get; set; }
        [UxmlAttribute] public float Radius { get; set; }
        [UxmlAttribute] public Color Color { get; set; }
        [UxmlAttribute] public Color FillColor { get; set; }
        [UxmlAttribute] public float Gap { get; set; }
        [UxmlAttribute] public float Thickness { get; set; }
        
       
        public CircleShape()
        {

            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<GeometryChangedEvent>(x => MarkDirtyRepaint());
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_targetGraphController == null) throw new NullReferenceException(nameof(_targetGraphController));
            var pointToMousePosition = _targetGraphController.GridPositionToMouse(Point);
            var painter = mgc.painter2D;
            painter.strokeColor = Color;
            painter.fillColor = FillColor;
            painter.lineWidth = Thickness;
            painter.BeginPath();
            painter.Arc(pointToMousePosition,Radius,45f+Gap,225f-Gap);
            painter.Fill();
            painter.Stroke();
            painter.ClosePath();
            painter.BeginPath();
            painter.Arc(pointToMousePosition,Radius,225+Gap,405-Gap);
            painter.Fill();
            painter.Stroke();
            painter.ClosePath();
        }


        public class Circle
        {
            private Vector2 _point;
            private float _radius;
            private Color _color = Color.red;
            private Color _fillColor = Color.white;
            private float _gap = 1f;
            private float _thickness = 1f;
            private GraphViewController _graphController;

            public Circle(GraphViewController graphController)
            {
                _graphController = graphController;
            }

            public Circle Create(Vector2 point)
            {
                _point = point;
                return this;
            }

            public Circle SetStyle(Color color, Color fillColor = default, float radius=5, float gap=2, float thickness=1)
            {
                _color = color;
                _fillColor = fillColor;
                _radius = radius;
                _gap = gap;
                _thickness = thickness;
                return this;
            }

            public CircleShape Draw()
            {
                var circle = new CircleShape
                {
                    _targetGraphController= _graphController,
                    Point = _point,
                    Color = _color,
                    FillColor = _fillColor,
                    Radius = _radius,
                    Gap = _gap,
                    Thickness = _thickness
                 
                };
                return circle;
            }
        }
    }
}
