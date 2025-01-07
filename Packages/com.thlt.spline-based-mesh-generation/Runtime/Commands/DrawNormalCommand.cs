using THLT.SplineMeshGeneration.Scripts.VisualElements;
using UnityEngine;
namespace THLT.SplineMeshGeneration.Scripts.Commands
{
    public class DrawNormalCommand: ICommand
    {
        private Vector2 _snapPoint;
        private GraphViewController _targetGraphController;
        private int _normalIndex;
        public LineDrawer NormalLine;
        private int _shapeIndex;
        public DrawNormalCommand(GraphViewController graphController, Vector2 snapPoint,int targetNormalIndex)
        {
            _targetGraphController = graphController;
            _snapPoint = snapPoint;
            _normalIndex = targetNormalIndex;
        }
        public void Execute()
        {
            _shapeIndex = _targetGraphController.Shapes.IndexOf(_targetGraphController.CurrentShape);
            NormalLine = _targetGraphController.DrawLine(_targetGraphController.CurrentShape.VertsData[_normalIndex].Point, _snapPoint,_targetGraphController.CurrentShape.NormalsColor, capType: LineDrawer.LineCapType.Arrow); 
            _targetGraphController.CurrentShape.NormalLinesDictionary.Add(_normalIndex, NormalLine);
            var normal = (NormalLine.End - NormalLine.Start).normalized;
            _targetGraphController.CurrentShape.VertsData[_normalIndex] = _targetGraphController.CurrentShape.VertsData[_normalIndex].SetNormal(normal);
        }

        public void Undo()
        {
            if (NormalLine != null)
            {
                _targetGraphController.Shapes[_shapeIndex].NormalLinesDictionary.Remove(_normalIndex);
                _targetGraphController.RemoveElementFromGraph(NormalLine);
                _targetGraphController.Shapes[_shapeIndex].VertsData[_normalIndex] = _targetGraphController.CurrentShape.VertsData[_normalIndex].SetNormal(Vector2.zero);
            }
        }
    }
}
