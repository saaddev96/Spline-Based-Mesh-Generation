using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace THLT.SplineMeshGeneration.Scripts
{
    public static class BezierSplineDataSampling
    {
        private static readonly Matrix4x4 BernsteinBasisMatrix = new Matrix4x4(new Vector4(1, -3, 3, -1),
            new Vector4(0, 3, -6, 3), new Vector4(0, 0, 3, -3), new Vector4(0, 0, 0, 1));
        
        public static void Sample(ISpline spline) // Caching segments along with the points in Dictionary List<(BezierKnot,BezierKnot),List<Vector3>> and only update the modified Segments
        {
            DateTime start = DateTime.Now;
            spline.Data.Clear();
            if (!IsValidKnotConfiguration(spline))
                return;
            try
            {
                var segmentsInv = 1 / (float)spline.Segments;
                var splineCurvesCount = spline.Knots.Count - 1;
                for (var i = 0; i < splineCurvesCount; i++)
                {
                     var b = (spline.Knots[i], spline.Knots[i + 1]);
                    for (var j = 0; j < spline.Segments; j++)
                    {
                        var t = j * segmentsInv;
                        var point = GetBezierFrameMatrix(t,b);
                        if (IsValidFrame(point))
                            spline.Data.Add(point);
                    }
                }
                if (!spline.IsSplineClosed) // adding the last Point Data to avoid duplication at the last knot and skipping it  if spline is closed
                {
                    var point = GetBezierFrameMatrix(1, (spline.Knots[^2], spline.Knots[^1]));
                    if (IsValidFrame(point))
                        spline.Data.Add(point);
                }
                else
                {
                    spline.Data.Add(spline.Data[0]);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during spline sampling: {e.Message}");
                spline.Data.Clear();
            }
            var duration = DateTime.Now - start;
            Debug.Log($"Done in {duration.Milliseconds} ms");
        }

        private static bool IsValidKnotConfiguration(ISpline spline)
        {
            if (spline.Knots.Count < 2 || spline.Knots == null)
                return false;
            return !spline.Knots.Any(k => k is null); // Check for null knots
        }

        private static bool IsValidFrame(Matrix4x4 matrix)
        {
            // Check for NaN or Infinity
            for (var i = 0; i < 16; i++)
            {
                if (float.IsNaN(matrix[i]) || float.IsInfinity(matrix[i]))
                    return false;
            }

            // Check orthonormality (within tolerance)
            const float tolerance = 0.0002f;
            var right = matrix.GetColumn(0);
            var up = matrix.GetColumn(1);
            var forward = matrix.GetColumn(2);

            return Mathf.Abs(Vector3.Dot(right, up)) < tolerance &&
                   Mathf.Abs(Vector3.Dot(up, forward)) < tolerance &&
                   Mathf.Abs(Vector3.Dot(right, forward)) < tolerance &&
                   Mathf.Abs(right.sqrMagnitude - 1f) < tolerance &&
                   Mathf.Abs(up.sqrMagnitude - 1f) < tolerance &&
                   Mathf.Abs(forward.sqrMagnitude - 1f) < tolerance;
        }

        public static Matrix4x4 GetBezierFrameMatrix(float t, (BezierKnot, BezierKnot) controlPoints)
        {
            var paramMatrix = ParametricMatrix(t);
            var pointsMatrix = ControlPointsMatrix(controlPoints);
            var tangentMatrix = ParametricDerivativeMatrix(t);
            var point = (paramMatrix * BernsteinBasisMatrix * pointsMatrix).GetRow(0);
            var tangent = (tangentMatrix * BernsteinBasisMatrix * pointsMatrix).GetRow(0);
            var biNormal = new Vector3(-tangent.z, 0, tangent.x);
            var normal = Vector3.Cross(biNormal, tangent);
            return new Matrix4x4(-biNormal.normalized, normal.normalized, tangent.normalized, point);
        }

        private static Matrix4x4 ControlPointsMatrix((BezierKnot k0, BezierKnot k1) controlPoints)
        {
            var matrix = new Matrix4x4();
            matrix.SetRow(0, controlPoints.k0.knotCenter.position);
            matrix.SetRow(1, controlPoints.k0.rightHandle.position);
            matrix.SetRow(2, controlPoints.k1.leftHandle.position);
            matrix.SetRow(3, controlPoints.k1.knotCenter.position);
            return matrix;
        }

        private static Matrix4x4 ParametricMatrix(float t)
        {
            var matrix = new Matrix4x4();
            var indeterminate = new Vector4(1, t, t * t, t * t * t);
            matrix.SetRow(0, indeterminate);
            return matrix;
        }

        private static Matrix4x4 ParametricDerivativeMatrix(float t)
        {
            var matrix = new Matrix4x4();
            var indeterminate = new Vector4(0, 1, 2 * t, 3 * t * t);
            matrix.SetRow(0, indeterminate);
            return matrix;
        }
    }
}