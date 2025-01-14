using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using THLT.SplineMeshGeneration.Scripts.Commands;
using THLT.SplineMeshGeneration.Scripts.Scriptables;
using Unity.Properties;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.UIElements;
using Vertex = THLT.SplineMeshGeneration.Scripts.Scriptables.Vertex;

namespace THLT.SplineMeshGeneration.Scripts.VisualElements
{
    public enum ShapeTypes
    {
        Single,
        Multi,
        Spiral
    }

    public enum InspectorModes
    {
        Draw,
        Edit
    }

    public enum DrawingMode
    {
        Positions,
        Normals,
        Selector
    }

    public class GraphViewController
    {
        private static readonly Vertex DefaultVertex = new Vertex(Vector2.zero, -1);
        private readonly GraphView _targetGraphView;


        private readonly int _maxZoomOut = 40;
        private readonly int _maxZoomIn = 6;
        private readonly int _defaultSize = 10;
        private readonly int _width = 950;
        private int _gridSize = 10;
        private int _zoom;
        private readonly Vector2 _mouseInputCorrection = new Vector2(0, -26);
        

        public int GridSize
        {
            get => _gridSize;
            set => _gridSize = value;
        }

        public Color IndicatorsColor { get; } = Color.black;
        public Color GridColor { get; } = new Color(0.1f, 0.1f, 0.1f);
        [CreateProperty] public ShapeTypes SelectedShapeType { get; set; } = ShapeTypes.Single;
        [CreateProperty] public bool ShapeFoldoutToggleValue { get; set; } = true;
        public Vertex SelectedVertex { get; set; } = DefaultVertex;
        public GraphMesh2D CurrentShape { get; private set; }
        public List<GraphMesh2D> Shapes { get; } = new();
        public int BlockSize => Mathf.RoundToInt(_width / (float)_gridSize);
        public float HalfSize => Mathf.RoundToInt(_gridSize / 2f);
        public int FontSize { get; set; } = 12;
        public float LabelOffset { get; set; } = 4;
        public event Action<int, int> OnSelectionChanged;
        public Action OnShapesDataChanged { get; set; }
        public Action OnSelectedVertDataUpdated { get; set; }
        public Action OnVerticesCleared { get; set; }
        public Action OnShapeAdded { get; set; }
        public Action OnShapeRemoved { get; set; }
        public InspectorModes CurrentInspectorModes { get; set; } = InspectorModes.Draw;
        public DrawingMode CurrentDrawingMode { get; set; } = DrawingMode.Positions;

        public GraphViewController(GraphView targetGraphView)
        {
            _targetGraphView = targetGraphView;
        }

        public void ResetSelectedVertex()
        {
            SelectedVertex = DefaultVertex;
            OnSelectedVertDataUpdated?.Invoke();
        }

        public Vector2 MousePositionToGrid(Vector2 mousePos)
        {
            var x = Mathf.Round(mousePos.x / BlockSize) - HalfSize;
            var y = -(Mathf.Round(mousePos.y / BlockSize) - HalfSize);

            return new Vector2(x, y);
        }

        public Vector2 GridPositionToMouse(Vector2 gridPos)
        {
            var x = (gridPos.x + HalfSize) * BlockSize;
            var y = HalfSize * BlockSize - gridPos.y * BlockSize;
            return new Vector2(x, y);
        }

        public bool IsValidPoint(Vector2 gridPoint)
        {
            if (CurrentShape == null) return false;
            // Check if list contains same point
            var contains = CurrentShape.VertsData.Exists(v => v.Point == gridPoint);
            // check if start point same as this grid point while the list count is more than two
            var isSameAsStart = CurrentShape.VertsData.Count > 4 &&
                                CurrentShape.VertsData[0].Point == gridPoint;
            // check if the first point equals the last one while the list count is more than two
            var isFirstAsLast = CurrentShape.VertsData.Count > 4 &&
                                CurrentShape.VertsData[0].Point == CurrentShape.VertsData[^1].Point;
            // check if point is in same line ;
            var inSameLine = CurrentShape.VertsData.Count > 2 && IsSameLine();

            bool IsSameLine()
            {
                var tolerance = 0.001f;
                var a = (CurrentShape.VertsData[^1].Point - CurrentShape.VertsData[^4].Point).normalized;
                var b = (gridPoint - CurrentShape.VertsData[^1].Point).normalized;
                var dot = Vector2.Dot(a, b);
                return 1 + dot < tolerance;
            }

            return (!contains || isSameAsStart) && !isFirstAsLast && !inSameLine;
        }

        public void ClearShape(GraphMesh2D graph)
        {
            var undoStackCount = graph.ShapeCommandInvoker.UndoStack.Count;
            for (var i = 0; i < undoStackCount; i++)
            {
                graph.ShapeCommandInvoker.UndoCommand();
            }

            OnShapesDataChanged?.Invoke();
            OnVerticesCleared?.Invoke();
        }


        public void HandleGraphScrolling(float deltaY)
        {
            _zoom += deltaY > 0 ? 2 : -2;
            _zoom = _zoom > _maxZoomOut ? _maxZoomOut : _zoom < -_maxZoomIn ? -_maxZoomIn : _zoom;
            _targetGraphView.value = _defaultSize + _zoom;
            _targetGraphView.MarkDirtyRepaint();
        }

        public void HandleGraphPointerDown(Vector2 point)
        {
            if (IsOutOfBounds(point)) return;
            var snapPoint = MousePositionToGrid(point + _mouseInputCorrection);
            switch (CurrentInspectorModes)
            {
                case InspectorModes.Draw:
                    switch (CurrentDrawingMode)
                    {
                        case DrawingMode.Positions:
                            if (CurrentShape is  null or {IsVisible:false}) return;
                            if (!IsValidPoint(snapPoint)) return;
                            var addDrawSegment = new AddAndDrawSegmentCommand(this,CurrentShape, snapPoint);
                            CurrentShape.ShapeCommandInvoker.ExecuteCommand(addDrawSegment);
                            ShapeFoldoutToggleValue = false;
                            break;
                        case DrawingMode.Normals or DrawingMode.Selector:
                            SelectVertex(point);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                case InspectorModes.Edit:
                    SelectVertex(point);
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }


        public void HandleGraphPointerDrag(Vector2 point)
        {
            if (IsOutOfBounds(point)) return;
            var snapPoint = MousePositionToGrid(point + _mouseInputCorrection);
            switch (CurrentInspectorModes)
            {
                case InspectorModes.Draw:
                    switch (CurrentDrawingMode)
                    {
                        case DrawingMode.Positions:
                            if (CurrentShape is  null or {IsVisible:false}) return;
                            if (!IsValidPoint(snapPoint)) return;
                            var addDrawSegment = new AddAndDrawSegmentCommand(this, CurrentShape,snapPoint);
                            CurrentShape.ShapeCommandInvoker.ExecuteCommand(addDrawSegment);
                            break;
                        case DrawingMode.Normals:
                            if (CurrentShape is  null or {IsVisible:false}) return;
                            DrawOrEditNormal(snapPoint);
                            break;
                        case DrawingMode.Selector:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                case InspectorModes.Edit:
                    UpdateSelectedPointPosition(snapPoint);
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        private bool IsOutOfBounds(Vector2 point)
        {
            return point.x > _width || point.y > _width - _mouseInputCorrection.y;
        }

        public LineDrawer DrawLine(Vector2 start, Vector2 end, Color color = default, float thickness = 2.0f,LineDrawer.LineCapType capType = LineDrawer.LineCapType.Circle)
        {
            var line = new LineDrawer.Line(this).Create(start, end).SetStyle(color, thickness, capType).Draw();
            _targetGraphView.Add(line);
            return line;
        }

        public CircleShape DrawVertex(Vector2 point, Color color, float radius = 5,float gap =15, float thickness = 1)
        {
            var circle = new CircleShape.Circle(this).Create(point).SetStyle(color, Color.white, radius,gap ,thickness).Draw();
            if (!CurrentShape.IsVisible)
            {
                circle.style.display = DisplayStyle.None;
            }
            _targetGraphView.Add(circle);
            return circle;
        }

        public void RemoveElementFromGraph(VisualElement visualElement)
        {
            _targetGraphView.Remove(visualElement);
        }

        public void SelectVertex(Vector2 snapPoint)
        {
            if (CurrentShape == null) return;
            var previousIndex = SelectedVertex.Index;
            
            foreach (var vert in CurrentShape.VertsData)
            {
                Debug.Log($"vert Index {vert.Index} distance :{Vector2.Distance(GridPositionToMouse(vert.Point),snapPoint+_mouseInputCorrection)}");
            }
            var verticesAtPos = CurrentShape.VertsData.Where(x => Vector2.Distance(GridPositionToMouse(x.Point),snapPoint+_mouseInputCorrection)<9f).OrderBy(x => x.Index).ToArray();
            if (verticesAtPos.Length > 0)
            {
                SelectedVertex = SelectedVertex.Index == verticesAtPos[0].Index ? verticesAtPos[1] : verticesAtPos[0];
                OnSelectionChanged?.Invoke(previousIndex, SelectedVertex.Index);
            }
        }

        public void UpdateSelectedPointPosition(Vector2 snapPoint)
        {
            if (CurrentShape == null) return;
            var selectedVertIndex = SelectedVertex.Index;
            if (selectedVertIndex == -1) return;
            var nextVertIndex = selectedVertIndex % 2 == 0 ? selectedVertIndex + 1 : selectedVertIndex - 1;
            // if (CurrentShape.VertsData.Exists(x =>GridPositionToMouse(x.Point) == GridPositionToMouse(snapPoint)) &&
            //     CurrentShape.VertsData[0].Point != snapPoint) return;
            if (SelectedVertex.Point == snapPoint) return;
            CurrentShape.VertsData[selectedVertIndex] =
                CurrentShape.VertsData[selectedVertIndex].SetPosition(snapPoint);
            CurrentShape.VertsData[nextVertIndex] = CurrentShape.VertsData[nextVertIndex].SetPosition(snapPoint);
            SelectedVertex = CurrentShape.VertsData[selectedVertIndex];
            UpdateSelectedPointShape(snapPoint);
            UpdateSelectedPointNormal(selectedVertIndex);
            UpdateSelectedPointNormal(nextVertIndex);
            OnShapesDataChanged?.Invoke();
            OnSelectedVertDataUpdated?.Invoke();
        }

        void UpdateSelectedPointShape(Vector2 snapPoint)
        {
            var selectedVertexIndex = SelectedVertex.Index;
            var lineIndex = Mathf.FloorToInt(selectedVertexIndex / 2f);
            if (lineIndex < CurrentShape.VertsLines.Count)
            {
                var secondLine = CurrentShape.VertsLines[lineIndex];
                secondLine.Start = snapPoint;
                secondLine.MarkDirtyRepaint();
            }

            if (lineIndex > 0)
            {
                var firstLine = CurrentShape.VertsLines[lineIndex - 1];
                firstLine.End = snapPoint;
                firstLine.MarkDirtyRepaint();
            }

            var vertexShape = CurrentShape.VertsPoints[lineIndex];
            vertexShape.Point = snapPoint;
            vertexShape.MarkDirtyRepaint();

        }

        public void DrawOrEditNormal(Vector2 snapPoint)
        {
            if (CurrentShape == null) return;
            var index = SelectedVertex.Index;
            if (index == -1) return;
            if (CurrentShape.NormalLinesDictionary.TryGetValue(index, out var normalLine))
            {
                if (normalLine.End == GridPositionToMouse(snapPoint)) return;
                normalLine.End = snapPoint;
                var normal = (normalLine.End - normalLine.Start).normalized;
                CurrentShape.VertsData[index] = CurrentShape.VertsData[index].SetNormal(normal);
            }
            else
            {
                var drawNormalCommand = new DrawNormalCommand(this, snapPoint, index);
                CurrentShape.ShapeCommandInvoker.ExecuteCommand(drawNormalCommand);
            }

            ShapeFoldoutToggleValue = false;
            OnShapesDataChanged?.Invoke();
        }

        public void UpdateSelectedPointNormal(int vertIndex)
        {
            if (CurrentShape == null) return;
            if (vertIndex == -1) return;
            if (!CurrentShape.NormalLinesDictionary.TryGetValue(vertIndex, out var targetLine))
            {
                if(CurrentShape.VertsData[vertIndex].Normal == Vector2.zero) return;
                DrawOrEditNormal(CurrentShape.VertsData[vertIndex].Point+CurrentShape.VertsData[vertIndex].Normal);
            }
            else if (targetLine != null)
            {
                targetLine.Start = CurrentShape.VertsData[vertIndex].Point;
                targetLine.End = CurrentShape.VertsData[vertIndex].Point + CurrentShape.VertsData[vertIndex].Normal;
            }

            OnShapesDataChanged?.Invoke();
        }

        public void UpdateU(float value)
        {
            if (CurrentShape == null) return;
            if (SelectedVertex.Index == -1) return;
            var selectedVertIndex = SelectedVertex.Index;
            CurrentShape.VertsData[selectedVertIndex] = SelectedVertex.SetU(value);
            OnShapesDataChanged?.Invoke();
        }


        public void UpdateShape()
        {
            foreach (var shape in Shapes)
            {
                foreach (var vLine in shape.VertsLines)
                {
                    vLine.MarkDirtyRepaint();
                }

                foreach (var nLine in shape.NormalLinesDictionary)
                {
                    nLine.Value.MarkDirtyRepaint();
                }

                foreach (var circle in shape.VertsPoints)
                {
                    circle.MarkDirtyRepaint();
                }
            }
        }

        public GraphMesh2D AddNewShape()
        {
            var newShape = CreateGraphMesh2D();
            Shapes.Add(ChangeCurrentShape(newShape));
            OnShapeAdded?.Invoke();
            return newShape;
        }


        public GraphMesh2D ChangeCurrentShape(GraphMesh2D newShape)
        {
            CurrentShape = newShape;
            ResetSelectedVertex();
            return CurrentShape;
        }

        public void ChangeCurrentShape(int index)
        {
            if (index >= Shapes.Count || index < 0)
            {
                ChangeCurrentShape(null);
                return;
            }

            CurrentShape = Shapes[index];
            ResetSelectedVertex();
        }

        public void RemoveShape(int index)
        {
            if (CurrentShape == Shapes[index])
            {
                ClearShape(CurrentShape);
                Shapes.RemoveAt(index);
                ChangeCurrentShape(0);
                ResetSelectedVertex();
            }
            else
            {
                ClearShape(Shapes[index]);
                Shapes.RemoveAt(index);
            }

            OnShapeRemoved?.Invoke();
            OnShapesDataChanged?.Invoke();
        }

        private GraphMesh2D CreateGraphMesh2D()
        {
            var mesh2D = new GraphMesh2D();
            return mesh2D;
        }
    }
}