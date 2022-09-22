using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public struct Point3D
    {
        public int X;
        public int Y;
        public int Z;

        public static Point3D Zero => new Point3D();

        public Point3D(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        /// <summary>
        /// Compares whether two <see cref="Point3D"/> instances are equal.
        /// </summary>
        /// <param name="value1"><see cref="Point3D"/> instance on the left of the equal sign.</param>
        /// <param name="value2"><see cref="Point3D"/> instance on the right of the equal sign.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public static bool operator ==(Point3D value1, Point3D value2)
        {
            return value1.X == value2.X
                && value1.Y == value2.Y
                && value1.Z == value2.Z;
        }

        /// <summary>
        /// Compares whether two <see cref="Point3D"/> instances are not equal.
        /// </summary>
        /// <param name="value1"><see cref="Point3D"/> instance on the left of the not equal sign.</param>
        /// <param name="value2"><see cref="Point3D"/> instance on the right of the not equal sign.</param>
        /// <returns><c>true</c> if the instances are not equal; <c>false</c> otherwise.</returns>	
        public static bool operator !=(Point3D value1, Point3D value2)
        {
            return !(value1 == value2);
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source <see cref="Point3D"/> on the left of the add sign.</param>
        /// <param name="value2">Source <see cref="Point3D"/> on the right of the add sign.</param>
        /// <returns>Sum of the vectors.</returns>
        public static Point3D operator +(Point3D value1, Point3D value2)
        {
            value1.X += value2.X;
            value1.Y += value2.Y;
            value1.Z += value2.Z;
            return value1;
        }

        /// <summary>
        /// Inverts values in the specified <see cref="Point3D"/>.
        /// </summary>
        /// <param name="value">Source <see cref="Point3D"/> on the right of the sub sign.</param>
        /// <returns>Result of the inversion.</returns>
        public static Point3D operator -(Point3D value)
        {
            value = new Point3D(-value.X, -value.Y, -value.Z);
            return value;
        }

        /// <summary>
        /// Subtracts a <see cref="Point3D"/> from a <see cref="Point3D"/>.
        /// </summary>
        /// <param name="value1">Source <see cref="Point3D"/> on the left of the sub sign.</param>
        /// <param name="value2">Source <see cref="Point3D"/> on the right of the sub sign.</param>
        /// <returns>Result of the vector subtraction.</returns>
        public static Point3D operator -(Point3D value1, Point3D value2)
        {
            value1.X -= value2.X;
            value1.Y -= value2.Y;
            value1.Z -= value2.Z;
            return value1;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source <see cref="Point3D"/> on the left of the mul sign.</param>
        /// <param name="value2">Source <see cref="Point3D"/> on the right of the mul sign.</param>
        /// <returns>Result of the vector multiplication.</returns>
        public static Point3D operator *(Point3D value1, Point3D value2)
        {
            value1.X *= value2.X;
            value1.Y *= value2.Y;
            value1.Z *= value2.Z;
            return value1;
        }

        /// <summary>
        /// Multiplies the components of vector by a scalar.
        /// </summary>
        /// <param name="value">Source <see cref="Point3D"/> on the left of the mul sign.</param>
        /// <param name="scaleFactor">Scalar value on the right of the mul sign.</param>
        /// <returns>Result of the vector multiplication with a scalar.</returns>
        public static Point3D operator *(Point3D value, float scaleFactor)
        {
            value.X = (int)(value.X * scaleFactor);
            value.Y = (int)(value.Y * scaleFactor);
            value.Z = (int)(value.Z * scaleFactor);
            return value;
        }

        /// <summary>
        /// Multiplies the components of vector by a scalar.
        /// </summary>
        /// <param name="scaleFactor">Scalar value on the left of the mul sign.</param>
        /// <param name="value">Source <see cref="Point3D"/> on the right of the mul sign.</param>
        /// <returns>Result of the vector multiplication with a scalar.</returns>
        public static Point3D operator *(float scaleFactor, Point3D value)
        {
            value.X = (int)(value.X * scaleFactor);
            value.Y = (int)(value.Y * scaleFactor);
            value.Z = (int)(value.Z * scaleFactor);
            return value;
        }

        /// <summary>
        /// Divides the components of a <see cref="Point3D"/> by the components of another <see cref="Point3D"/>.
        /// </summary>
        /// <param name="value1">Source <see cref="Point3D"/> on the left of the div sign.</param>
        /// <param name="value2">Divisor <see cref="Point3D"/> on the right of the div sign.</param>
        /// <returns>The result of dividing the vectors.</returns>
        public static Point3D operator /(Point3D value1, Point3D value2)
        {
            value1.X /= value2.X;
            value1.Y /= value2.Y;
            value1.Z /= value2.Z;
            return value1;
        }

        /// <summary>
        /// Divides the components of a <see cref="Point3D"/> by a scalar.
        /// </summary>
        /// <param name="value1">Source <see cref="Point3D"/> on the left of the div sign.</param>
        /// <param name="divider">Divisor scalar on the right of the div sign.</param>
        /// <returns>The result of dividing a vector by a scalar.</returns>
        public static Point3D operator /(Point3D value1, float divider)
        {
            float factor = 1 / divider;
            value1.X = (int)(value1.X * factor);
            value1.Y = (int)(value1.Y * factor);
            value1.Z = (int)(value1.Z * factor);
            return value1;
        }
    }

    public ref struct ValuePoint3D
    {
        public int x;
        public int y;
        public int z;

        public ValuePoint3D(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public ValuePoint3D(Point3D point)
        {
            this.x = point.X;
            this.y = point.Y;
            this.z = point.Z;
        }
    }
}
