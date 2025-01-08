using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace THLT.SplineMeshGeneration.Scripts
{
    [Serializable]
    public class BezierKnot : IEnumerable<Transform> ,IEquatable<BezierKnot>
    {
     
        public Transform leftHandle;
        public Transform rightHandle;
        public Transform knotCenter;

        public static BezierKnot Create(string name = "knot") 
        {
            var knot = new BezierKnot()
            {
                leftHandle = new GameObject("Left Handle").transform,
                rightHandle = new GameObject("Right Handle").transform,
                knotCenter = new GameObject(name).transform
            };
            knot.knotCenter.gameObject.AddComponent<SphereCollider>().radius = 0.8f;
            return knot;
        }

        public BezierKnot SetPosition(Vector3 leftHandleVec, Vector3 center, Vector3 rightHandleVec)
        {
            knotCenter.localPosition = center;
            leftHandle.localPosition = knotCenter.InverseTransformPoint(leftHandleVec);
            rightHandle.localPosition = knotCenter.InverseTransformPoint(rightHandleVec);
            return this;
        }

        public BezierKnot SetHandlesPosition(Vector3 leftHandleVec, Vector3 rightHandleVec)
        {
            leftHandle.localPosition = leftHandleVec;
            rightHandle.localPosition = rightHandleVec;
            return this;
        }

        public BezierKnot SetParent(Transform parent)
        {

            knotCenter.SetParent(parent);
            leftHandle.SetParent(knotCenter);
            rightHandle.SetParent(knotCenter);
            EditorUtility.SetDirty(knotCenter.gameObject);
            return this;
        }

        public BezierKnot SetIcon(Texture2D handlesTexture,Texture2D centerTexture)
        {
            knotCenter.gameObject.SetIcon(centerTexture);
            leftHandle.gameObject.SetIcon(handlesTexture);
            rightHandle.gameObject.SetIcon(handlesTexture);
            return this;
        }

        public void RemoveIcons()
        {
            knotCenter.gameObject.RemoveIcon();
            leftHandle.gameObject.RemoveIcon();
            rightHandle.gameObject.RemoveIcon();
        }
        public void DestroyObjects()
        {
            if (knotCenter != null)
            {
                Object.DestroyImmediate(knotCenter.gameObject); 
            }
        }

        public IEnumerator<Transform> GetEnumerator()
        {
            return new KnotEnumerator((leftHandle, rightHandle, knotCenter));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();

        }

        public bool Equals(BezierKnot other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(leftHandle, other.leftHandle) && Equals(rightHandle, other.rightHandle) && Equals(knotCenter, other.knotCenter);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(BezierKnot)) return false;
            return Equals((BezierKnot)obj);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(leftHandle, rightHandle, knotCenter);
        }

        public static bool operator ==(BezierKnot left, BezierKnot right)
        {
            if (left is null || right is null)
                return Equals(left, right);
            return left.Equals(right);
        }
        public static bool operator !=(BezierKnot left, BezierKnot right)
        {
            if (left is null || right is null)
                return !Equals(left, right);
            return !(left.Equals(right));
        }
    }
}

public class KnotEnumerator : IEnumerator<Transform>
{
    private (Transform, Transform, Transform) Handles;

    public KnotEnumerator((Transform, Transform, Transform) handles)
    {
        Handles = handles;
    }

    private int index = -1;

    public bool MoveNext()
    {
        index++;
        return index < 3;
    }

    public void Reset()
    {
        index = -1;
    }

    public Transform Current => index switch
    {
        0 => Handles.Item1,
        1 => Handles.Item2,
        2 => Handles.Item3,
        _ => null
    };

    object IEnumerator.Current => Current;

    public void Dispose()
    {
    }
}