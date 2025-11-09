using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Cubes
{
    public static class CubeHelper
    {
        public static MeshHelper.CubeFace GetFaceFromPlayerPos(Player player, CubePosition position, bool allowYAxisPlacement = true)
        {
            Vector3 dir = player.Position - (position.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2));
            Vector2 dirXZ = new Vector2(dir.X, dir.Z);

            MeshHelper.CubeFace face;

            if (dirXZ.Length() > MathF.Abs(dir.Y) || !allowYAxisPlacement)
            {
                if (MathF.Abs(dirXZ.X) > MathF.Abs(dirXZ.Y))
                {
                    //facing left or right
                    if (dirXZ.X > 0)
                        face = MeshHelper.CubeFace.LEFT;
                    else face = MeshHelper.CubeFace.RIGHT;
                }
                else
                {
                    if (dirXZ.Y > 0)
                        face = MeshHelper.CubeFace.BACK;
                    else face = MeshHelper.CubeFace.FRONT;
                }
            }
            else
            {
                if (dir.Y > 0)
                    face = MeshHelper.CubeFace.UP;
                else face = MeshHelper.CubeFace.DOWN;
            }
            

            return face;
        }
    }
}
