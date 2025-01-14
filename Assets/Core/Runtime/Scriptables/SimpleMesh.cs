using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace THLT.SplineMeshGeneration.Scripts.Scriptables
{
    [CreateAssetMenu(fileName = "New SimpleMesh", menuName = "Scriptable Objects/SimpleMesh")]
    public class SimpleMesh : BaseMesh
    {
        public bool closedCaps ;
        public override Mesh Generate(Mesh mesh, List<PointData> pointsData,Vector2 scale,Vector2 tiling,float lenght)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            if (pointsData == null) throw new ArgumentNullException(nameof(pointsData));
            if (mesh2dData.Count==0 || mesh2dData?[0] == null)
            {
                Debug.LogWarning("Mesh2dData is null or not assigned to SimpleMesh");
                return null;
            }
            Debug.Log("Current Scale : "+scale);
            mesh.Clear();
            if(pointsData.Count<1) return null;
            ClearCachedLists();
            GenerateVertices(pointsData,scale,tiling,lenght);
            GenerateTriangles(pointsData.Count);
            mesh.SetVertices(Verts);
            mesh.RecalculateBounds();
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, Uvs);
            mesh.SetTriangles(Triangles, 0);
            return mesh;
        }

        protected override void GenerateVertices(List<PointData> pointsData, Vector2 scale,Vector2 tiling, float lenght)
        {
            var pointsPerVInv = 1 / (float)(pointsData.Count-1);
            for (var i = 0; i < pointsData.Count; i++)
            {
                var splinePoint = pointsData[i];
                var t = i * pointsPerVInv;
                foreach (var vert in mesh2dData[0].vertsData)
                {
                    Verts.Add(splinePoint.LocalToWorldPoint(new Vector2(vert.Point.x*scale.x, vert.Point.y*scale.y)));
                    Normals.Add(splinePoint.LocalToWorldVec(vert.Normal));
                    Uvs.Add(new Vector2(vert.U, t * lenght / tiling.y*VMultiplier));
                }
            }
        
        }

        public void CloseRings(int lastRing)
        {
            if (closedCaps)
            {
                var triangleCount = (IndicesCount / 2) - 2;
                var startRoot = indices[0];
                var endRoot = (lastRing) * IndicesCount + indices[0];
                for (var i = 0; i < triangleCount; i++)
                {
                    var A1 = indices[2 + i * 2];
                    var A2 = indices[2 + i * 2 + 2];
                    Triangles.Add(startRoot);
                    Triangles.Add(A1);
                    Triangles.Add(A2);
                    var B1 = (lastRing) * IndicesCount + indices[2 + i * 2];
                    var B2 = (lastRing) * IndicesCount + indices[2 + i * 2 + 2];
                    Triangles.Add(B2);
                    Triangles.Add(B1);
                    Triangles.Add(endRoot);
                }
            }
        }
        protected override void GenerateTriangles(int frameCount)
        {
            CloseRings(frameCount-1);
            for (int slices = 0; slices < frameCount - 1; slices++)
            {
                var rootA = slices * IndicesCount;
                var rootB = (slices + 1) * IndicesCount;
                for (int line = 0; line < IndicesCount; line += 2)
                {
                    var currentIndex = indices[line];
                    var nextIndex = indices[line + 1];
                    var lineA1 = rootA + currentIndex;
                    var lineA2 = rootA + nextIndex;
                    var lineB1 = rootB + currentIndex;
                    var lineB2 = rootB + nextIndex;
                    // first triangle
                    Triangles.Add(lineA1);
                    Triangles.Add(lineB1);
                    Triangles.Add(lineB2);
                    // second triangle
                    Triangles.Add(lineA1);
                    Triangles.Add(lineB2);
                    Triangles.Add(lineA2);
                }
            }

        }

    }
}