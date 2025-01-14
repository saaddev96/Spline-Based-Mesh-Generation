using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace THLT.SplineMeshGeneration.Scripts.Scriptables
{
    [System.Serializable]
    public class Mesh2D
    {
        public Vertex[] vertsData;
        
        public static implicit operator GraphMesh2D(Mesh2D mesh2D)
        {
           var graphMesh2D = new GraphMesh2D
           {
               VertsData = mesh2D.vertsData.ToList(),
           };
           return graphMesh2D;
        }

      
    }
}