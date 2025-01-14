using System;
using UnityEngine;

namespace THLT.SplineMeshGeneration.Scripts
{
    public class Spline : MonoBehaviour
    {
        [SerializeReference] private BaseSpline spline;

        public BaseSpline MSpline
        {
            get => spline;
            set => spline = value ?? new BezierSpline() { Root = this }; 
        }
        
        public void OnActive() => spline?.OnActive();

        public void OnDeactive()=>spline?.OnDeactive();

        public Mesh GenerateMesh()=>spline?.GenerateMesh();

        public void AddMaterial(Material material = null)=>spline?.AddMaterial(material);

        public void RemoveMaterial()=>spline?.RemoveMaterial();

        public void ChangeMaterial(int index, Material newMat)=>spline?.ChangeMaterial(index, newMat);

        public void CreateKnot(Vector2  mousePos)=>spline?.CreateKnot(mousePos);
        public void Sample()=>spline?.Sample();
        public void UpdateCreatedHandlesPos(Event e)=>spline?.UpdateCreatedHandlesPos(e);

        public void UpdateSelectedHandlesPos(Event e)=>spline?.UpdateSelectedHandlesPos(e);

        public void Undo()=>spline?.Undo();

        public void OnDestroy() => spline?.OnDestroy();
        
    }

}