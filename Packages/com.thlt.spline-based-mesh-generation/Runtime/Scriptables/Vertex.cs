using Unity.Properties;
using UnityEngine;

namespace THLT.SplineMeshGeneration.Scripts.Scriptables
{
    [System.Serializable]
    public struct Vertex
    {
        [DontCreateProperty] [SerializeField]private Vector2 point;
        [DontCreateProperty] [SerializeField]private Vector2 normal;
        [DontCreateProperty] [SerializeField]private float u;
        [DontCreateProperty] [SerializeField]private int index ;
        
        
        public Vector2 Point
        {
            get=>point;
            set=>point=value;
        }
        public Vector2 Normal
        {
            get=>normal;
            set=>normal=value;
        }
        public float U
        {
            get=>u;
            set=>u=value;
        }
        [CreateProperty]
        public int Index
        {
            get => index;
            set => index = value;
        }
        public Vertex(Vector2 point,int index,float u=default, Vector2 normal = default)
        {
            this.point = point;
            this.normal = normal;
            this.u = u;
            this.index = index;
        }

        public Vertex SetPosition(Vector2 pos)
        {
            point = pos;
            return this;
        }
        public Vertex SetNormal(Vector2 newNormal)
        {
            normal = newNormal;
            return this;
        }
        public Vertex SetU(float value)
        {
            u = value;
            return this;
        }
    }
}