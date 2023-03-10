using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.GameStates;

namespace ViMG.Cubes
{
    public class CubeChainLight : Cube
    {
        private Cube chains;
        private Vector4 color;

        public CubeChainLight() : base("ceiling_chain_light", new RectangleF(80, 80, 16, 16), Color.White, 10)
        {
            Transparency = TransparencyValue.Transparent;
            Collision = CollisionValue.Rope;

            Name = "Rusted Steel Chain Light";

            color = Color.CornflowerBlue.ToVector4();
            color.W = 0.5f;
        }

        public override void PostChunkGen(WorldPrototype world, CubePosition position)
        {
            base.PostChunkGen(world, position);

            world.AddEntity(new Entities.CubeLight(position, color, new Vector2(Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 2.5f)));
        }

        public override void OnPlayerPlaced(Player player, CubePosition position)
        {
            base.OnPlayerPlaced(player, position);

            player.world.EntityManager.Add(new Entities.CubeLight(position, color, new Vector2(Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 2.5f)));
        }

        public override RectangleF GetSourceRect(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters)
        {
            chains ??= Main.Registry.CubeRegistry.Get("ceiling_chains");

            Cube aboveCube = world.ChunkManager.ThreadedView.GetCube(parameters.position + new CubePosition(0, 1, 0)).GetOrDefault(Main.Registry.CubeRegistry.Air);
            //Cube belowCube = world.ChunkManager2.GetCube(pos - new CubePosition(0, 1, 0)).GetOrDefault(Main.Registry.CubeRegistry.Air);
            //if it's solid, we're hanging from the ceiling. Use the top-attached sourceRect.
            if (aboveCube != this)
            {
                if (aboveCube != chains && aboveCube.Solid)
                    return new RectangleF(80, 80, 16, 16);
                else return new RectangleF(96, 80, 16, 16);
            }
            else return base.GetSourceRect(pass, world, parameters);
        }

        public override bool ShouldMeshPass(RenderPass pass)
        {
            return pass == RenderPass.Opaque;
        }

        public override void MakeCubeVerts(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters, List<VertexCube> vertices, List<int> indices)
        {
            parameters.positionWS += new Vector3(CUBE_SCALE / 2f, 0, CUBE_SCALE / 2f);
            DrawHelper3D.MakeXMeshVerts(pass, world, parameters, Vector3.One, vertices, indices);
        }
    }
}
