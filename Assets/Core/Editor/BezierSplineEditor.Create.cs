using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace THLT.SplineMeshGeneration.Scripts.Editor
{
    public partial class BezierSplineEditor
    {
        private static List<Spline> Splines => SplinesData.Splines;

        private static Spline CurrentSpline => SplinesData.ActiveSpline;
        private readonly Color _activeColor = new (0.20f, 0.77f, 0.92f,0.25f);
        private readonly Color _inactiveColor = new (0, 0, 0,0.25f);
        private void AddSpline() 
        {
           
            var newSpline = CreateSpline();
            Splines.Add(newSpline);
            SplinesData.ChangeActiveSpline(newSpline);
            var index = Splines.Count - 1;
            _splineTreeView.AddItem(new TreeViewItemData<Spline>(index, newSpline));
          
        }

        private Spline CreateSpline()
        {
            var nameTag = "Spline " + Splines.Count; 
            var newSplineObj = new GameObject(nameTag)
            {
                hideFlags = HideFlags.HideInHierarchy,
                transform =
                {
                    position = Vector3.zero
                }
            };
            newSplineObj.AddComponent<MeshRenderer>();
            newSplineObj.AddComponent<MeshFilter>();
            EditorUtility.SetDirty(newSplineObj);
            var newSpline = newSplineObj.AddComponent<Spline>(); 
            newSpline.MSpline = new BezierSpline() 
            {
                Root = newSpline
            };
            return newSpline;
        }

        private void PopulateTreeView()
        {
          
            var treeRoot = SplineTreeRoot;
            if (treeRoot == null)
            {
                return;
            }

            _splineTreeView.SetRootItems(treeRoot);
            _splineTreeView.bindItem = (e, id) =>
            {
                var targetSpline = _splineTreeView.GetItemDataForId<Spline>(id).MSpline; 
                e.dataSource = targetSpline;
                e.userData = id;
                var root = e.Q<VisualElement>("root");
                if (targetSpline == null)
                {
                    PrintError("No target spline found");
                    return;
                }
                var foldout = e.Q<Foldout>("ExtraData"); 
                if (foldout == null)
                {
                    PrintError("No label found");
                    return;
                }
                foldout.text = $"Spline : {id}";
                if (targetSpline.Root == CurrentSpline)
                {
                    HighlightSelectedSpline(root); 
                }
                var selectBtn = e.Q<Button>("Select");
                if (selectBtn == null)
                {
                    PrintError("No select button found");
                    return;
                }
                selectBtn.userData = id;
                var removeBtn = e.Q<Button>("Remove");
                if (removeBtn == null)
                {
                    PrintError("No remove button found");
                    return;
                }
                removeBtn.userData= id;
                var segments = e.Q<SliderInt>("Slider");
                if (segments == null)
                {
                    PrintError("Segment's Slider not found");
                    return;
                }
                segments.SetBinding(nameof(SliderInt.value), new DataBinding
                {
                    dataSourcePath = new PropertyPath(nameof(BezierSpline.Segments)),
                    bindingMode = BindingMode.TwoWay
                });
                var canDrawSpline = e.Q<Toggle>("DrawSpline");
                if (canDrawSpline == null)
                {
                    PrintError("No Draw spline Toggle found");
                    return;
                }
                canDrawSpline.SetBinding(nameof(Toggle.value), new DataBinding
                {
                    
                    dataSourcePath = new PropertyPath(nameof(BezierSpline.CanDrawSpline)),
                    bindingMode = BindingMode.TwoWay
                });
                
                var canDrawPoints = e.Q<Toggle>("DrawPoints");
                if (canDrawPoints == null)
                {
                    PrintError("No Draw Points Toggle found");
                    return;
                }
                canDrawPoints.SetBinding(nameof(Toggle.value), new DataBinding
                {
                    
                    dataSourcePath = new PropertyPath(nameof(BezierSpline.CanDrawPoints)),
                    bindingMode = BindingMode.TwoWay
                });

                
                var mesh2DField = e.Q<ObjectField>("Mesh2DField");
                if (mesh2DField == null)
                {
                    PrintError("No mesh 2D field found"); 
                    return;
                } 
                mesh2DField.SetBinding(nameof(ObjectField.value), new DataBinding
                {
                    dataSourcePath = new PropertyPath(nameof(BezierSpline.CustomMesh)), 
                    bindingMode = BindingMode.TwoWay
                });
                
                var meshScaleField = e.Q<Vector2Field>("MeshScale");
                if (meshScaleField == null)
                {
                    PrintError("mesh Scale  not found");
                    return;
                }
                var tilingField = e.Q<Vector2Field>("Tiling");
                
                if (tilingField == null)
                {
                    PrintError("tiling field not found");
                    return;
                }
                if (mesh2DField.value is null)
                {
                    meshScaleField.AddToClassList("Hidden");
                    tilingField.AddToClassList("Hidden");
                 
                }
                meshScaleField.SetBinding(nameof(Vector2Field.value), new DataBinding
                {
                    dataSourcePath = new PropertyPath(nameof(BezierSpline.Scale)), 
                    bindingMode = BindingMode.TwoWay
                        
                });
                tilingField.SetBinding(nameof(Vector2Field.value), new DataBinding
                {
                    dataSourcePath = new PropertyPath(nameof(BezierSpline.Tiling)), 
                    bindingMode = BindingMode.TwoWay
                        
                });
                var meshMaterials = e.Q<ListView>("Materials");
                if (meshMaterials == null)
                {
                    PrintError("meshMaterials listview not found");
                    return;
                }
                EventCallback<ChangeEvent<Object>> materialValueChanged = null;
                meshMaterials.itemsSource = targetSpline.MeshMaterials;
                meshMaterials.makeItem = () => new ObjectField
                {
                    objectType = typeof(Material)
                    
                };
                meshMaterials.bindItem = (v, i) =>
                {
                    if (v is not ObjectField objectField) return;
                    objectField.userData = i;
                    objectField.value = meshMaterials.itemsSource[i] as Material;
                    materialValueChanged = (evt) =>
                    {
                        var index = (int)objectField.userData;
                        targetSpline.ChangeMaterial(index,evt.newValue as Material);

                    };
                    objectField.RegisterValueChangedCallback(materialValueChanged);
                };
                
                meshMaterials.unbindItem = (v, _) =>
                {
                    if (v is not ObjectField objectField) return;
                    objectField.Unbind();
                    objectField.UnregisterValueChangedCallback(materialValueChanged);
                };
                meshMaterials.onAdd = (b) =>  
                {
                    targetSpline.AddMaterial();
                    b.Rebuild();
                };
                meshMaterials.onRemove = (b) =>
                {
                    targetSpline.RemoveMaterial();
                    b.Rebuild();
                };
                var bakeBtn =e.Q<Button>("Bake");
                if (bakeBtn is null)
                {
                    PrintError("bake button element not found");
                    return;
                }
                bakeBtn.userData = id;
                selectBtn.RegisterCallback<ClickEvent>(SelectSpline);
                void SelectSpline(ClickEvent _)
                {
                    var btnIndex = (int)selectBtn.userData;
                    SplinesData.ChangeActiveSpline(Splines[btnIndex]);
                    HighlightSelectedSpline(root);
                }
                removeBtn.RegisterCallback<ClickEvent>(_ => RemoveSpline((int)removeBtn.userData));
                bakeBtn.RegisterCallback<ClickEvent>(_=>Bake((int)bakeBtn.userData));
                mesh2DField.RegisterValueChangedCallback(m =>
                {
                    if (m.newValue is null) return;
                    meshScaleField.RemoveFromClassList("Hidden");
                    tilingField.RemoveFromClassList("Hidden");
                });
             
            };
        }

        private void HighlightSelectedSpline(VisualElement visualElement)
        {
            var elm = _splineTreeView.GetRootElementForIndex(SplinesData.PreviousSplineIndex)?.Q<VisualElement>("root");
            if (elm != null) elm.style.backgroundColor = new StyleColor(_inactiveColor);
            visualElement.style.backgroundColor = new StyleColor(_activeColor);
        }
        private  void UndoKnot()
        {
            CurrentSpline?.Undo();
        }

        private void RemoveSpline(int id)
        {
            var item = Splines[id];
            var isActiveSpline =  SplinesData.ActiveSpline == Splines[id];
            Splines.Remove(item);
            if (Splines.Count > 0)
            {
                if (isActiveSpline)
                    SplinesData.ChangeActiveSpline(Splines[0]);
            }
            else
            {
                SplinesData.ChangeActiveSpline(null);
            }
            EditorUtility.SetDirty(item.gameObject);
            DestroyImmediate(item.gameObject);
            RefreshTreeView();
        }

        private  void Bake(int splineId)
        {
            if (splineId >= SplinesData.Splines.Count) return;
            var targetSpline = SplinesData.Splines[splineId];
            if (targetSpline is { MSpline: null }) return;
            var mesh = targetSpline.MSpline.Mesh;
            if (mesh is { vertexCount: 0 }) return;
            SplinesData.ChangeActiveSpline(null);
            var meshObject = targetSpline.gameObject;
           // meshObject.AddComponent<MeshCollider>().sharedMesh = mesh;
            foreach (var k in targetSpline.MSpline.Knots)
            {
                    k.DestroyObjects();
            }
            SplinesData.Splines.RemoveAt(splineId);
            DestroyImmediate(targetSpline);
            meshObject.hideFlags = HideFlags.None;
            RefreshTreeView();
        }
        private static IList<TreeViewItemData<Spline>> SplineTreeRoot
        {
            get
            {
                var treeViewRoot = new List<TreeViewItemData<Spline>>();
                for (var index = 0; index < Splines.Count; index++)
                {
                    var spline = Splines[index];
                    treeViewRoot.Add(new TreeViewItemData<Spline>(index, spline));
                }

                return treeViewRoot;
            }
        }

        private void RefreshTreeView()
        {
            _splineTreeView.SetRootItems(SplineTreeRoot);
            _splineTreeView.Rebuild();
        }
    }
}