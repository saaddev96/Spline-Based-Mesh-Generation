using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace THLT.SplineMeshGeneration.Scripts.Scriptables
{
    [CreateAssetMenu(fileName = "New MultiMeshes", menuName = "Scriptable Objects/MultiMeshes")]
    public class MultiMesh : BaseMesh
    {
        
        public override Mesh Generate(Mesh mesh, List<PointData> pointsData, Vector2 scale,Vector2 tiling, float length)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            if (pointsData == null) throw new ArgumentNullException(nameof(pointsData));
            if (mesh2dData.Count==0 || mesh2dData == null)
            {
                Debug.LogWarning("subMeshesData is null or not assigned to MultiMesh");
                return null;
            }
            // Clear Mesh
            mesh.Clear();
            // Clear Cached Lists
            ClearCachedLists();
            GenerateVertices(pointsData,scale,tiling,length);
            mesh.SetVertices(Verts);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, Uvs);
            GenerateTriangles(pointsData.Count, mesh); // Function is assign the triangles to mesh
            return mesh;
        }

        protected override void GenerateVertices(List<PointData> pointsData, Vector2 scale,Vector2 tiling, float length)
        {
           //var circumference = UCircumference(subMeshesData.ToArray());
            for (int i = 0; i < pointsData.Count; i++)
            {
                var point = pointsData[i];
                
                foreach (var subMesh in mesh2dData)
                {
                    var t = i / (float) pointsData.Count; 
                    foreach (var vert in subMesh.vertsData)
                    {
                        Verts.Add(point.LocalToWorldPoint(new Vector2(vert.Point.x*scale.x, vert.Point.y*scale.y)));
                        Normals.Add(point.LocalToWorldVec(vert.Normal));
                        Uvs.Add(new Vector2(vert.U, t*length/tiling.y*VMultiplier));
                    }
                }
            }
        }

        protected override void GenerateTriangles(int frameCount,Mesh mesh)
        {
            mesh.subMeshCount = mesh2dData.Count;
            var subMeshFirstIndex = 0;
            for (var j = 0; j < mesh2dData.Count; j++)
            {
                Triangles.Clear(); // clearing triangle list for the next submesh triangles
                var subMeshData = mesh2dData[j].vertsData;
                for (var slice = 0; slice < frameCount- 1; slice++)
                {
                    var rootA = slice * IndicesCount;  // slice root index
                    
                    var rootB = (slice + 1) * IndicesCount; // neighbor slice root index
                    
                    for (var line = subMeshFirstIndex; line < subMeshFirstIndex+subMeshData.Length; line += 2) // incrementing by 2 indices representing line to next index for next line
                    {
                        // first line
                        
                        var lineA1 = rootA + indices[line];
                        var lineA2 = rootA + indices[line + 1];
                        
                        // neighbor line 
                        
                        var lineB1 = rootB + indices[line];
                        var lineB2 = rootB + indices[line + 1];
                        
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

                mesh.SetTriangles(Triangles, j);
                subMeshFirstIndex += subMeshData.Length; // adding length to track next submesh starting index
            }
        }

    }
}