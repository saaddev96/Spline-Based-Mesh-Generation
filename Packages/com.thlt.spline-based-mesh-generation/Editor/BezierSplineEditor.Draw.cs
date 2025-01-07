using System.Linq;
using UnityEngine;
using UnityEditor;
namespace THLT.SplineMeshGeneration.Scripts.Editor
{
    public partial class BezierSplineEditor
    {
        
        [DrawGizmo(GizmoType.NonSelected)]
        private static void OnEditorDrawGizmos(Transform objectTransform, GizmoType gizmoType)
        {
            DrawKnotHandles(CurrentSpline?.MSpline);
            DrawSpline(CurrentSpline?.MSpline);
            DrawPointsHandle(CurrentSpline?.MSpline);
        }
        
        private static void DrawPointsHandle(ISpline spline)
        {
            if (spline is not { CanDrawGizmos: true }) return;
            foreach (var p in spline.Data)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(p.Point,  p.Point + p.Right);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(p.Point, p.Point + p.Up);
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(p.Point, p.Point + p.Forward);
            }
        }
        private static void DrawSpline(ISpline spline) 
        {
            if (spline is not { CanDrawGizmos: true } || spline.Knots.Count < 2) return;
            Handles.color = Color.black;
            Vector3[] points = new Vector3[spline.Data.Count];
            for (int i = 0; i < spline.Data.Count; i++)
            {
                points[i] = spline.Data[i].Point;
            }
            Handles.DrawPolyLine(points);
        }
        private static void DrawKnotHandles(ISpline spline)
        {
            if (spline is null || spline.Knots.Any(b=>b.Any(k=>k is null))) return;
            foreach (var knot in spline.Knots)
            {
                if(knot is null ) return;
                Gizmos.color = Color.cyan;
                var rot = (knot.rightHandle.position - knot.knotCenter.position) == Vector3.zero
                    ? Quaternion.identity
                    : Quaternion.LookRotation(knot.rightHandle.position - knot.knotCenter.position);
                Gizmos.DrawMesh(_knotMesh, knot.knotCenter.position,rot,Vector3.one*1f);
                Gizmos.DrawLine(knot.knotCenter.position+(knot.leftHandle.position-knot.knotCenter.position).normalized*0.5f+Vector3.up*0.12f, knot.leftHandle.position);
                Gizmos.DrawLine(knot.knotCenter.position+(knot.rightHandle.position-knot.knotCenter.position).normalized*1.2f+Vector3.up*0.05f, knot.rightHandle.position);
               
            }
        }
    }
}