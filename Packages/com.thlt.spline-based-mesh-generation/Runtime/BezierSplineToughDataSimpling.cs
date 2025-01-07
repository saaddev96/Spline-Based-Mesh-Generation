using System;
using System.Linq;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

namespace THLT.SplineMeshGeneration.Scripts
{
    public static class BezierSplineToughDataSimpling
    {
        private static readonly float4x4 BernsteinBasisMatrix = new float4x4(new float4(1, -3, 3, -1), new float4(0, 3, -6, 3), new float4(0, 0, 3, -3), new float4(0, 0, 0, 1));

        private static NativeList<CurveControlPoints> _curvesControlPoints;
        private static NativeArray<float4x4> _curvesPoints;
        private static NativeArray<float> _partialLenghts;
        // Caching segments along with the points in Dictionary List<(BezierKnot,BezierKnot),List<Vector3>> and only update the modified Segments
        public static void Sample(ISpline spline)
        {
            DateTime start = DateTime.Now;
            if(spline is null) throw new ArgumentNullException(nameof(spline));
            spline.Data.Clear();
            if (!IsValidKnotConfiguration(spline))
                return;
            try
            {
                _curvesControlPoints = new NativeList<CurveControlPoints>(Allocator.TempJob);
                var segmentsInv = 1 / (float)(spline.Segments-1);
                var splineCurvesCount = spline.Knots.Count - 1;
                for (var i = 0; i < splineCurvesCount; i++)
                {
                    var curvePoints = new CurveControlPoints(spline.Knots[i].knotCenter.position,
                        spline.Knots[i].rightHandle.position, spline.Knots[i + 1].leftHandle.position,
                        spline.Knots[i + 1].knotCenter.position, spline.Knots[i].knotCenter.localRotation,spline.Knots[i+1].knotCenter.localRotation);
                    _curvesControlPoints.Add(curvePoints);
                }
                _partialLenghts = new NativeArray<float>(_curvesControlPoints.Length, Allocator.TempJob);
                _curvesPoints = new NativeArray<float4x4>(_curvesControlPoints.Length * spline.Segments, Allocator.TempJob);
                var splineSamplingJob = new SampleSplineJob
                {
                    CurvesControls = _curvesControlPoints.AsDeferredJobArray(),
                    Points = _curvesPoints,
                    Segments = spline.Segments,
                    SegmentInv = segmentsInv,
                    IsClosed = spline.IsSplineClosed,
                    PartialLengths = _partialLenghts
                };
                splineSamplingJob.Schedule(_curvesControlPoints.Length, 20).Complete();
                spline.Length = _partialLenghts.Aggregate((x,y)=>x+y);
                foreach (var c in _curvesPoints)
                {
                    
                    if (IsValidFrame(c))
                    {
                        spline.Data.Add( (Matrix4x4)c);
                    }
                    else
                    {
                        Debug.LogWarning("Invalid Frame");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during spline sampling: {e.Message}");
                _curvesControlPoints.Dispose();
                _curvesPoints.Dispose();
                _partialLenghts.Dispose();
                spline.Data.Clear();
            }
            finally
            {
                _partialLenghts.Dispose();
                _curvesControlPoints.Dispose();
                _curvesPoints.Dispose();
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

        private static bool IsValidFrame(float4x4 matrix)
        {
            // Check for NaN or Infinity
            for (int row = 0; row < 4; row++)
            {
                float4 currentRow = matrix[row] ;
                for (int v = 0; v < 4; v++)
                {
                    if (float.IsNaN(currentRow[v]) || float.IsInfinity(currentRow[v]))
                        return false;
                }
            }
           
            // Check Orthonormality (within tolerance)
            const float tolerance = 0.01f;
            var right = matrix.c0;
            var up = matrix.c1;
            var forward = matrix.c2;
            // return math.abs(math.dot(right, up)) < tolerance &&
            //        math.abs(math.dot(up, forward)) < tolerance &&
            //        math.abs(math.dot(right, forward)) < tolerance &&
            //        math.abs(math.lengthsq(right) - 1f) < tolerance &&
            //        math.abs(math.lengthsq(up)  - 1f) < tolerance &&
            //        math.abs(math.lengthsq(forward) - 1f) < tolerance;
            return math.abs(math.lengthsq(right) - 1f) < tolerance &&math.abs(math.lengthsq(up)  - 1f) < tolerance &&math.abs(math.lengthsq(forward) - 1f) < tolerance;
        }

        public static float4x4 GetBezierFrameMatrix(float t, CurveControlPoints controlPoints) 
        {
           
            var paramMatrix = ParametricMatrix(t);
            var pointsMatrix = ControlPointsMatrix(controlPoints);
            var tangentMatrix = ParametricDerivativeMatrix(t);
            var point = math.mul(math.mul(paramMatrix, BernsteinBasisMatrix), pointsMatrix).GetRow(0);
            var tangent = math.mul(math.mul(tangentMatrix, BernsteinBasisMatrix), pointsMatrix).GetRow(0);
            var rot = math.slerp(controlPoints.R0, controlPoints.R1, t);
            var normalizedTangent = math.normalize(tangent);
            var  biNormal = math.normalize(new float4(math.mul(rot,new float3(-normalizedTangent.z, 0, normalizedTangent.x)), 0));
            var  normal =  math.normalize(new float4(math.cross(new float3(biNormal.x, biNormal.y, biNormal.z), new float3(normalizedTangent.x, normalizedTangent.y, normalizedTangent.z)), 0));
           // var matrix =  new float4x4(-biNormal, normal, normalizedTangent, point);

            var finalMatrix =  new float4x4(-biNormal,normal, normalizedTangent, point);
            return finalMatrix;
        }
        private static float4x4 ControlPointsMatrix(CurveControlPoints controlPoints)
        {
            var matrix = new float4x4();
            matrix = matrix.SetRow(0, new float4(controlPoints.K0, 0));
            matrix = matrix.SetRow(1, new float4(controlPoints.K1, 0));
            matrix = matrix.SetRow(2, new float4(controlPoints.K2, 0));
            matrix = matrix.SetRow(3, new float4(controlPoints.K3, 0));
            return matrix;
        }

        private static float4x4 ParametricMatrix(float t)
        {
            var matrix = new float4x4();
            var indeterminate = new float4(1, t, t * t, t * t * t);
            matrix = matrix.SetRow(0, indeterminate);
            return matrix;
        }

        private static float4x4 ParametricDerivativeMatrix(float t)
        {
            var matrix = new float4x4();
            var indeterminate = new float4(0, 1, 2 * t, 3 * t * t);
            matrix = matrix.SetRow(0, indeterminate);
            return matrix;
        }

        public struct CurveControlPoints
        {
            public readonly float3 K0;
            public readonly float3 K1;
            public readonly float3 K2;
            public readonly float3 K3;
            public readonly quaternion R0;
            public readonly quaternion R1;

            public CurveControlPoints(float3 k0, float3 k1, float3 k2, float3 k3) : this()
            {
                K0 = k0;
                K1 = k1;
                K2 = k2;
                K3 = k3;
            }

            public CurveControlPoints(float3 k0, float3 k1, float3 k2, float3 k3,quaternion r0, quaternion r1):this(k0,k1,k2,k3)
            {
                R0 = r0;
                R1 = r1;
            }
        }

        [BurstCompile]
        public struct SampleSplineJob : IJobParallelFor
        {
            public int Segments;
            public float SegmentInv;
            public bool IsClosed;
            [NativeDisableParallelForRestriction] public NativeArray<float4x4> Points;
            [NativeDisableParallelForRestriction] public NativeArray<float> PartialLengths;
            [ReadOnly] public NativeArray<CurveControlPoints> CurvesControls;

            public void Execute(int index)
            {
                float curveLength = 0;
                for (var j = 0; j < Segments; j++)
                {
                    var t = j * SegmentInv;
                    var frame = GetBezierFrameMatrix(t, CurvesControls[index]);
                    var currentFrameIndex = index * Segments + j;
                    Points[currentFrameIndex] = frame;
                    if (j > 0)
                    {
                        curveLength += math.distance(Points[currentFrameIndex - 1].c3, frame.c3);
                    }
                }
                // replacing the last frame with the first one when the spline is closed avoiding duplication at the last knot
                if (IsClosed)
                {
                    Points[^1] = Points[0];
                }

                PartialLengths[index] = curveLength;
            }
        }
    }
}

public static class DataExtension
{
    public static float4x4 SetRow(this float4x4 matrix, int index, float4 row)
    {
        if (!IsValidIndex(index)) return matrix;
        // Convert matrix to rows
        NativeArray<float4> rows = new NativeArray<float4>(4, Allocator.Temp);
        rows[0] = new float4(matrix.c0.x, matrix.c1.x, matrix.c2.x, matrix.c3.x);
        rows[1] = new float4(matrix.c0.y, matrix.c1.y, matrix.c2.y, matrix.c3.y);
        rows[2] = new float4(matrix.c0.z, matrix.c1.z, matrix.c2.z, matrix.c3.z);
        rows[3] = new float4(matrix.c0.w, matrix.c1.w, matrix.c2.w, matrix.c3.w);

        // Update the specific row
        rows[index] = row;
        var result = new float4x4(
            new float4(rows[0].x, rows[1].x, rows[2].x, rows[3].x),
            new float4(rows[0].y, rows[1].y, rows[2].y, rows[3].y),
            new float4(rows[0].z, rows[1].z, rows[2].z, rows[3].z),
            new float4(rows[0].w, rows[1].w, rows[2].w, rows[3].w)
        );
        rows.Dispose();
        // Reconstruct the matrix
        return result;
    }

    public static float4 GetRow(this float4x4 matrix, int index) => index switch
    {
        0 => new float4(matrix.c0.x, matrix.c1.x, matrix.c2.x, matrix.c3.x),
        1 => new float4(matrix.c0.y, matrix.c1.y, matrix.c2.y, matrix.c3.y),
        2 => new float4(matrix.c0.z, matrix.c1.z, matrix.c2.z, matrix.c3.z),
        3 => new float4(matrix.c0.w, matrix.c1.w, matrix.c2.w, matrix.c3.w),
        _ => throw new ArgumentOutOfRangeException(nameof(index), "Index must be between 0 and 3")
    };

    public static float4x4 SetColumn(this float4x4 matrix, int index, float4 point)
    {
        if (!IsValidIndex(index)) return matrix;
        switch (index)
        {
            case 0:
                matrix.c0 = point;
                break;
            case 1:
                matrix.c1 = point;
                break;
            case 2:
                matrix.c2 = point;
                break;
            case 3:
                matrix.c3 = point;
                break;
        }

        return matrix;
    }
    
    private static bool IsValidIndex(int index)
    {
        if (index is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be between 0 and 3.");

        return true;
    }
}