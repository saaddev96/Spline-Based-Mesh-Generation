using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using THLT.SplineMeshGeneration.Scripts.Scriptables;
using THLT.SplineMeshGeneration.Scripts.VisualElements;
using Unity.Properties;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Linq;
using GraphView = THLT.SplineMeshGeneration.Scripts.VisualElements.GraphView;
using Vertex = THLT.SplineMeshGeneration.Scripts.Scriptables.Vertex;

namespace THLT.SplineMeshGeneration.Scripts.Editor
{
    public class Mesh2DDrawerEditor :EditorWindow
    {
       
        [SerializeField] private VisualTreeAsset graphInterfaceTree;
        // Elements
        private VisualElement _root;
        private GraphView _graphView;
        private TabView _tabView;
        private Button _createButton;
        private Tab _currentTab;
        private DropdownField _shapesDropDown;
        private ListView _shapesListView;
        private ToggleButtonGroup _drawingModeGroupButton;
        private Button _addShapeButton;
        private Foldout _shapesFoldout;
        private Label _warningLabel;
        private Vector2Field _selectedVertexPositionField;
        private Vector2Field _selectedVertexNormalField;
        private FloatField _selectedVertexUField;
        private Label _selectedVertexLabel;
        private GraphViewController _graphViewController;
        private ToolbarButton _undoButton;
        private ToolbarButton _removeVertexButton;
        private TextField _shapeNameTextField;
        private VisualElement _selectedShape;
        
        private readonly Color _activeColor = new (0.20f, 0.77f, 0.92f,0.25f);
        private readonly Color _inactiveColor = new (0, 0, 0,0.25f);
        
        private const string WARNING_ESCAPE_SEQUENCE = "\u26a0\ufe0f";
        private const string Mesh2DPath= "Assets/2DMeshes";
        private  string Created2DMeshPath;
        
        [MenuItem("THLT/Spline-based Mesh Generation/2D Mesh Drawer")]
        public static void Create()
        {
          var win =  GetWindow<Mesh2DDrawerEditor>("2D Mesh Drawer");
          win.Focus();
        }
        private void CreateGUI()
        {
            Initialize();  
        }


        private void Initialize()
        {
            _root = rootVisualElement;
            _root.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            graphInterfaceTree?.CloneTree(_root);
            PrefetchElements();
            BindElements();
            HandleCurrentTabElements(_tabView.activeTab);
          
            _graphViewController.OnShapesDataChanged = () =>
            {
                _shapesListView.Rebuild();
            };
            _graphViewController.OnSelectedVertDataUpdated = () =>
            {
                UpdateSelectVertElementData( _graphViewController.SelectedVertex.Point, _graphViewController.SelectedVertex.Normal, _graphViewController.SelectedVertex.U,_graphViewController.SelectedVertex.Index);
            };
            _graphViewController.OnVerticesCleared = () =>
            {
                _graphViewController.ResetSelectedVertex();
                UpdateSelectVertElementData();
            };
            _addShapeButton.clicked += () =>
            {
                _graphViewController.ShapeFoldoutToggleValue = true;
               _graphViewController.AddNewShape();
               HandleAddButton();
                _shapesListView.Rebuild();
            };
            _createButton.clicked += CreateShape;
            _shapesListView.selectionChanged += _ =>
            {
                ShapeChanged();
            };
            _graphViewController.OnShapeRemoved =()=>
            {
                MultiShapeWarningCheck();
                HandleAddButton();
            };
            _undoButton.clicked += () =>
            {
                _graphViewController.CurrentShape?.ShapeCommandInvoker.UndoCommand();
                _shapesListView.Rebuild();
            };
            _removeVertexButton.clicked += () =>
            {
                _graphViewController.ClearShape(_graphViewController.CurrentShape);
            };
            _tabView.activeTabChanged += InspectorTabChanged;
            _graphViewController.OnSelectionChanged += UpdateSelectedVertex;
            HandleAddButton();
        }
        private void PrefetchElements()
        {
            _graphView = _root.Q<GraphView>("GraphView");
            if((_graphView == null)) throw new NullReferenceException("No graph is found");
            _graphViewController = _graphView.GraphViewController;
            
            _tabView = _root.Q<TabView>("TabView");
            if((_tabView == null)) throw new NullReferenceException("TabView");

            _createButton = _root.Q<Button>("Create");
            if((_createButton == null)) throw new NullReferenceException("Create");

            _shapesDropDown = _root.Q<DropdownField>("ShapeType"); 
            if((_shapesDropDown == null)) throw new NullReferenceException(nameof(_shapesDropDown));
            
            _shapesListView = _root.Q<ListView>("ShapesList");
            if((_shapesListView == null)) throw new NullReferenceException(nameof(_shapesListView));
            
            _drawingModeGroupButton = _root.Q<ToggleButtonGroup>("DrawingModeBtn");
            if((_drawingModeGroupButton == null)) throw new NullReferenceException(nameof(_drawingModeGroupButton));
            
            _addShapeButton = _root.Q<Button>("AddShape");
            if((_addShapeButton == null)) throw new NullReferenceException(nameof(_addShapeButton));
            
            _shapesFoldout = _root.Q<Foldout>("ShapesFoldout");
            if((_shapesFoldout == null)) throw new NullReferenceException(nameof(_shapesFoldout));
            
            _warningLabel = _root.Q<Label>("Warning");
            if((_warningLabel == null)) throw new NullReferenceException(nameof(_warningLabel)); 
            
            _selectedVertexLabel = _root.Q<Label>("SelectedVertexLabel");
            if((_selectedVertexLabel == null)) throw new NullReferenceException(nameof(_selectedVertexLabel));
            
            _selectedVertexPositionField = _root.Q<Vector2Field>("PositionVector");
            if(_selectedVertexPositionField == null) throw new NullReferenceException(nameof(_selectedVertexPositionField));

            _selectedVertexNormalField = _root.Q<Vector2Field>("NormalVector");
            if(_selectedVertexNormalField == null) throw new NullReferenceException(nameof(_selectedVertexNormalField));
            
            _selectedVertexUField = _root.Q<FloatField>("UField");
            if(_selectedVertexUField == null) throw new NullReferenceException(nameof(_selectedVertexUField)); 
            
            _undoButton = _root.Q<ToolbarButton>("Undo");
            if(_undoButton == null) throw new NullReferenceException(nameof(_undoButton)); 
            
            _removeVertexButton  = _root.Q<ToolbarButton>("RemoveVertex");
            if(_removeVertexButton == null) throw new NullReferenceException(nameof(_removeVertexButton)); 
            
            _shapeNameTextField = _root.Q<TextField>("ShapeName");
            if(_shapeNameTextField == null) throw new NullReferenceException(nameof(_shapeNameTextField)); 
            
        }

        void ShapeChanged()
        {
            if (_shapesListView.selectedIndex == -1) return;
            var selectedIndex = _shapesListView.selectedIndex;
            if(_graphViewController.Shapes.Count<=selectedIndex) throw new ArgumentOutOfRangeException($"index {selectedIndex} is out of range");
            _graphViewController.ChangeCurrentShape(selectedIndex);
            HighlightSelectedShape();
        }

        void HighlightSelectedShape()
        {
            var shapeIndex = _graphViewController.Shapes.IndexOf(_graphViewController.CurrentShape);
            if (_selectedShape != null)
            {
                _selectedShape.style.backgroundColor = _inactiveColor;
            }
            _selectedShape = _shapesListView.GetRootElementForIndex(shapeIndex)?.Q<VisualElement>("root");
            if (_selectedShape != null) _selectedShape.style.backgroundColor = _activeColor;
        }
        void UpdateSelectVertElementData(Vector2 pos = default,Vector2 normal= default,float u= default,int index =-1)
        {
            _selectedVertexLabel.text = index== -1 ?" Selected Vertex : NaN " :  $" Selected Vertex : {index} ";
            _selectedVertexPositionField.SetValueWithoutNotify(pos);
            _selectedVertexNormalField.SetValueWithoutNotify(normal);
            _selectedVertexUField.SetValueWithoutNotify(u);
        }
        void GenericShapeCreator<T>() where T : BaseMesh
        {
            Created2DMeshPath = Path.Combine(Application.dataPath,"2DMeshes");
            if (!Directory.Exists(Created2DMeshPath))
            {
                Directory.CreateDirectory(Created2DMeshPath);
                AssetDatabase.Refresh();
            }
            var mesh2d = CreateInstance<T>();
            var indices = new int[]{};
            var previousShapeVertCount = 0;
            foreach (var shape in _graphViewController.Shapes) 
            {
                if(shape.VertsData.Count<=0) return;
                var array = new Vertex[shape.VertsData.Count];
                shape.VertsData.CopyTo(array);
                var shape2dCopy = new GraphMesh2D
                {
                    VertsData = array.ToList(),
                }; 
                var verts = shape2dCopy.VertsData;
                int[] shapeIndices;
                if (verts[0].Point == verts[^1].Point)
                {
                    verts.RemoveAt(verts.Count-1);
                    verts.RemoveAt(verts.Count-1);
                    shapeIndices = new int[verts.Count];
                    shapeIndices[0] = previousShapeVertCount+(verts.Count - 1);
                    for (var i = 1; i < verts.Count; i++)
                    {
                        shapeIndices[i] = previousShapeVertCount+(i-1);
                    } 
                }
                else
                {
                    verts.RemoveAt(0);
                    verts.RemoveAt(verts.Count-1);
                    shapeIndices = new int[verts.Count];
                    for (var i = 0; i < verts.Count; i++)
                    {
                        shapeIndices[i] = previousShapeVertCount+i;
                    } 
                }
                previousShapeVertCount += verts.Count;
                mesh2d.mesh2dData.Add(shape2dCopy);
                indices = indices.Concat(shapeIndices).ToArray();
            }
            mesh2d.indices = indices;
            var guids2 = AssetDatabase.FindAssets($"{_shapeNameTextField.value}", new[] {Mesh2DPath});
            var path = $"{Mesh2DPath}/{_shapeNameTextField.value}-{guids2.Length}.asset";
            
            AssetDatabase.CreateAsset(mesh2d,path);
            AssetDatabase.Refresh();
        }
        void CreateShape()
        {
            switch (_graphViewController.SelectedShapeType)
            {
                case ShapeTypes.Single:
                    GenericShapeCreator<SimpleMesh>();
                    break;
                case ShapeTypes.Multi:
                    GenericShapeCreator<MultiMesh>();
                    break;
                case ShapeTypes.Spiral:
                    GenericShapeCreator<SpiralMesh>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        void HandleAddButton()
        {
            var addButtonStyle = _addShapeButton.style;
            switch (_graphViewController.SelectedShapeType)
            {
                case  ShapeTypes.Single or ShapeTypes.Spiral when _graphViewController.CurrentShape == null:
                    addButtonStyle.display = DisplayStyle.Flex;
                    return;
                case ShapeTypes.Single or ShapeTypes.Spiral:
                    addButtonStyle.display = DisplayStyle.None;
                    break;
                case ShapeTypes.Multi:
                    addButtonStyle.display = DisplayStyle.Flex;
                    break;
            }
        }

        void ShowWarning(string warningText)
        {
            if (string.IsNullOrEmpty(warningText))
            {
                _warningLabel.text = "";
                return;
            }

            _warningLabel.text = $"{WARNING_ESCAPE_SEQUENCE} {warningText}";
        }
        private void OnDisable()
        {
            _tabView.activeTabChanged -= InspectorTabChanged;
            _graphViewController.OnSelectionChanged -= UpdateSelectedVertex;
        }

        private void InspectorTabChanged(Tab previousTab, Tab newTab)
        {
            _currentTab = newTab;
            HandleCurrentTabElements(_currentTab);
        }

        private void HandleCurrentTabElements(Tab currentTab)
        {
            switch (currentTab.tabIndex)
            {
                case 0:
                    _graphViewController.CurrentInspectorModes = InspectorModes.Draw;
                    break;
                case 1:
                    _graphViewController.CurrentInspectorModes = InspectorModes.Edit;
                    break;
            }
        }
        
        void BindElements()
        {
            BindField<Foldout, bool>(_shapesFoldout, nameof(GraphViewController.ShapeFoldoutToggleValue),_graphViewController);
            HandleOnValueChanged<ToggleButtonGroup, ToggleButtonGroupState>(_drawingModeGroupButton,OnDrawingModeSwitched);
            HandleOnValueChanged<DropdownField, string>(_shapesDropDown, OnShapeTypeChanged);
            HandleOnValueChanged<Vector2Field, Vector2>(_selectedVertexNormalField, OnSelectedVertexNormalChanged);
            HandleOnValueChanged<FloatField, float>(_selectedVertexUField, OnSelectedVertexUChanged);
            HandleOnValueChanged<Foldout, bool>(_shapesFoldout, ShapesFoldoutToggleValueChanged);
            BindGraphShapesListViewItems();
        }

        void ShapesFoldoutToggleValueChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue) HighlightSelectedShape();
        }
        void OnSelectedVertexNormalChanged(ChangeEvent<Vector2> evt)
        {
            var selectedVertIndex = _graphViewController.SelectedVertex.Index;
            _graphViewController.CurrentShape.VertsData[selectedVertIndex] = _graphViewController.SelectedVertex.SetNormal(evt.newValue);
            _graphViewController.UpdateSelectedPointNormal(selectedVertIndex);
        }
        void OnSelectedVertexUChanged(ChangeEvent<float> evt)
        {
            _graphViewController.UpdateU(evt.newValue);
        }
        void BindGraphShapesListViewItems()
        {
            _shapesListView.itemsSource = _graphViewController.Shapes;
            _shapesListView.bindItem = BindGraphShapeItem;
           
        }

        void BindGraphShapeItem(VisualElement shape, int index)
        {
      
            var normalsColorField = shape.Q<ColorField>("NormalsColor");
            if ((normalsColorField == null)) throw new NullReferenceException(nameof(normalsColorField));
            
            normalsColorField.userData = index;
            
            var drawingColorField = shape.Q<ColorField>("ShapeColor");
            if ((drawingColorField == null)) throw new NullReferenceException(nameof(drawingColorField));
            
            drawingColorField.userData = index;
            
            var removeShapeBtn =  shape.Q<Button>("Remove");
            if ((removeShapeBtn == null)) throw new NullReferenceException(nameof(removeShapeBtn));
            
            var displayToggleGroup =  shape.Q<ToggleButtonGroup>("DisplayToggleGroup");
            if ((displayToggleGroup == null)) throw new NullReferenceException(nameof(displayToggleGroup));
            
            displayToggleGroup.userData = index;
            
            var shapeVertices = shape.Q<ListView>("Vertices");
            if ((shapeVertices == null)) throw new NullReferenceException(nameof(shapeVertices));
            
            BindField<ColorField, Color>(normalsColorField, nameof(GraphMesh2D.NormalsColor),_graphViewController.Shapes[index]);
            
            BindField<ColorField, Color>(drawingColorField, nameof(GraphMesh2D.DrawingColor), _graphViewController.Shapes[index]);
            
            HandleOnValueChanged<ColorField, Color>(normalsColorField, (x) =>
            {
                var shapeIndex = (int)normalsColorField.userData;
                foreach (var keyValueNormal in _graphViewController.Shapes[shapeIndex].NormalLinesDictionary)
                {
                    keyValueNormal.Value.Color = x.newValue;
                }
            });
            
            HandleOnValueChanged<ColorField, Color>(drawingColorField, (x) =>
            {
                var shapeIndex = (int)drawingColorField.userData;
                foreach (var keyValueLine in _graphViewController.Shapes[shapeIndex].VertsLines)
                {
                    keyValueLine.Color = x.newValue;
                }

                foreach (var circle in _graphViewController.Shapes[shapeIndex].VertsPoints)
                {
                    circle.Color = x.newValue;
                    circle.MarkDirtyRepaint();
                }
            });
     
            var shapeVisibility = _graphViewController.Shapes[index].IsVisible;
            displayToggleGroup.value =ToggleButtonGroupState.CreateFromOptions(new List<bool> { shapeVisibility, !shapeVisibility });
            HandleOnValueChanged<ToggleButtonGroup, ToggleButtonGroupState>(displayToggleGroup, x =>
            {
                VisibilityCheck(x.newValue);
            });

            void VisibilityCheck(ToggleButtonGroupState toggleButtonGroupState)
            {
                if (toggleButtonGroupState[0])
                {
                    _graphViewController.Shapes[index].Show();
                }
                else if (toggleButtonGroupState[1])
                {
                    _graphViewController.Shapes[index].Hide();
                }
            }
            
            removeShapeBtn.clicked += () =>
            {
                _graphViewController.RemoveShape(index);
                HighlightSelectedShape();
            };
            
           
            shapeVertices.itemsSource = _graphViewController.Shapes[index].VertsData;
            shapeVertices.bindItem = (item, j) =>
            {
                var vertexFoldout = item.Q<Foldout>("VertexFoldout");
                vertexFoldout.text = "Vertex " + j;

                var vertexPosField = item.Q<Vector2Field>("PositionVector");
                vertexPosField.value = _graphViewController.Shapes[index].VertsData[j].Point;

                var vertexNormalField = item.Q<Vector2Field>("NormalVector");
                vertexNormalField.value = _graphViewController.Shapes[index].VertsData[j].Normal;
                
                var vertexUField = item.Q<FloatField>("UField");
                vertexUField.value = _graphViewController.Shapes[index].VertsData[j].U;
                
            };
            HighlightSelectedShape();
        }
        void OnDrawingModeSwitched(ChangeEvent<ToggleButtonGroupState> evt)
        {
            var newValue = evt.newValue;
            _graphViewController.CurrentDrawingMode = newValue switch
            {
                _ when newValue[0] => DrawingMode.Positions,
                _ when newValue[1] => DrawingMode.Normals,
                _=> DrawingMode.Selector
            };
        }

        void OnShapeTypeChanged(ChangeEvent<string> evt)
        {
            var newValue = evt.newValue;
            _graphViewController.SelectedShapeType = Enum.Parse<ShapeTypes>(newValue);
            MultiShapeWarningCheck();
            HandleAddButton();
        }

        void MultiShapeWarningCheck()
        {
            if (_graphViewController.SelectedShapeType is ShapeTypes.Single or ShapeTypes.Spiral &&_graphViewController.Shapes.Count > 1)
            {
                ShowWarning("the extra shapes will be discarded. consider switching to multi shapes.");
            }
            else
            {
                ShowWarning(null);
            }
        }
        void UpdateSelectedVertex(int previousIndex, int newIndex)
        {

            switch (_graphViewController.CurrentInspectorModes)
            {
                case InspectorModes.Edit:
                    _selectedVertexLabel.text = $" Selected Vertex : {newIndex} ";
                    _selectedVertexPositionField.value = _graphViewController.SelectedVertex.Point;
                    _selectedVertexNormalField.value = _graphViewController.SelectedVertex.Normal;
                    _selectedVertexUField.value = _graphViewController.SelectedVertex.U;
                    break;
                case InspectorModes.Draw:
                    if(!_graphViewController.ShapeFoldoutToggleValue) return;
                    var shapeIndex = _graphViewController.Shapes.IndexOf(_graphViewController.CurrentShape);
                    var currentShapeVerticesListView = _shapesListView.GetRootElementForIndex(shapeIndex);
                    var selectedVertex = currentShapeVerticesListView.Q<ListView>("Vertices");
                    if (previousIndex > -1)
                    {
                        selectedVertex.GetRootElementForIndex(previousIndex).Q<Foldout>("VertexFoldout").value = false;
                    }
            
                    var foldout = selectedVertex.GetRootElementForIndex(newIndex).Q<Foldout>("VertexFoldout");
                    if(foldout != null) foldout.value = true;
                    selectedVertex.selectedIndex = newIndex;
                    _shapesListView.ScrollTo(selectedVertex.GetRootElementForIndex(newIndex));

                    break;
            }
        }
        void HandleOnValueChanged<T, TV>(T target, EventCallback<ChangeEvent<TV>> action) where T : VisualElement,INotifyValueChanged<TV>
        {
            if(target is null) throw new ArgumentNullException(nameof(target));
            
            target.RegisterValueChangedCallback(action);
        }
        static T BindField<T,TS>(T target, [NotNull] string dataPath, object dataSource,BindingMode bindingMode = BindingMode.TwoWay) where T : VisualElement,INotifyValueChanged<TS>
        {
           
            if (dataPath == null) throw new ArgumentNullException(nameof(dataPath));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (dataSource is null) throw new ArgumentNullException(nameof(dataSource));
            
            target.dataSource =dataSource;
            target.SetBinding(nameof(target.value),new DataBinding
            { 
                dataSourcePath = new PropertyPath(dataPath),
                bindingMode = bindingMode
                    
            });
            return target;
        }
        
        
    }
}