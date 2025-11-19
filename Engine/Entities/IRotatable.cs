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
        public Vector3 Rotation { get; set; } // in euler angles

        public Vector3 Forward
        {
            get
            {
                Matrix mat = Matrix.CreateRotationX(-Rotation.X) *
                        Matrix.CreateRotationY(-Rotation.Y) *
                        Matrix.CreateRotationZ(-Rotation.Z);

                return Vector3.Transform(new Vector3(0, 0, 1), mat);
            }
        }

        public Vector3 ForwardYawOnly
        {
            get
            {
                Matrix mat = Matrix.CreateRotationY(-Rotation.Y);

                return Vector3.Transform(new Vector3(0, 0, 1), mat);
            }
        }

        public Vector3 Up
        {
            get
            {
                Matrix mat = Matrix.CreateRotationX(-Rotation.X) *
                            Matrix.CreateRotationY(-Rotation.Y) *
                            Matrix.CreateRotationZ(-Rotation.Z);

                return Vector3.Transform(new Vector3(0, 1, 0), mat);
            }
        }

        public Vector3 UpYawOnly
        {
            get
            {
                Matrix mat = Matrix.CreateRotationY(-Rotation.Y);

                return Vector3.Transform(new Vector3(0, 1, 0), mat);
            }
        }

        public Vector3 Right
        {
            get
            {
                Matrix mat = Matrix.CreateRotationX(-Rotation.X) *
                            Matrix.CreateRotationY(-Rotation.Y) *
                            Matrix.CreateRotationZ(-Rotation.Z);

                return Vector3.Transform(new Vector3(1, 0, 0), mat);
            }
        }
    }
}
