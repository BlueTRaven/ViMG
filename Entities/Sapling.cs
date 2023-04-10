using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.VertexDeclarations;

namespace ViMG.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
	[EntityMeta(1, 0)]
    public class Sapling : Entity, ICubeTracker
    {
		private static (VertexBuffer vbo, IndexBuffer ibo) mesh;

		private float startTime;
		private float toGrowTime;

        public CubePosition TrackedPosition => CubePosition.FromWorldSpace(Position);
		private bool grown;

		public Sapling()
        {

        }

        public Sapling(CubePosition position)
        {
			if (position.Y == 0)
				throw new Exception();

			this.Position = position.InWorldSpace();
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

			startTime = world.GetTime();
            //any amount of time between three minutes and an hour, in intervals of a minute.
            toGrowTime = world.GetTime() + Main.random.Next(3, 60) * 60;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			if (world.GetTime() > toGrowTime && !grown)
            {
				Cube treeCube = Main.Registry.CubeRegistry.Get("tree");

				//Destroy self
				world.ChunkManager.ThreadedView.SetCube(TrackedPosition, 0);

				//TODO performance
				//batch these SetCube calls
				int num = Main.random.Next(3, 12);
				for (int i = 0; i < num; i++)
				{
					var posOffset = TrackedPosition;
					posOffset.Y += i;

					world.ChunkManager.ThreadedView.SetCube(posOffset, treeCube.Id);
				}

				Tree tree = new Tree(TrackedPosition.InWorldSpace() + new Vector3(Cube.CUBE_SCALE * 0.5f, 0, Cube.CUBE_SCALE * 0.5f),
					num, TrackedPosition);
				world.EntityManager.Add(tree);

				grown = true;
			}
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

			if (mesh.vbo == null)
				MakeMesh(device);

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(
				Main.assetsManager.GetAsset<Texture2D>("cubes_textures"),
				DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.vbo, mesh.ibo,
				Matrix.CreateTranslation(Position + new Vector3(Cube.CUBE_SCALE / 2f, 0, Cube.CUBE_SCALE / 2f)), GetSourceRect()));
		}

		private RectangleF GetSourceRect()
        {
			float percent = (world.GetTime() - startTime) / (toGrowTime - startTime);
			int i = (int)(3f * percent);

			return new RectangleF(16 * i, 80, 16, 16);
        }

        private void MakeMesh(GraphicsDevice device)
        {
			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			DrawHelper3D.MakeXMeshRaw(vertices, indices, Vector3.Zero, Vector3.One, new RectangleF(0, 0, 1, 1));

			mesh = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
		}

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);
            SaveHelper.SaveFloat32(saveBytes, toGrowTime);
            SaveHelper.SaveFloat32(saveBytes, startTime);

            if (Position.Y == 0)
                throw new Exception();
        }

        public override void OnLoad(byte[] loadBytes, in int version)
        {
            base.OnLoad(loadBytes, version);

            int index = 0;
            Position = SaveHelper.LoadCubePosition(loadBytes, ref index).InWorldSpace();

            toGrowTime = SaveHelper.LoadFloat32(loadBytes, ref index);
            if (version == 0)
            {
                _ = SaveHelper.LoadFloat32(loadBytes, ref index);
                startTime = 0;
            }
            else
            {
                startTime = SaveHelper.LoadFloat32(loadBytes, ref index);
            }

            if (Position.Y == 0)
                throw new Exception();
        }
    }
}
