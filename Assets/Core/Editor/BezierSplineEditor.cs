using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using TreeView = UnityEngine.UIElements.TreeView;


namespace THLT.SplineMeshGeneration.Scripts.Editor
{
    public partial class BezierSplineEditor : EditorWindow
    {
        [FormerlySerializedAs("_winVisualTree")] [SerializeField]
        private VisualTreeAsset winVisualTree;
        private bool _isMouseDown;
        private VisualElement _root;
        private TreeView _splineTreeView;
        private static Mesh _knotMesh;
        private static void Print(object msg) => Debug.Log(msg);
        private static void PrintError(object msg) => Debug.LogError(msg);
        
        [MenuItem("THLT/Spline-based Mesh Generation/Splines")]
        public static void Create()
        {
            BezierSplineEditor wn = GetWindow<BezierSplineEditor>();
            wn.titleContent = new GUIContent("Spline Mesh Generator");
        }

        public void CreateGUI()
        {
            if (winVisualTree == null)
            {
                PrintError("winVisualTree is null Assign it");
                return;
            }
            _root = rootVisualElement;
            if (_root == null)
            {
                PrintError("rootVisualElement is null");
                return;
            }
            winVisualTree.CloneTree(_root);
            EditorSceneManager.sceneOpened += OnSceneOpened;
            SplinesData.LoadSplines();
            InitializeUIElements();
            PopulateTreeView();
        
        }

    
        private void OnEnable()
        { 
            _knotMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Packages/com.thlt.spline-based-mesh-generation/Meshes/Knot.fbx");
            SceneView.duringSceneGui += EditorUpdate;
            GizmoUtility.use3dIcons = false;
        }
  
        private void OnDisable()  
        {
            SceneView.duringSceneGui -= EditorUpdate;  
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            SplinesData.ChangeActiveSpline(null);
            foreach (var spline in Splines)
            {
                spline.MSpline.OnDeactive();
            }
        }
        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            SplinesData.ActiveSpline = null;
            SplinesData.LoadSplines();
            RefreshTreeView();
          
        }
        private void InitializeUIElements()
        {
            var createButton = _root.Q<Button>("CreateNew");
            if (createButton == null)
            {
                PrintError("createButton is null");
                return;
            }
            _splineTreeView = _root.Q<TreeView>("SplinesTreeView");
            if (_splineTreeView == null)
            {
                PrintError("_splineTreeView is null");
                return;
            }
            var undoButton = _root.Q<Button>("Undo");
            if (undoButton == null)
            {
                PrintError("undoButton is null");
                return;
            }
            createButton.RegisterCallback<ClickEvent>(_=>AddSpline());
            undoButton.RegisterCallback<ClickEvent>(_=>UndoKnot());
            _splineTreeView.RegisterCallback<DragUpdatedEvent>(_ =>
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            });
        }
        private void EditorUpdate(SceneView sceneView)
        {
            var e = Event.current;
            if (e.keyCode == KeyCode.Delete)
            { 
                e.type = EventType.Used;
            }
            if (e is null || CurrentSpline is null) return;
            if (e.shift && e.type == EventType.MouseDown && e.button == 0)
            {
                Tools.current = Tool.Move;
                CurrentSpline?.CreateKnot(e.mousePosition);
                _isMouseDown = true;
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                _isMouseDown = false;
            }
       
            if (_isMouseDown)
            {
                CurrentSpline?.UpdateCreatedHandlesPos(e);
            }
            CurrentSpline?.UpdateSelectedHandlesPos(e);
        }
        private void OnDestroy()
        {
            SceneView.duringSceneGui -= EditorUpdate;
        }
    }
}