using THLT.SplineMeshGeneration.Scripts.Scriptables;
using THLT.SplineMeshGeneration.Scripts.VisualElements;
using UnityEngine;

namespace THLT.SplineMeshGeneration.Scripts.Commands
{
    public class AddAndDrawSegmentCommand : ICommand
    {
        private Vector2 _snapPoint;
        private GraphMesh2D _targetShape;
        private GraphViewController _graphController;
        private Vertex _vertex1;
        private Vertex _vertex2;
        private int _vertAddedIndex;
        private LineDrawer _line;
        private CircleShape _circle;
        public GraphMesh2D TargetShape => _targetShape;
        public Vertex Vertex1 => _vertex1;
        public Vertex Vertex2 => _vertex2;
        public AddAndDrawSegmentCommand(GraphViewController graphController, GraphMesh2D targetShape, Vector2 snapPoint)
        {
            _graphController= graphController;
            _targetShape = targetShape;
            _snapPoint = snapPoint;
        }
        
        public void Execute()
        {
            _vertAddedIndex = _targetShape.VertsData.Count;
            
            _vertex1 = new Vertex(_snapPoint, _targetShape.VertsData.Count);
            _targetShape.VertsData.Add(_vertex1);
            
          
            _vertex2 = new Vertex(_snapPoint, _targetShape.VertsData.Count);
            _targetShape.VertsData.Add(_vertex2);
            
            _circle = _graphController.DrawVertex(_snapPoint,_targetShape.DrawingColor);
            _targetShape.VertsPoints.Add(_circle);
            
            if (_targetShape.VertsData.Count <= 2) return;
            
            _line = _graphController.DrawLine(_targetShape.VertsData[^4].Point, _targetShape.VertsData[^1].Point,_targetShape.DrawingColor); 
            _targetShape.VertsLines.Add(_line);
        
        }

        public void Undo()
        {
            if (_targetShape.VertsData.Count> _vertAddedIndex)
            {
                _targetShape.VertsData.RemoveAt(_vertAddedIndex);
            }
            if (_targetShape.VertsData.Count > _vertAddedIndex)
            {
                _targetShape.VertsData.RemoveAt(_vertAddedIndex);
            }

            if (_targetShape.VertsLines.Contains(_line))
            {
                _targetShape.VertsLines.Remove(_line);
                _graphController.RemoveElementFromGraph(_line);
            }

            if (_targetShape.VertsPoints.Contains(_circle))
            {
                _targetShape.VertsPoints.Remove(_circle);
                _graphController.RemoveElementFromGraph(_circle);
            }
            _graphController.ResetSelectedVertex();
        }
    }
}
