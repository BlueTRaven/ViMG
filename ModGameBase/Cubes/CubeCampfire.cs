using BrUtility;
using Engine;
using Engine.ChunkStuff;
using Engine.Clients;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.ChunkStuff;
using ViMG.Entities;
using ViMG.Items;
using ViMG.Rendering;
using ViMG.VertexDeclarations;
using static ViMG.Cubes.Cube;

namespace ViMG.Cubes
{
    public class CubeCampfire : Cube
    {
        private static VerySimpleMesh heldMesh;

        public CubeCampfire() : base("campfire", 1)
        {
            Transparency = TransparencyValue.Transparent;
            Collision = CollisionValue.None;

            Client = new ClientCubeCampfire(this);
        }

        public override bool CanPlace(World world, ChunkManager manager, CubePosition position)
        {
            return manager.CubeView.GetCube(position - new CubePosition(0, 1, 0)).GetOrDefault(GlobalState.Registry.CubeRegistry.Air).Solid;
        }

        public override void OnAdjacentUpdated(World world, ChunkManager manager, CubePosition position, CubePosition updating, int updatedId, double updatedTime)
        {
            if (updating.Y == position.Y - 1)
            {
                //if the cube below us updates and it is an air block/no longer solid, remove self.
                Cube cube = GlobalState.Registry.CubeRegistry.Get(updatedId);

                if (cube == null || !cube.Solid)
                    manager.CubeView.SetCube(position, 0);
            }

            base.OnAdjacentUpdated(world, manager, position, updating, updatedId, updatedTime);
        }

        public override void OnPlayerPlaced(Player player, CubePosition position)
        {
            base.OnPlayerPlaced(player, position);

            player.GetWorld().EntityManager.Add(new EntityCubeFlame(position, player.GetWorld().GetTime() + GlobalState.random.NextFloat(3f * 60f, 15f * 60f)));

            //player.GetWorld().EntityManager.Add(new CubeLight(position, Color.OrangeRed.ToVector4(), new Vector2(Cube.CUBE_SCALE * 4, Cube.CUBE_SCALE * 8)));
        }

        public override void OnLoaded(World world, CubePosition position)
        {
            base.OnLoaded(world, position);

            //world.EntityManager.Add(new CubeLight(position, Color.OrangeRed.ToVector4(), new Vector2(Cube.CUBE_SCALE * 4, Cube.CUBE_SCALE * 8)));
        }

        public override bool ShouldMeshPass(RenderPass pass)
        {
            return pass == RenderPass.Opaque;
        }

        public override void MakeCubeVerts(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, FastList<VertexCube> vertices, List<int> indices, int vertexOffset = 0)
        {
            parameters.positionWS += new Vector3(CUBE_SCALE / 2f, 0, CUBE_SCALE / 2f);
            DrawHelper3D.MakeXMeshVerts(pass, data, parameters, Vector3.One, vertices, indices, vertexOffset);
        }

    }

    public class ClientCubeCampfire : ClientCube
    {
        private static VerySimpleMesh heldMesh;

        public ClientCubeCampfire(Cube cube) : base(cube, new RectangleF(192, 16, 16, 16), Color.White)
        {
        }

        public override CubeAnimation GetAnimation(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            return new CubeAnimation(0.125f, 3, 16);
        }

        public override VerySimpleMesh GetHeldMesh(GraphicsDevice device)
        {
            if (heldMesh.IBO == null)
            {
                FastList<VertexCube> vertices = new FastList<VertexCube>();
                List<int> indices = new List<int>();

                Vector3 a = Vector3.Zero;
                Vector3 b = new Vector3(0, CUBE_SCALE / 2f, 0);
                Vector3 c = new Vector3(CUBE_SCALE / 2f, CUBE_SCALE / 2f, 0);
                Vector3 d = new Vector3(CUBE_SCALE / 2f, 0, 0);

                MeshHelper.MakeQuadVertsVertexPositionColorTextureNormal(b, c, d, a,
                    new Vector3(0, 0, 1), Color.White, vertices, indices);

                heldMesh = VerySimpleMesh.Opaque(device, new ChunkRenderMesher.VertexAttributes(vertices, indices));
                //heldMesh = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
                //heldMesh = new SimpleMesh<VertexCube, int>(device, vertices, indices, GlobalState.assetsManager.GetAsset<Texture2D>("cubes_textures"));
            }

            return heldMesh;
        }

        public override RectangleF GetHeldSourceRect(ClientStates client)
        {
            const float FRAME_TIME = 0.125f * 3;

            float alive = (float)client.currInterpState.time;

            float t = ((alive % FRAME_TIME) * 3f) / (FRAME_TIME * 3f);

            return new RectangleF((int)(t * 3) * 16f + 192, 16f, 16f, 16f);
        }
    }
}
