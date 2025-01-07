using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Linq;

namespace THLT.SplineMeshGeneration.Scripts
{
    [System.Serializable] [InitializeOnLoad]
    public static class SplinesData
    {
        private static List<Spline> _splines;
        public static List<Spline> Splines
        {
            get => _splines ?? new List<Spline>();
            set => _splines = value?? new List<Spline>();
        }
        public static Spline ActiveSpline { get; private set; }
        public static int PreviousSplineIndex { get; private set; }

        static SplinesData()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            LoadSplines();
        }
        
        public static void LoadSplines()
        { 
            Splines = Object.FindObjectsByType<Spline>(FindObjectsSortMode.None).OrderBy(x => x.gameObject.name).ToList();
            if (Splines.Count > 0)
            {
                ChangeActiveSpline(Splines[0]);
            }
        }
        public static void ChangeActiveSpline(Spline newSpline)
        {
            PreviousSplineIndex = Splines.IndexOf(ActiveSpline);
            ActiveSpline?.OnDeactive();
            ActiveSpline = newSpline;
            ActiveSpline?.OnActive();
            Selection.activeGameObject = null;
        } 
    }
}