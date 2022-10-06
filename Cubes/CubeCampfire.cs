using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;
using ViMG.Items;

namespace ViMG.Cubes
{
    public class CubeCampfire : Cube
    {
        private static SimpleMesh<VertexCube, int> heldMesh;

        public CubeCampfire() : base("campfire", new RectangleF(192, 16, 16, 16), Color.White, 1)
        {
            Transparency = TransparencyValue.Transparent;
            Collision = CollisionValue.None;
        }

        public override SimpleMesh<VertexCube, int> GetHeldMesh(GraphicsDevice device)
        {
            if (heldMesh == null)
            {
                List<VertexCube> vertices = new List<VertexCube>();
                List<int> indices = new List<int>();

                Vector3 a = Vector3.Zero;
                Vector3 b = new Vector3(0, CUBE_SCALE / 2f, 0);
                Vector3 c = new Vector3(CUBE_SCALE / 2f, CUBE_SCALE / 2f, 0);
                Vector3 d = new Vector3(CUBE_SCALE / 2f, 0, 0);

                MeshHelper.MakeQuadVertsVertexPositionColorTextureNormal(b, c, d, a,
                    new Vector3(0, 0, 1), Color.White, vertices, indices);

                heldMesh = new SimpleMesh<VertexCube, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("cubes_textures"));
            }

            return heldMesh;
        }

        public override RectangleF GetHeldSourceRect(World world)
        {
            const float FRAME_TIME = 0.125f * 3;

            float alive = world.GetTime();

            float t = ((alive % FRAME_TIME) * 3f) / (FRAME_TIME * 3f);

            return new RectangleF((int)(t * 3) * 16f + 192, 16f, 16f, 16f);
        }

        public override void MakeVerts(RenderPass pass, World world, Vector3 pos, Vector3 min, Vector3 max, CubeVisualInstance visual, Cube cube, List<VertexCube> vertices, List<int> indices)
        {
			if (pass != RenderPass.Opaque)
				return;

            DrawHelper3D.MakeXMeshVerts(pass, cube, world, pos + new Vector3(CUBE_SCALE / 2f, 0, CUBE_SCALE / 2f), vertices, indices);
        }

        public override bool CanPlace(World world, ChunkManager manager, CubePosition position)
        {
            return manager.GetCube(position - new CubePosition(0, 1, 0)).GetOrDefault(Main.Registry.CubeRegistry.Air).Solid;
        }

        public override void OnAdjacentUpdated(ChunkData parent, CubePosition position, ChunkData updatingParent, CubePosition updating, int updatedId)
        {
            if (updating.Y == position.Y - 1)
            {
                //if the cube below us updates and it is an air block/no longer solid, remove self.
                Cube cube = Main.Registry.CubeRegistry.Get(updatedId);

                if (cube == null || !cube.Solid)
                    parent.SetCube(position, 0);
            }

            base.OnAdjacentUpdated(parent, position, updatingParent, updating, updatedId);
        }

        public override void OnPlayerPlaced(Player player, CubePosition position)
        {
            base.OnPlayerPlaced(player, position);

            player.GetWorld().EntityManager.Add(new EntityCubeFlame(position, player.GetWorld().GetTime() + Main.random.NextFloat(3f * 60f, 15f * 60f)));

            //player.GetWorld().EntityManager.Add(new CubeLight(position, Color.OrangeRed.ToVector4(), new Vector2(Cube.CUBE_SCALE * 4, Cube.CUBE_SCALE * 8)));
        }

        public override void OnLoaded(World world, CubePosition position)
        {
            base.OnLoaded(world, position);

            //world.EntityManager.Add(new CubeLight(position, Color.OrangeRed.ToVector4(), new Vector2(Cube.CUBE_SCALE * 4, Cube.CUBE_SCALE * 8)));
        }

        public override CubeAnimation GetAnimation(MeshHelper.CubeFace face, RenderPass pass, World world, CubePosition pos)
        {
            return new CubeAnimation(0.125f, 3);
        }
    }
}
