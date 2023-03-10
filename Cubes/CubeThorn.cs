using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Cubes
{
    public class CubeThorn : Cube
    {
        private const float RES = 16;
        private const float ONE_PIXEL = Cube.CUBE_SCALE / RES;

        public CubeThorn() : base("thorn", new CubeFacingLayout(new RectangleF(32, 128, 16, 16), new RectangleF(32, 144, 16, 16)), Color.White, 16, 0)
        {
            Name = "Thorn";

            Transparency = TransparencyValue.TransparentOccludesSiblings;
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

            NoneOrTopOrBottom(pass, world, parameters, vertices, indices);
        }

        private void NoneOrTopOrBottom(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters, List<VertexCube> vertices, List<int> indices)
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

            if ((parameters.faces & MeshHelper.CubeFace.RIGHT) == MeshHelper.CubeFace.RIGHT)
            {
                Vector3 r_t_n = new Vector3(max.X - ONE_PIXEL, min.Y, min.Z);
                Vector3 r_b_n = new Vector3(max.X - ONE_PIXEL, max.Y, min.Z);
                Vector3 r_t_f = new Vector3(max.X - ONE_PIXEL, min.Y, max.Z);
                Vector3 r_b_f = new Vector3(max.X - ONE_PIXEL, max.Y, max.Z);
                MakeCubeFaceVerts(pass, world, parameters, new ChunkMesher.CubeMeshingQuad(r_t_n, r_t_f, r_b_f, r_b_n, new Vector3(1, 0, 0)), 
                    MeshHelper.CubeFace.RIGHT, vertices, indices);
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

            if ((parameters.faces & MeshHelper.CubeFace.LEFT) == MeshHelper.CubeFace.LEFT)
            {
                Vector3 l_t_n = new Vector3(min.X + ONE_PIXEL, min.Y, min.Z);
                Vector3 l_b_n = new Vector3(min.X + ONE_PIXEL, max.Y, min.Z);
                Vector3 l_t_f = new Vector3(min.X + ONE_PIXEL, min.Y, max.Z);
                Vector3 l_b_f = new Vector3(min.X + ONE_PIXEL, max.Y, max.Z);
                MakeCubeFaceVerts(pass, world, parameters, new ChunkMesher.CubeMeshingQuad(l_t_f, l_t_n, l_b_n, l_b_f, new Vector3(-1, 0, 0)), 
                    MeshHelper.CubeFace.LEFT, vertices, indices);
            }

            if ((parameters.faces & MeshHelper.CubeFace.DOWN) == MeshHelper.CubeFace.DOWN)
            {
                Vector3 l_t_n = new Vector3(min.X, min.Y, min.Z);
                Vector3 r_t_n = new Vector3(max.X, min.Y, min.Z);
                Vector3 l_t_f = new Vector3(min.X, min.Y, max.Z);
                Vector3 r_t_f = new Vector3(max.X, min.Y, max.Z);
                MakeCubeFaceVerts(pass, world, parameters, new ChunkMesher.CubeMeshingQuad(l_t_f, r_t_f, r_t_n, l_t_n, new Vector3(0, -1, 0)), 
                    MeshHelper.CubeFace.DOWN, vertices, indices);
            }

            if ((parameters.faces & MeshHelper.CubeFace.UP) == MeshHelper.CubeFace.UP)
            {
                Vector3 r_b_n = new Vector3(max.X, max.Y, min.Z);
                Vector3 l_b_n = new Vector3(min.X, max.Y, min.Z);
                Vector3 r_b_f = new Vector3(max.X, max.Y, max.Z);
                Vector3 l_b_f = new Vector3(min.X, max.Y, max.Z);
                MakeCubeFaceVerts(pass, world, parameters, new ChunkMesher.CubeMeshingQuad(r_b_f, l_b_f, l_b_n, r_b_n, new Vector3(0, 1, 0)), 
                    MeshHelper.CubeFace.UP, vertices, indices);
            }
        }
    }
}
