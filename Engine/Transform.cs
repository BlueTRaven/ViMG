using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
    public static class Transform
    {
        public static Matrix FromTRS(Vector3 translation, Vector3 rotation, Vector3 scale)
        {
            return Matrix.CreateTranslation(translation) *
                Matrix.CreateRotationX(rotation.X) *
                Matrix.CreateRotationY(rotation.Y) *
                Matrix.CreateRotationZ(rotation.Z) *
                Matrix.CreateScale(scale);
        }
    }
}
