using System.Collections.Generic;
using THLT.SplineMeshGeneration.Scripts.Commands;
using THLT.SplineMeshGeneration.Scripts.VisualElements;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using CommandInvoker = THLT.SplineMeshGeneration.Scripts.Commands.CommandInvoker;
namespace THLT.SplineMeshGeneration.Scripts.Scriptables
{
    [System.Serializable]
    public class GraphMesh2D
    {
        public  CommandInvoker ShapeCommandInvoker {get; private set;} = new();
        [CreateProperty] public Color DrawingColor { get; set; } = new (1f, 0f, 0f);
        [CreateProperty] public Color NormalsColor { get; set; } =  new (0f, 0f, 1f,0.5f);
        public List<LineDrawer> VertsLines { get; } = new();
        public List<CircleShape> VertsPoints { get; } = new();
        public Dictionary<int, LineDrawer> NormalLinesDictionary { get; } = new();
        public List<Vertex> VertsData { get; set; }= new ();
        public bool IsVisible { get; set; } = true;
        public static implicit operator Mesh2D(GraphMesh2D mesh2D)
        {
            var mesh2d = new Mesh2D()
            {
                vertsData = mesh2D.VertsData.ToArray(),
            };
            return mesh2d;
        }


        public void Show(){


            try
            {
                foreach (var line in VertsLines)
                {
                    line.style.display = DisplayStyle.Flex;
                }

                foreach (var point in VertsPoints)
                {
                    point.style.display = DisplayStyle.Flex;
                }

                foreach (var normal in NormalLinesDictionary)
                {
                    normal.Value.style.display = DisplayStyle.Flex;
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                IsVisible = true;
            }
        }

        public void Hide()
        {
            try
            {
                foreach (var line in VertsLines)
                {
                    line.style.display = DisplayStyle.None;
                }

                foreach (var point in VertsPoints)
                {
                    point.style.display = DisplayStyle.None;
                }

                foreach (var normal in NormalLinesDictionary)
                {
                    normal.Value.style.display = DisplayStyle.None;
                }
            }
            catch
            {
                //ignored
            }
            finally
            {
                IsVisible = false;
            }
        }
    }
}
