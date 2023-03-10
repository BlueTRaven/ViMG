using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Cubes
{
    public class CubeThorn : Cube
    {
        private const float RES = 16;
        private const float ONE_PIXEL = Cube.CUBE_SCALE / RES;

        public CubeThorn() : base("thorn", new CubeFacingLayout(new RectangleF(32, 128, 16, 16), new RectangleF(32, 144, 16, 16)), Color.White, 16, 4)
        {
            Name = "Thorn";

            Transparency = TransparencyValue.TransparentOccludesSiblings;
        }

        public override RectangleF GetSourceRect(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            MeshHelper.CubeFace obscuredFaces = ~parameters.faces;

            bool above = false;
            bool left = false;
            bool right = false;
            bool below = false;
            bool back = false;
            switch (face)
            {
                case MeshHelper.CubeFace.LEFT:
                    above = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.UP);
                    left = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.BACK);
                    right = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.FRONT);
                    below = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.DOWN);
                    back = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.RIGHT);
                    break;
                case MeshHelper.CubeFace.RIGHT:
                    above = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.UP);
                    left = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.FRONT);
                    right = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.BACK);
                    below = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.DOWN);
                    back = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.LEFT);
                    break;
                case MeshHelper.CubeFace.UP:
                    above = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.BACK);
                    below = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.FRONT);
                    left = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.LEFT);
                    right = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.RIGHT);
                    back = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.DOWN);
                    break;
                case MeshHelper.CubeFace.DOWN:
                    above = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.BACK);
                    below = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.FRONT);
                    left = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.LEFT);
                    right = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.RIGHT);
                    back = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.UP);
                    break;
                case MeshHelper.CubeFace.FRONT:
                    above = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.UP);
                    below = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.DOWN);
                    left = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.LEFT);
                    right = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.RIGHT);
                    back = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.BACK);
                    break;
                case MeshHelper.CubeFace.BACK:
                    above = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.UP);
                    below = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.DOWN);
                    left = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.RIGHT);
                    right = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.LEFT);
                    back = obscuredFaces.HasFlagFast(MeshHelper.CubeFace.FRONT);
                    break;
                default:
                    
                    break;
            }

            if (above && below && !(left || right))               //two connections, both perpendicular and parallel to each other
                return new RectangleF(32, 128, 16, 16);
            else if (left && right && !(above || below))
                return new RectangleF(32, 144, 16, 16);
            else if (left && back && !(above || below || right))    //two connections, one behind
                return new RectangleF(48, 144, 16, 16);
            else if (below && back && !(above || left || right))
                return new RectangleF(64, 144, 16, 16);
            else if (right && back && !(above || left || below))
                return new RectangleF(64, 128, 16, 16);
            else if (above && back && !(below || left || right))
                return new RectangleF(48, 128, 16, 16);
            else if (below && right && !(above || left))            //two connections perpendicular (back irrelevant)
                return new RectangleF(80, 128, 16, 16);
            else if (below && left && !(above || right))
                return new RectangleF(96, 128, 16, 16);
            else if (above && right && !(below || left))
                return new RectangleF(80, 144, 16, 16);
            else if (above && left && !(below || right))
                return new RectangleF(96, 144, 16, 16);
            else if (left && above && below && !right)              //three connections perpendicular (back irrelevant)
                return new RectangleF(112, 144, 16, 16);
            else if (left && below && right && !above)
                return new RectangleF(128, 144, 16, 16);
            else if (right && above && below && !left)
                return new RectangleF(128, 128, 16, 16);
            else if (left && above && right && !below)
                return new RectangleF(112, 128, 16, 16);
            else if (above && right && below && left)               //four connections perpendicular (back irrelevant)
                return new RectangleF(48, 160, 16, 16);
            else if (!(above || right || below || left))
                return new RectangleF(32, 160, 16, 16);
            else if (above || below && !(left || right))
                return new RectangleF(32, 128, 16, 16);
            else if (left || right && !(above || below))
                return new RectangleF(32, 144, 16, 16);
            else return new RectangleF();
        }

        public override void MakeCubeVerts(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters, List<VertexCube> vertices, List<int> indices)
        {
            /*
                        Vector3 l_t_n = new Vector3(min.X, min.Y, min.Z);
                        Vector3 r_t_n = new Vector3(max.X, min.Y, min.Z);
                        Vector3 r_b_n = new Vector3(max.X, max.Y, min.Z);
                        Vector3 l_b_n = new Vector3(min.X, max.Y, min.Z);
                        Vector3 l_t_f = new Vector3(min.X, min.Y, max.Z);
                        Vector3 r_t_f = new Vector3(max.X, min.Y, max.Z);
                        Vector3 r_b_f = new Vector3(max.X, max.Y, max.Z);
                        Vector3 l_b_f = new Vector3(min.X, max.Y, max.Z);
            */

            MakeMeshNoneOrTopOrBottom(pass, world, parameters, vertices, indices);
        }

        private void MakeMeshNoneOrTopOrBottom(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters, List<VertexCube> vertices, List<int> indices)
        {
            Vector3 min = parameters.positionWS;
            Vector3 max = parameters.positionWS + new Vector3(CUBE_SCALE);

            if ((parameters.faces & MeshHelper.CubeFace.FRONT) == MeshHelper.CubeFace.FRONT)
            {
                Vector3 l_t_n = new Vector3(min.X, min.Y, min.Z + ONE_PIXEL);
                Vector3 r_t_n = new Vector3(max.X, min.Y, min.Z + ONE_PIXEL);
                Vector3 r_b_n = new Vector3(max.X, max.Y, min.Z + ONE_PIXEL);
                Vector3 l_b_n = new Vector3(min.X, max.Y, min.Z + ONE_PIXEL);
                MakeCubeFaceVerts(pass, world, parameters, new ChunkMesher.CubeMeshingQuad(l_t_n, r_t_n, r_b_n, l_b_n, new Vector3(0, 0, -1)), 
                    MeshHelper.CubeFace.FRONT, vertices, indices);
            }

            if ((parameters.faces & MeshHelper.CubeFace.LEFT) == MeshHelper.CubeFace.LEFT)
            {
                Vector3 r_t_n = new Vector3(max.X - ONE_PIXEL, min.Y, min.Z);
                Vector3 r_b_n = new Vector3(max.X - ONE_PIXEL, max.Y, min.Z);
                Vector3 r_t_f = new Vector3(max.X - ONE_PIXEL, min.Y, max.Z);
                Vector3 r_b_f = new Vector3(max.X - ONE_PIXEL, max.Y, max.Z);
                MakeCubeFaceVerts(pass, world, parameters, new ChunkMesher.CubeMeshingQuad(r_t_n, r_t_f, r_b_f, r_b_n, new Vector3(1, 0, 0)), 
                    MeshHelper.CubeFace.LEFT, vertices, indices);
            }

            if ((parameters.faces & MeshHelper.CubeFace.BACK) == MeshHelper.CubeFace.BACK)
            {
                Vector3 l_t_f = new Vector3(min.X, min.Y, max.Z - ONE_PIXEL);
                Vector3 r_t_f = new Vector3(max.X, min.Y, max.Z - ONE_PIXEL);
                Vector3 r_b_f = new Vector3(max.X, max.Y, max.Z - ONE_PIXEL);
                Vector3 l_b_f = new Vector3(min.X, max.Y, max.Z - ONE_PIXEL);
                MakeCubeFaceVerts(pass, world, parameters, new ChunkMesher.CubeMeshingQuad(r_t_f, l_t_f, l_b_f, r_b_f, new Vector3(0, 0, 1)), 
                    MeshHelper.CubeFace.BACK, vertices, indices);
            }

            if ((parameters.faces & MeshHelper.CubeFace.RIGHT) == MeshHelper.CubeFace.RIGHT)
            {
                Vector3 l_t_n = new Vector3(min.X + ONE_PIXEL, min.Y, min.Z);
                Vector3 l_b_n = new Vector3(min.X + ONE_PIXEL, max.Y, min.Z);
                Vector3 l_t_f = new Vector3(min.X + ONE_PIXEL, min.Y, max.Z);
                Vector3 l_b_f = new Vector3(min.X + ONE_PIXEL, max.Y, max.Z);
                MakeCubeFaceVerts(pass, world, parameters, new ChunkMesher.CubeMeshingQuad(l_t_f, l_t_n, l_b_n, l_b_f, new Vector3(-1, 0, 0)), 
                    MeshHelper.CubeFace.RIGHT, vertices, indices);
            }

            if ((parameters.faces & MeshHelper.CubeFace.DOWN) == MeshHelper.CubeFace.DOWN)
            {
                Vector3 l_t_f = new Vector3(min.X, min.Y + ONE_PIXEL, max.Z);
                Vector3 r_t_f = new Vector3(max.X, min.Y + ONE_PIXEL, max.Z);
                Vector3 l_t_n = new Vector3(min.X, min.Y + ONE_PIXEL, min.Z);
                Vector3 r_t_n = new Vector3(max.X, min.Y + ONE_PIXEL, min.Z);
                MakeCubeFaceVerts(pass, world, parameters, new ChunkMesher.CubeMeshingQuad(r_t_n, l_t_n, l_t_f, r_t_f, new Vector3(0, -1, 0)), 
                    MeshHelper.CubeFace.DOWN, vertices, indices);
            }

            if ((parameters.faces & MeshHelper.CubeFace.UP) == MeshHelper.CubeFace.UP)
            {
                Vector3 r_b_f = new Vector3(max.X, max.Y - ONE_PIXEL, max.Z);
                Vector3 l_b_f = new Vector3(min.X, max.Y - ONE_PIXEL, max.Z);
                Vector3 r_b_n = new Vector3(max.X, max.Y - ONE_PIXEL, min.Z);
                Vector3 l_b_n = new Vector3(min.X, max.Y - ONE_PIXEL, min.Z);
                MakeCubeFaceVerts(pass, world, parameters, new ChunkMesher.CubeMeshingQuad(l_b_n, r_b_n, r_b_f, l_b_f, new Vector3(0, 1, 0)), 
                    MeshHelper.CubeFace.UP, vertices, indices);
            }
        }
    }
}
