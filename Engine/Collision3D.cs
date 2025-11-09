using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public static class Collision3D
    {
        public struct AABB3D
        {
            public Vector3 Position;
            public readonly Vector3 HalfExtents;

            public AABB3D(Vector3 position, Vector3 halfExtents)
            {
                Position = position;
                HalfExtents = halfExtents;
            }
        }

        public struct Sweep
        {
            public Hit hit;
            public Vector3 position;
            public float time;

            public static Sweep Invalid { get { Sweep sweep = new Sweep(); sweep.time = float.MaxValue; return sweep; } }
        }

        public struct Hit
        {
            public bool valid;

            //public ICollider collider;
            public Vector3 position;
            public Vector3 overlap;
            public Vector3 normal;
            public float time;
        }

        public static Sweep TryMoveAABB(AABB3D aabb, Vector3 velocity, AABB3D[] aabbs)
        {
            Vector3 a = aabb.Position - aabb.HalfExtents;
            Vector3 b = (aabb.Position + velocity) + aabb.HalfExtents;

            AABB3D range = new AABB3D(((b - a) / 2) + a, (b - a) / 2);

            Sweep nearest = new Sweep();
            nearest.time = 1;
            nearest.position = aabb.Position + velocity;

            for (int i = 0; i < aabbs.Length; i++)
            {
                if (aabbs[i].Position == aabb.Position)
                    continue;
                else
                {
                    Sweep sweep = SweepAABBVsAABB(aabb, aabbs[i], velocity);

                    if (sweep.time < nearest.time)
                        nearest = sweep;
                }
            }

            return nearest;
        }

        public static Hit AABBVsAABB(AABB3D acollider, AABB3D bcollider)
        {
            var a = acollider;
            var b = bcollider;

            float dx = b.Position.X - a.Position.X;
            float px = (b.HalfExtents.X + a.HalfExtents.X) - MathF.Abs(dx);
            if (px <= 0)
                return new Hit();

            float dy = b.Position.Y - a.Position.Y;
            float py = (b.HalfExtents.Y + a.HalfExtents.Y) - MathF.Abs(dy);
            if (py <= 0)
                return new Hit();

            float dz = b.Position.Z - a.Position.Z;
            float pz = (b.HalfExtents.Z + a.HalfExtents.Z) - MathF.Abs(dz);
            if (pz <= 0)
                return new Hit();

            Hit hit = new Hit();
            hit.valid = true;

            if (px < pz || px < py)
            {
                if (px < py)
                {
                    //X max
                    int sx = MathF.Sign(dx);
                    hit.overlap.X = px * sx;
                    hit.normal.X = sx;
                    hit.position.X = a.Position.X + (a.HalfExtents.X * sx);
                    hit.position.Y = b.Position.Y;
                    hit.position.Z = b.Position.Z;
                }
                else
                {
                    //Y max
                    int sy = MathF.Sign(dy);
                    hit.overlap.Y = py * sy;
                    hit.normal.Y = sy;
                    hit.position.X = b.Position.X;
                    hit.position.Y = a.Position.Y + (a.HalfExtents.Y * sy);
                    hit.position.Z = b.Position.Z;
                }
            }
            else
            {
                //Z max
                int sz = MathF.Sign(dz);
                hit.overlap.Z = pz * sz;
                hit.normal.Z = sz;
                hit.position.X = b.Position.X;
                hit.position.Y = b.Position.Y;
                hit.position.Z = a.Position.Z + (a.HalfExtents.Z * sz);
            }

            return hit;
        }

        public static Hit AABBVsSegment(AABB3D collider, Vector3 point, Vector3 direction, float paddingX = 0, float paddingY = 0, float paddingZ = 0)
        {
            var aabb = collider;

            float scaleX = 1.0f / direction.X;
            float scaleY = 1.0f / direction.Y;
            float scaleZ = 1.0f / direction.Z;
            float signX = MathF.Sign(scaleX);
            float signY = MathF.Sign(scaleY);
            float signZ = MathF.Sign(scaleZ);
            float nearTimeX = (aabb.Position.X - signX * (aabb.HalfExtents.X + paddingX) - point.X) * scaleX;
            float nearTimeY = ((aabb.Position.Y - (signY * (aabb.HalfExtents.Y + paddingY))) - point.Y) * scaleY;
            float nearTimeZ = (aabb.Position.Z - signZ * (aabb.HalfExtents.Z + paddingZ) - point.Z) * scaleZ;
            float farTimeX = (aabb.Position.X + signX * (aabb.HalfExtents.X + paddingX) - point.X) * scaleX;
            float farTimeY = (aabb.Position.Y + signY * (aabb.HalfExtents.Y + paddingY) - point.Y) * scaleY;
            float farTimeZ = (aabb.Position.Z + signZ * (aabb.HalfExtents.Z + paddingZ) - point.Z) * scaleZ;

            if (float.IsNaN(nearTimeX))
                nearTimeX = scaleX;
            if (float.IsNaN(nearTimeY))
                nearTimeY = scaleY;
            if (float.IsNaN(nearTimeZ))
                nearTimeZ = scaleZ;

            if (nearTimeX > farTimeY || nearTimeY > farTimeX || nearTimeZ > farTimeZ)
                return new Hit();

            float nearTime = Math.Max(Math.Max(nearTimeX, nearTimeY), nearTimeZ);//nearTimeX > nearTimeY ? nearTimeX : nearTimeY;
            float farTime = Math.Min(Math.Min(farTimeX, farTimeY), farTimeZ);//farTimeX < farTimeY ? farTimeX : farTimeY;

            if ((nearTime >= 1 || nearTime < 0) || farTime <= 0)
                return new Hit();

            Hit hit = new Hit();
            hit.valid = true;
            hit.time = Math.Clamp(nearTime, 0, 1);

            if (nearTimeX > nearTimeZ)
            {
                hit.normal.X = -signX;
                hit.normal.Y = 0;
                hit.normal.Z = 0;
            }
            else
            {
                if (nearTimeY > nearTimeZ)
                {
                    hit.normal.X = 0;
                    hit.normal.Y = -signY;
                    hit.normal.Z = 0;
                }
                else
                {
                    hit.normal.X = 0;
                    hit.normal.Y = 0;
                    hit.normal.Z = -signZ;
                }
            }
           

            hit.overlap.X = (1.0f - hit.time) * -direction.X;
            hit.overlap.Y = (1.0f - hit.time) * -direction.Y;
            hit.overlap.Z = (1.0f - hit.time) * -direction.Z;
            hit.position.X = point.X + direction.X * (hit.time + float.Epsilon);
            hit.position.Y = point.Y + direction.Y * (hit.time + float.Epsilon);
            hit.position.Z = point.Z + direction.Z * (hit.time + float.Epsilon);
            return hit;
        }

        public static Sweep SweepAABBVsAABB(AABB3D acollider, AABB3D bcollider, Vector3 aVelocity)
        {
            var a = acollider;
            var b = bcollider;

            Sweep sweep = new Sweep();
            sweep.time = 1;

            if (Math.Abs(aVelocity.Length()) < float.Epsilon)
            {
                sweep.position = a.Position;

                sweep.hit = AABBVsAABB(acollider, bcollider);

                if (sweep.hit.valid)
                    sweep.time = 0;
                else sweep.time = 1;

                return sweep;
            }

            sweep.hit = AABBVsSegment(bcollider, a.Position, aVelocity, a.HalfExtents.X, a.HalfExtents.Y, a.HalfExtents.Z);

            if (sweep.hit.valid)
            {
                float ep = 1e-8f;
                sweep.time = Math.Clamp(sweep.hit.time - ep, 0, 1);
                sweep.position.X = a.Position.X + aVelocity.X * sweep.time;
                sweep.position.Y = a.Position.Y + aVelocity.Y * sweep.time;
                sweep.position.Z = a.Position.Z + aVelocity.Z * sweep.time;

                Vector3 direction = Vector3.Normalize(aVelocity);

                sweep.hit.position.X = Math.Clamp(
                  sweep.hit.position.X + direction.X * a.HalfExtents.X,
                  b.Position.X - b.HalfExtents.X, b.Position.X + b.Position.X);

                sweep.hit.position.Y = Math.Clamp(
                  sweep.hit.position.Y + direction.Y * a.HalfExtents.Y,
                  b.Position.Y - b.HalfExtents.Y, b.Position.Y + b.Position.Y);

                sweep.hit.position.Z = Math.Clamp(
                    sweep.hit.position.Z + direction.Z * a.HalfExtents.Z,
                    b.Position.Z - b.HalfExtents.Z, b.Position.Z + b.Position.Z);
            }
            else
            {
                sweep.position.X = a.Position.X + aVelocity.X;
                sweep.position.Y = a.Position.Y + aVelocity.Y;
                sweep.position.Z = a.Position.Z + aVelocity.Z;
                sweep.time = 1;
            }
            return sweep;
        }
    }
}
