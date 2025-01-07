using System;
using System.Collections.Generic;
using THLT.SplineMeshGeneration.Scripts.Commands;
using THLT.SplineMeshGeneration.Scripts.Scriptables;
using Unity.Collections;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Serialization;

namespace THLT.SplineMeshGeneration.Scripts
{
    [Serializable]
    public abstract class BaseSpline : ISpline
    {
      // Serialized Private Fields

        [SerializeField]  protected Spline root;
        [SerializeField] [HideInInspector] protected Texture2D handleTexture;

     
        [SerializeField] [HideInInspector] protected Texture2D knotCenterTexture;
        [SerializeField] [HideInInspector] protected Transform selectedHandle;
        [SerializeField] [HideInInspector] protected BezierKnot selectedBezierKnot; 
        [SerializeField] [HideInInspector] protected Mesh mesh;
        [SerializeField] [HideInInspector] protected int segments = 58;
        [SerializeField] [HideInInspector] protected Vector2 scale = Vector2.one;
        [SerializeField] [HideInInspector] protected Vector2 tiling = Vector2.one;
        [SerializeField] [HideInInspector] protected float radius = 0.5f;
        [SerializeField] [HideInInspector] protected CommandInvoker splineCommandInvoker;
        
        protected Vector3? SelectedCurrentPos;
        protected Quaternion? SelectedCurrentRot;
        
        protected const string HandleTexturePath= "Packages/com.thlt.spline-based-mesh-generation/UI/Handle.png";
        protected const string KnotCenterTexturePath= "Packages/com.thlt.spline-based-mesh-generation/UI/knotCenter.png";
     
        // Max Distance between two Knots to merge
        public  float KnotMaxDistance { get; set; }= 1f; 

        [FormerlySerializedAs("_meshFilter")] [SerializeField]
        [HideInInspector] protected MeshFilter meshFilter;

        [FormerlySerializedAs("_meshRenderer")] [SerializeField]
        [HideInInspector] protected MeshRenderer meshRenderer;

        [FormerlySerializedAs("_customMesh")] [SerializeField]
        [HideInInspector] protected BaseMesh customMesh;

        [FormerlySerializedAs("_knots")] [SerializeField]
        [HideInInspector] protected List<BezierKnot> knots = new();

        [FormerlySerializedAs("_data")] [SerializeField]
        [HideInInspector] protected List<PointData> data = new();

        [FormerlySerializedAs("_meshMaterials")] [SerializeField]
        [HideInInspector] protected List<Material> meshMaterials = new();

        // public properties

        [CreateProperty]
        public Vector2 Scale
        {
            get => scale;
            set
            {
                scale = value.magnitude<=0? new Vector2(0.01f,0.01f) : value;
                GenerateMesh();
            }
        }
        [CreateProperty]
        public Vector2 Tiling
        {
            get => tiling;
            set
            {
                tiling = value;
                GenerateMesh();
            }
        }

        public float Length { get; set;}
        public Spline Root
        {
            get => root;
            set => root = value;
        }

        [CreateProperty] public bool CanDrawGizmos { get; set; } = true;

        [CreateProperty]
        public int Segments
        {
            get => segments;
            set
            {
                 segments = value <= 0 ? 2 : value;
                 BezierSplineToughDataSimpling.Sample(this);
                 GenerateMesh();
            }
        } 
        // checking if the spline loop is closed by comparing first knot with the last
        public bool IsSplineClosed 
        {
            get
            {
                if(Knots.Count>1) 
                    return Knots[0] == Knots[^1];
                return false;
            }
        }
        
        public CommandInvoker SplineCommandInvoker
        {
            get => splineCommandInvoker;
            set => splineCommandInvoker = value;
        }
        public List<BezierKnot> Knots => knots;

        public List<PointData> Data => data;
        
        [CreateProperty]
        public BaseMesh CustomMesh
        {
            get => customMesh;
            set
            {
                 customMesh = value ?? throw new NullReferenceException();
                 GenerateMesh();
            }
        }

        public Mesh Mesh
        {
            get=>mesh;
            set=>mesh = value;
        }
        public List<Material> MeshMaterials => meshMaterials;

        public Texture2D HandleTexture
        {
            get => handleTexture;
            set => handleTexture = value;
        }

        public Texture2D KnotCenterTexture
        {
            get => knotCenterTexture;
            set => knotCenterTexture = value;
        }

        public void Print(object msg) => Debug.Log(msg);
         
        public abstract void OnActive();
        public abstract void OnDeactive();
        public abstract Mesh GenerateMesh();
        public abstract void AddMaterial(Material material = null);
        public abstract void RemoveMaterial();
        public abstract void ChangeMaterial(int index, Material newMat);
        public abstract void CreateKnot(Vector2 mousePos);
        public abstract BezierKnot KnotConstructor(string name, Transform parent, Vector3 pointCenter, Texture2D handleIcon,Texture2D knotCenterIcon);
        public abstract void UpdateCreatedHandlesPos(Event e);
        public abstract void UpdateSelectedHandlesPos(Event e);
        public abstract void Undo();
        public abstract void OnDestroy();

    }
}
