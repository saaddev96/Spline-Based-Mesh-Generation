using System.Collections.Generic;
using UnityEngine;

namespace THLT.SplineMeshGeneration.Scripts.Scriptables
{
    public  abstract class BaseMesh : ScriptableObject
    {

        public int[] indices;
        public List<Mesh2D> mesh2dData = new List<Mesh2D>();
        protected readonly List<Vector3> Verts = new();
        protected readonly List<Vector3> Normals = new();
        protected readonly List<Vector2> Uvs = new();
        protected readonly List<int> Triangles = new();
        protected int IndicesCount => indices.Length;
        protected const int VMultiplier = 10;
        public abstract Mesh Generate(Mesh mesh, List<PointData> pointsData, Vector2 scale,Vector2 tiling, float lenght);
        protected abstract void GenerateVertices(List<PointData> pointsData, Vector2 scale,Vector2 tiling, float lenght);
        protected virtual void GenerateTriangles(int frameCount){}
        protected virtual void GenerateTriangles(int frameCount, Mesh mesh){}
        public void ClearCachedLists()
        {
            Verts?.Clear();
            Normals?.Clear();
            Uvs?.Clear();
            Triangles?.Clear();
        }
        private void OnDisable() 
        {
            ClearCachedLists();
        }
    }
}