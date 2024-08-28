using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

namespace ViMG.Entities
{
    //TODO this thing's broke
    public class EntityCaveCompass : Entity, ICubeTracker
    {
		private static VerySimpleMesh mesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("cubes_textures");

        private Quaternion target;
        private Quaternion current;

        private Vector3 center;
        private float density;

		public CubePosition TrackedPosition { get; private set; }

        public EntityCaveCompass()
        {

        }

        public EntityCaveCompass(CubePosition position)
        {
            this.Position = position.InWorldSpace();
            this.TrackedPosition = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            center = Vector3.Zero;
            density = float.MaxValue;

            const int SEARCH_RADIUS = 2;

            //TODO: re-implement density calculation
            /*for (int x = -SEARCH_RADIUS; x <= SEARCH_RADIUS; x++)
            {
                for (int y = -SEARCH_RADIUS; y <= SEARCH_RADIUS; y++)
                {
                    for (int z = -SEARCH_RADIUS; z <= SEARCH_RADIUS; z++)
                    {
                        ChunkPosition pos = ChunkPosition.CubeChunk(CubePosition.FromWorldSpace(Position)) + new ChunkPosition(x, y, z);
                        if (world.ChunkManager2.IsInWorldBounds(pos))
                        {
                            Chunk currentChunk = world.ChunkManager.GetChunk(pos);

                            if (currentChunk != null && !currentChunk.GetData().Filled)
                            {
                                Vector3 currentCenter = pos.InWorldSpace() + new Vector3(Chunk.CHUNK_SIZE * Cube.CUBE_SCALE);
                                float currentDensity = currentChunk.GetData().Density;// (currentCenter - Position).Length();

                                if (currentDensity > -1 && CubePosition.FromWorldSpace(currentCenter).Y < 180 && currentDensity < density)
                                {
                                    c = currentChunk;
                                    center = currentCenter;
                                    density = currentDensity;
                                }
                            }
                        }
                    }
                }
            }*/
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            Vector3 dir = center - Position;

            //if (c != null && ChunkPosition.WorldSpaceChunk(world.player.Position) == c.Position)
            {
                dir = world.player.Position - Position;
            }

            if (CubePosition.FromWorldSpace(Position).Y > 180f)
            {
                //z to prevent gymbal lock
                dir = new Vector3(0, -1, 0.0001f);
            }

            float yaw = MathF.Atan2(dir.X, dir.Z);
            float padj = MathF.Sqrt(MathF.Pow(dir.X, 2) + MathF.Pow(dir.Z, 2));
            float pitch = MathF.Atan2(padj, dir.Y);

            target = Quaternion.CreateFromYawPitchRoll(yaw, pitch, 0);

            current = Quaternion.Slerp(current, target, 0.1f);
        }

        public bool OnInteract(Player player)
        {
            return false;
        }

        public void TrackingCubeUpdated(World world, ChunkManager manager, ushort updatedId)
        {
            world.EntityManager.Remove(this);
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

			if (mesh.IBO == null)
				MakeMesh(device);

			Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
                Matrix.CreateTranslation(0, -Cube.CUBE_SCALE / 2f, 0) *
                Matrix.CreateFromQuaternion(current) *
                Matrix.CreateTranslation(0, Cube.CUBE_SCALE / 2f, 0) *
                Matrix.CreateTranslation(Position + new Vector3(Cube.CUBE_SCALE / 2f, 0, Cube.CUBE_SCALE / 2f)), new RectangleF(16, 16, 16, 16)));
		}

        private static void MakeMesh(GraphicsDevice device)
        {
            FastList<VertexCube> vertices = new FastList<VertexCube>();
            List<int> indices = new List<int>();

			DrawHelper3D.MakeXMeshRaw(vertices, indices, Vector3.Zero, Vector3.One, new RectangleF(0, 0, 1, 1));

            mesh = VerySimpleMesh.Opaque(device, new ChunkRenderMesher.VertexAttributes(vertices, indices)); // MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
		}
    }
}
