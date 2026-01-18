using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Physics
{
    public struct OrientedBoundingBox
    {
        public Vector3 Center;
        public Vector3 HalfExtents;
        public Quaternion Orientation;

        public float Top => Center.Y + HalfExtents.Y;
        public float Bottom => Center.Y - HalfExtents.Y;

        public static OrientedBoundingBox Empty => new OrientedBoundingBox(Vector3.Zero, Vector3.Zero, Quaternion.Identity);

        public OrientedBoundingBox(Vector3 center, Vector3 halfExtents, Quaternion orientation)
        {
            Center = center;
            HalfExtents = halfExtents;
            Orientation = Quaternion.Normalize(orientation);
        }

        public OrientedBoundingBox(Rectangle3D bounds)
        {
            Center = bounds.Center;
            HalfExtents = bounds.Size / 2;
            Orientation = Quaternion.Identity;
        }

        public static OrientedBoundingBox FromTwoPositions(Vector3 a, Vector3 b)
        {
            return new OrientedBoundingBox((a + b) / 2f, a - b / 2f, Quaternion.Identity);
        }

        /// <summary>
        /// Gets the world-space basis vectors of the box from its quaternion.
        /// </summary>
        public void GetAxes(out Vector3 axisX, out Vector3 axisY, out Vector3 axisZ)
        {
            Matrix rot = Matrix.CreateFromQuaternion(Orientation);

            axisX = new Vector3(rot.M11, rot.M12, rot.M13);
            axisY = new Vector3(rot.M21, rot.M22, rot.M23);
            axisZ = new Vector3(rot.M31, rot.M32, rot.M33);
        }

        /// <summary>
        /// SAT overlap test between two oriented bounding boxes.
        /// </summary>
        public bool Overlaps(OrientedBoundingBox other)
        {
            GetAxes(out var A0, out var A1, out var A2);
            other.GetAxes(out var B0, out var B1, out var B2);

            Vector3 T = other.Center - Center;

            float R00 = Vector3.Dot(A0, B0);
            float R01 = Vector3.Dot(A0, B1);
            float R02 = Vector3.Dot(A0, B2);

            float R10 = Vector3.Dot(A1, B0);
            float R11 = Vector3.Dot(A1, B1);
            float R12 = Vector3.Dot(A1, B2);

            float R20 = Vector3.Dot(A2, B0);
            float R21 = Vector3.Dot(A2, B1);
            float R22 = Vector3.Dot(A2, B2);

            float t0 = Vector3.Dot(T, A0);
            float t1 = Vector3.Dot(T, A1);
            float t2 = Vector3.Dot(T, A2);

            const float EPSILON = 1e-6f;

            float AbsR00 = MathF.Abs(R00) + EPSILON;
            float AbsR01 = MathF.Abs(R01) + EPSILON;
            float AbsR02 = MathF.Abs(R02) + EPSILON;

            float AbsR10 = MathF.Abs(R10) + EPSILON;
            float AbsR11 = MathF.Abs(R11) + EPSILON;
            float AbsR12 = MathF.Abs(R12) + EPSILON;

            float AbsR20 = MathF.Abs(R20) + EPSILON;
            float AbsR21 = MathF.Abs(R21) + EPSILON;
            float AbsR22 = MathF.Abs(R22) + EPSILON;

            float a0 = HalfExtents.X, a1 = HalfExtents.Y, a2 = HalfExtents.Z;
            float b0 = other.HalfExtents.X, b1 = other.HalfExtents.Y, b2 = other.HalfExtents.Z;

            float ra, rb;

            // Axes A0, A1, A2
            ra = a0;
            rb = b0 * AbsR00 + b1 * AbsR01 + b2 * AbsR02;
            if (MathF.Abs(t0) > ra + rb) return false;

            ra = a1;
            rb = b0 * AbsR10 + b1 * AbsR11 + b2 * AbsR12;
            if (MathF.Abs(t1) > ra + rb) return false;

            ra = a2;
            rb = b0 * AbsR20 + b1 * AbsR21 + b2 * AbsR22;
            if (MathF.Abs(t2) > ra + rb) return false;

            // Axes B0, B1, B2
            ra = a0 * AbsR00 + a1 * AbsR10 + a2 * AbsR20;
            rb = b0;
            if (MathF.Abs(t0 * R00 + t1 * R10 + t2 * R20) > ra + rb) return false;

            ra = a0 * AbsR01 + a1 * AbsR11 + a2 * AbsR21;
            rb = b1;
            if (MathF.Abs(t0 * R01 + t1 * R11 + t2 * R21) > ra + rb) return false;

            ra = a0 * AbsR02 + a1 * AbsR12 + a2 * AbsR22;
            rb = b2;
            if (MathF.Abs(t0 * R02 + t1 * R12 + t2 * R22) > ra + rb) return false;

            // Cross products (9 axes)
            ra = a1 * AbsR20 + a2 * AbsR10;
            rb = b1 * AbsR02 + b2 * AbsR01;
            if (MathF.Abs(t2 * R10 - t1 * R20) > ra + rb) return false;

            ra = a1 * AbsR21 + a2 * AbsR11;
            rb = b0 * AbsR02 + b2 * AbsR00;
            if (MathF.Abs(t2 * R11 - t1 * R21) > ra + rb) return false;

            ra = a1 * AbsR22 + a2 * AbsR12;
            rb = b0 * AbsR01 + b1 * AbsR00;
            if (MathF.Abs(t2 * R12 - t1 * R22) > ra + rb) return false;

            ra = a0 * AbsR20 + a2 * AbsR00;
            rb = b1 * AbsR12 + b2 * AbsR11;
            if (MathF.Abs(t0 * R20 - t2 * R00) > ra + rb) return false;

            ra = a0 * AbsR21 + a2 * AbsR01;
            rb = b0 * AbsR12 + b2 * AbsR10;
            if (MathF.Abs(t0 * R21 - t2 * R01) > ra + rb) return false;

            ra = a0 * AbsR22 + a2 * AbsR02;
            rb = b0 * AbsR11 + b1 * AbsR10;
            if (MathF.Abs(t0 * R22 - t2 * R02) > ra + rb) return false;

            ra = a0 * AbsR10 + a1 * AbsR00;
            rb = b1 * AbsR22 + b2 * AbsR21;
            if (MathF.Abs(t1 * R00 - t0 * R10) > ra + rb) return false;

            ra = a0 * AbsR11 + a1 * AbsR01;
            rb = b0 * AbsR22 + b2 * AbsR20;
            if (MathF.Abs(t1 * R01 - t0 * R11) > ra + rb) return false;

            ra = a0 * AbsR12 + a1 * AbsR02;
            rb = b0 * AbsR21 + b1 * AbsR20;
            if (MathF.Abs(t1 * R02 - t0 * R12) > ra + rb) return false;

            return true;
        }
    }

}
