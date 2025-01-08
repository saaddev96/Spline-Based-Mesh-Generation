using UnityEditor;
using UnityEngine;

namespace THLT.SplineMeshGeneration.Scripts.Commands
{
    public class CreateKnotCommand :ICommand
    {

        private Vector2 _position;
        private ISpline _spline;
        public CreateKnotCommand(ISpline spline, Vector2 position)
        {
            _spline = spline;
            _position=position;
        }
        public void Execute()
        {
            if(_spline.IsSplineClosed) return;
            var ray = HandleUtility.GUIPointToWorldRay(_position);
            if (!Physics.Raycast(ray, out var hit)) return;
            var point = hit.point;
            if (_spline.Knots.Count > 0)
            {
                if ((hit.point - _spline.Knots[0].knotCenter.position).magnitude <= _spline.KnotMaxDistance)
                {
                    _spline.Knots.Add(_spline.Knots[0]);
                    return;
                }
            }
            var localPoint = _spline.Root.gameObject.transform.InverseTransformPoint(point);
            var knt = _spline.KnotConstructor($"knot{_spline.Knots.Count}" ,_spline.Root.gameObject.transform, localPoint,_spline.HandleTexture,_spline.KnotCenterTexture);
            _spline.Knots.Add(knt);
        }

        public void Undo()
        {
            if (_spline.Knots.Count <= 0) return;
            var lastKnot = _spline.Knots[^1];
            var isClosed = _spline.IsSplineClosed;
            _spline.Knots.RemoveAt(_spline.Knots.Count - 1);
            BezierSplineToughDataSimpling.Sample(_spline);
            _spline.GenerateMesh();
            if (isClosed && _spline.Knots.Count > 1) // check if spline is closed and there is more than 1 knot 
                return;
            EditorUtility.SetDirty(lastKnot.knotCenter.gameObject);
            Object.DestroyImmediate(lastKnot.knotCenter.gameObject);
        }
    }
}
