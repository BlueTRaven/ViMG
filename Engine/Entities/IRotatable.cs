using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Entities
{
    public interface IRotatable
    {
        //public Vector3 Rotation { get; set; } // in euler angles
        public Quaternion Rotation { get; set; }

        public Vector3 Forward
        {
            get
            {
                //Matrix mat = Matrix.CreateRotationX(-Rotation.X) *
                //        Matrix.CreateRotationY(-Rotation.Y) *
                //        Matrix.CreateRotationZ(-Rotation.Z);
                Matrix mat = Matrix.CreateFromQuaternion(Rotation);

                return Vector3.Transform(new Vector3(0, 0, 1), mat);
            }
        }

        public Vector3 ForwardYawOnly
        {
            get
            {
                var newQuat = Rotation;
                newQuat.X = 0;
                newQuat.Z = 0;
                var mag = float.Sqrt(newQuat.W * newQuat.W + newQuat.Y * newQuat.Y);
                newQuat.W /= mag;
                newQuat.Y /= mag;
                Matrix mat = Matrix.CreateFromQuaternion(newQuat);
                //Matrix mat = Matrix.CreateRotationY(-Rotation.Y);

                return Vector3.Transform(new Vector3(0, 0, 1), mat);
            }
        }

        public Vector3 Up
        {
            get
            {
                //Matrix mat = Matrix.CreateRotationX(-Rotation.X) *
                //            Matrix.CreateRotationY(-Rotation.Y) *
                //            Matrix.CreateRotationZ(-Rotation.Z);
                Matrix mat = Matrix.CreateFromQuaternion(Rotation);

                return Vector3.Transform(new Vector3(0, 1, 0), mat);
            }
        }

        public Vector3 Right
        {
            get
            {
                //Matrix mat = Matrix.CreateRotationX(-Rotation.X) *
                //            Matrix.CreateRotationY(-Rotation.Y) *
                //            Matrix.CreateRotationZ(-Rotation.Z);
                Matrix mat = Matrix.CreateFromQuaternion(Rotation);

                return Vector3.Transform(new Vector3(1, 0, 0), mat);
            }
        }
    }
}
