using System;
using System.Collections.Generic;
using THLT.SplineMeshGeneration.Scripts.Commands;
using THLT.SplineMeshGeneration.Scripts.Scriptables;
using Unity.Collections;
using UnityEngine;

namespace THLT.SplineMeshGeneration.Scripts
{
    public interface ISpline
    {
        CommandInvoker SplineCommandInvoker {get; set;}
        List<BezierKnot> Knots { get; }
        List<PointData> Data {get;}
        List<Material> MeshMaterials{get;}
        BaseMesh CustomMesh { get; set; }
        Mesh Mesh { get; set; }
        Vector2 Scale { get; set; }
        Vector2 Tiling { get; set; }
        Spline Root{ get;}
        int Segments{ get; set; }
        float Length { get; set; }
        bool IsSplineClosed { get; }
        bool CanDrawSpline{get;set;}
        bool CanDrawPoints{get;set;}
        float KnotMaxDistance { get; set; }
        float  CastMaxDistance { get; set; }
        public Texture2D HandleTexture {get;set; }

        public Texture2D KnotCenterTexture {get;set;}

        void OnActive();
        void OnDeactive();
        Mesh GenerateMesh();
        void AddMaterial(Material material = null);
        void RemoveMaterial();
        void ChangeMaterial(int index, Material newMat);
        void CreateKnot(Vector2 mousePos);
        void Sample();
        BezierKnot KnotConstructor(string name, Transform parent, Vector3 pointCenter, Texture2D handleIcon, Texture2D knotCenterIcon);
        void UpdateCreatedHandlesPos(Event e);
        void UpdateSelectedHandlesPos(Event e);
        void Undo();
    }
    
}