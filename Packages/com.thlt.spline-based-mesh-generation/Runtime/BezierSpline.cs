using UnityEngine;
using System;
using System.Linq;
using THLT.SplineMeshGeneration.Scripts.Commands;
using UnityEditor;
using Object = UnityEngine.Object;

namespace THLT.SplineMeshGeneration.Scripts
{
    [Serializable]
    public class BezierSpline : BaseSpline
    {

        public override void OnActive()
        {
            Selection.selectionChanged += HandlerSelected;
            handleTexture ??=
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/THLT/SplineMeshGeneration/UI/Handle.png");
            knotCenterTexture ??=
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/THLT/SplineMeshGeneration/UI/knotCenter.png");
            SetHandleIcons();
            InitSetup();
            GenerateMesh();
            SplineCommandInvoker ??= new CommandInvoker();
        }

        public override void OnDeactive()
        {
            Selection.selectionChanged -= HandlerSelected;
            CustomMesh?.ClearCachedLists();
            RemoveHandleIcons();
        }

        private void InitSetup()
        {
            if (mesh == null)
            {
                mesh = new Mesh
                {
                    name = "Spline Mesh"
                };
                mesh.MarkDynamic();
            }

            if (meshRenderer == null)
            {
                Print("MeshRenderer Called");
                meshRenderer = root.gameObject.GetComponent<MeshRenderer>();
            }

            if (meshFilter == null)
            {
                meshFilter = root.gameObject.GetComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;
            }
        }

        private void SetHandleIcons()
        {
            foreach (var knot in Knots)
            {
                knot.SetIcon(handleTexture, knotCenterTexture);
            }
        }

        private void RemoveHandleIcons()
        {
            foreach (var knot in Knots)
            {
                knot.RemoveIcons();
            }
        }

        public override Mesh GenerateMesh()
        {
            if (mesh == null)
            {
                Debug.LogError(" Mesh  returns null or is is not instantiated");
                return null;
            }
            if (meshRenderer == null)
            {
                Debug.LogError(" Mesh Renderer no found");
                return null;
            }
            if (meshFilter == null)
            {
                Debug.LogError(" Mesh Filter no found");
                return null;
            }
            if (CustomMesh == null)
            {
                Debug.LogWarning(" Custom 2D Mesh not assigned or it is null");
                return null;
            }
            return CustomMesh?.Generate(mesh, Data, scale, tiling,Length);
        }

        public override  void AddMaterial(Material material = null)
        {
            meshMaterials.Add(material);
            UpdateMaterials();
        }

        public override  void RemoveMaterial()
        {
            if (!meshMaterials.Any()) return;
            var index = meshMaterials.Count() - 1;
            meshMaterials.RemoveAt(index);
            UpdateMaterials();
        }

        public override  void ChangeMaterial(int index, Material newMat)
        {
            if (newMat == null) throw new ArgumentNullException(nameof(newMat));
            if (index < 0 || index >= meshMaterials.Count())
                throw new ArgumentOutOfRangeException();
            meshMaterials[index] = newMat;
            UpdateMaterials();
        }

        private void UpdateMaterials()
        {
            if (meshRenderer == null) return;
            EditorUtility.SetDirty(meshRenderer);
            meshRenderer.SetSharedMaterials(meshMaterials);
        }

        public override  void CreateKnot(Vector2 mousePos)
        {
            var createCommand = new CreateKnotCommand(this, mousePos);
            splineCommandInvoker.ExecuteCommand(createCommand);
        }

        public override BezierKnot KnotConstructor(string name, Transform parent, Vector3 pointCenter, Texture2D handleIcon, Texture2D knotCenterIcon)
        {
            return BezierKnot
                .Create(name)
                .SetParent(parent)
                .SetPosition(pointCenter, pointCenter, pointCenter)
                .SetIcon(handleTexture, knotCenterTexture);
         
        }
        public override  void UpdateCreatedHandlesPos(Event e)
        {
            if(e is null) throw new NullReferenceException("input Event is null");
            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;
            var knt = Knots[^1];
            Selection.activeTransform = knt.rightHandle;
            knt.rightHandle.position = hit.point;
            BezierSplineToughDataSimpling.Sample(this);
            GenerateMesh();
        }

        public override void UpdateSelectedHandlesPos(Event e)
        {
            if (selectedHandle is null) return;
            if (SelectedCurrentPos is null && SelectedCurrentRot is null) return;
            if (SelectedCurrentPos == selectedHandle.position && SelectedCurrentRot == selectedHandle.rotation) return;
            if (!e.alt)
            {
                if (selectedHandle.gameObject.name == selectedBezierKnot.leftHandle.name)
                {
                    selectedBezierKnot.SetHandlesPosition(selectedHandle.localPosition,
                        -selectedHandle.localPosition);
                }
                else if (selectedHandle.gameObject.name == selectedBezierKnot.rightHandle.name)
                {
                    selectedBezierKnot.SetHandlesPosition(-selectedHandle.localPosition,
                        selectedHandle.localPosition);
                }
            }
            BezierSplineToughDataSimpling.Sample(this);
            GenerateMesh();
            SelectedCurrentPos = selectedHandle.position;
            SelectedCurrentRot = selectedHandle.rotation;


        }

        private void HandlerSelected()
        {
            selectedBezierKnot = Knots.Find((k) => k.Contains(Selection.activeTransform));
            if (selectedBezierKnot != null)
            {
                selectedHandle = Selection.activeTransform;
                SelectedCurrentPos = selectedHandle.position;
                SelectedCurrentRot = selectedHandle.rotation;
            }
            else
            {
                SelectedCurrentPos = null;
                SelectedCurrentRot = null;
                selectedHandle = null;
                selectedBezierKnot = null;
                
            }
        }

        public override  void Undo()
        {
            SplineCommandInvoker.UndoCommand();
        }

        public override void OnDestroy()
        {
            Selection.selectionChanged -= HandlerSelected;
        }

  
    }
}