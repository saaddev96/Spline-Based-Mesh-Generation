using System;
using System.Collections.Generic;
using THLT.SplineMeshGeneration.Scripts.Commands;
using THLT.SplineMeshGeneration.Scripts.Scriptables;
using Unity.Collections;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace THLT.SplineMeshGeneration.Scripts
{
    [Serializable]
    public abstract class BaseSpline : ISpline
    {
        // Serialized Private Fields

        [SerializeField] protected Spline root;
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
        // Private Fields

        // Max Distance between two Knots to merge
        public float KnotMaxDistance { get; set; } = 1f;
        public float CastMaxDistance { get; set; } = 100f;

        [FormerlySerializedAs("_meshFilter")] [SerializeField] [HideInInspector]
        protected MeshFilter meshFilter;

        [FormerlySerializedAs("_meshRenderer")] [SerializeField] [HideInInspector]
        protected MeshRenderer meshRenderer;

        [FormerlySerializedAs("_customMesh")] [SerializeField] [HideInInspector]
        protected BaseMesh customMesh;

        [FormerlySerializedAs("_knots")] [SerializeField] [HideInInspector]
        protected List<BezierKnot> knots = new();

        [FormerlySerializedAs("_data")] [SerializeField] [HideInInspector]
        protected List<PointData> data = new();

        [FormerlySerializedAs("_meshMaterials")] [SerializeField] [HideInInspector]
        protected List<Material> meshMaterials = new();

        // public properties

        [CreateProperty]
        public Vector2 Scale
        {
            get => scale;
            set
            {
                EditorUtility.SetDirty(root);
                scale = value.magnitude <= 0 ? new Vector2(0.01f, 0.01f) : value;
                Sample();
            }
        }

        [CreateProperty]
        public Vector2 Tiling
        {
            get => tiling;
            set
            {
                EditorUtility.SetDirty(root);
                tiling = value;
                Sample();
            }
        }

        public float Length { get; set; }

        public Spline Root
        {
            get => root;
            set => root = value;
        }

        [CreateProperty] public bool CanDrawSpline { get; set; } = true;
        [CreateProperty] public bool CanDrawPoints { get; set; } = false;

        [CreateProperty]
        public int Segments
        {
            get => segments;
            set
            {
                EditorUtility.SetDirty(root);
                segments = value <= 0 ? 2 : value;
                Sample();
            }
        }

        // checking if the spline loop is closed by comparing first knot with the last
        public bool IsSplineClosed
        {
            get
            {
                if (Knots.Count > 1)
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
                EditorUtility.SetDirty(root);
                customMesh = value ?? throw new NullReferenceException();
                Sample();
            }
        }

        public Mesh Mesh
        {
            get => mesh;
            set => mesh = value;
        }

        public MeshCollider SplineMeshCollider { get; set; }
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
        public abstract void Sample();
        public abstract void UpdateMeshCollider();

        public abstract BezierKnot KnotConstructor(string name, Transform parent, Vector3 pointCenter,
            Texture2D handleIcon, Texture2D knotCenterIcon);

        public abstract void UpdateCreatedHandlesPos(Event e);
        public abstract void UpdateSelectedHandlesPos(Event e);
        public abstract void Undo();
        public abstract void OnDestroy();
    }
}