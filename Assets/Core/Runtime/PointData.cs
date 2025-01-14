using System;
using UnityEngine;

namespace THLT.SplineMeshGeneration.Scripts
{
    [Serializable]
    public struct PointData : IEquatable<PointData>
    {
        public Matrix4x4 spaceAtPoint;
        public Vector3 Point=> spaceAtPoint.GetPosition();
        public Vector3 Forward=> spaceAtPoint.GetColumn(2);
        public Vector3 Right=> spaceAtPoint.GetColumn(0);
        public Vector3 Up=> spaceAtPoint.GetColumn(1);
        public PointData(Matrix4x4 spaceAtPoint)
        {
            this.spaceAtPoint = spaceAtPoint;
        }

        public Vector3 LocalToWorldPoint(Vector3 localPoint)
        {
            return spaceAtPoint.MultiplyPoint3x4(localPoint);
        }
        public Vector3 LocalToWorldVec(Vector3 localVector)
        {
            return spaceAtPoint.MultiplyVector(localVector);
        }

        public PointData OffsetMatrix(Vector3 offset)
        {
            var newPoint = LocalToWorldPoint(offset);
            spaceAtPoint.m03 = newPoint.x;
            spaceAtPoint.m13 = newPoint.y;
            spaceAtPoint.m23 = newPoint.z;
            return spaceAtPoint;
        }

        public PointData RotateTowards(Vector3 dir)
        {
            if (dir == Vector3.zero)
            {
                return this;
            }

            var biNormal = new Vector3(dir.x, -dir.z, dir.y);
            var normal = Vector3.Cross(biNormal,dir );
            spaceAtPoint = Matrix4x4.TRS(spaceAtPoint.GetPosition(), Quaternion.LookRotation(dir,biNormal), Vector3.one);
            return spaceAtPoint;
        }
        public bool Equals(PointData other)
        {
            return spaceAtPoint.Equals(other.spaceAtPoint);
        }
        public override bool Equals(object obj)
        {
            return obj is PointData other && Equals(other);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(spaceAtPoint);
        }
        public static implicit operator Matrix4x4(PointData d)
        {
            return d.spaceAtPoint;
        }
        public static implicit operator PointData(Matrix4x4 v)
        {
            return new PointData(v);
        }
    }
}
