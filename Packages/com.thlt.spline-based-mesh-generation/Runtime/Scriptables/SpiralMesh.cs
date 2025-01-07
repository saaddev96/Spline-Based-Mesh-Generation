using System;
using System.Collections.Generic;
using UnityEngine;

namespace THLT.SplineMeshGeneration.Scripts.Scriptables
{
    [CreateAssetMenu(fileName = "New SpiralMesh", menuName = "Scriptable Objects/SpiralMesh")]
    public class SpiralMesh : BaseMesh
    {
        public int maxPointsPerRoll = 20;
        public float radius=1;
        public override Mesh Generate(Mesh mesh, List<PointData> pointsData, Vector2 scale,Vector2 tiling, float length)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            if (pointsData == null) throw new ArgumentNullException(nameof(pointsData));
            if (mesh2dData.Count==0 || mesh2dData?[0] == null)
            {
                Debug.LogWarning("Mesh2dData is null or not assigned to SpiralMesh");
                return null;
            }
            // Clearing Mesh
            mesh.Clear();
            // Clearing Cached Lists
            ClearCachedLists();
            GenerateVertices(pointsData,scale,tiling,length);
            GenerateTriangles(pointsData.Count);
            mesh.SetVertices(Verts);
            mesh.RecalculateBounds();
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, Uvs);
            mesh.SetTriangles(Triangles, 0);
            return mesh;
        }

        protected override void GenerateVertices(List<PointData> pointsData,Vector2 scale,Vector2 tiling, float length)
        {
            const float TAU = Mathf.PI * 2f;
            var pointsPerRollInv = 1 / (float)maxPointsPerRoll;
            float Turn(int index) =>index * pointsPerRollInv;
            Vector3 CalculateOffset(int index)
            {
                var angle = Turn(index) * TAU;
                var offset = new Vector3(Mathf.Cos(angle),
                    Mathf.Sin(angle),
                    0f);
                return offset*radius;
            }
            
            for (var i = 0; i < pointsData.Count; i++)
            {
               var offset = CalculateOffset(i);
                var newSplinePoint = pointsData[i].OffsetMatrix(offset);
                if (i < pointsData.Count - 1)
                {
                    var nextOffset = CalculateOffset(i + 1);
                    var nextSplinePoint = pointsData[i + 1].OffsetMatrix(nextOffset);
                    var dir = nextSplinePoint.Point-newSplinePoint.Point;
                    newSplinePoint.RotateTowards(dir.normalized);
                   
                }
             
                foreach (var vert in mesh2dData[0].vertsData)
                {
                    Verts.Add(newSplinePoint.LocalToWorldPoint(new Vector2(vert.Point.x*scale.x, vert.Point.y*scale.y)));
                    Normals.Add(newSplinePoint.LocalToWorldVec(vert.Normal));
                    Uvs.Add(new Vector2(vert.U, Turn(i)*length/tiling.y*VMultiplier));
                }
            }
        }

        protected override void GenerateTriangles(int frameCount)
        {
            for (int slice = 0; slice < frameCount - 1; slice++)
            {
                var rootA = slice * IndicesCount;
                var rootB = (slice + 1) * IndicesCount;
                for (var line = 0; line < IndicesCount; line += 2)
                {
                    var currentIndex = indices[line];
                    var nextIndex = indices[line + 1];
                    var lineA1 = rootA + currentIndex;
                    var lineA2 = rootA + nextIndex;
                    var lineB1 = rootB + currentIndex;
                    var lineB2 = rootB + nextIndex;

                    // first Triangle
                    Triangles.Add(lineA1);
                    Triangles.Add(lineB1);
                    Triangles.Add(lineB2);
                    //Second Triangle
                    Triangles.Add(lineA1);
                    Triangles.Add(lineB2);
                    Triangles.Add(lineA2);
                }
            }
        }

    }
}