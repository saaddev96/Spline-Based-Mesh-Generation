using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

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
        public static Spline ActiveSpline { get; set; }
        public static int PreviousSplineIndex { get; private set; }

        public static event Action OnSplinesLoaded;
        static SplinesData()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
           
        } 
       
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                LoadSplines();
                OnSplinesLoaded?.Invoke();
            }
            
        }
        
        public static void LoadSplines()
        { 
            Splines = Object.FindObjectsByType<Spline>(FindObjectsSortMode.None).OrderBy(x => x.gameObject.name).ToList();
            SampleAllSplines();
            ChangeActiveSpline(null);
            // if (Splines.Count > 0)
            // {
            //     SampleAllSplines();
            //     ChangeActiveSpline(Splines[0]);
            // }
            // else
            // {
            //     ChangeActiveSpline(null); 
            // }
        }

        static void SampleAllSplines()
        {
            foreach (var spline in Splines)
            {
                spline.Sample();
                spline.OnDeactive();
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