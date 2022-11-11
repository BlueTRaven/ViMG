using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Entities
{
	[EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
	[EntityMeta(0, 0)]
    public class Sapling : Entity, ICubeTracker
    {
		private static (VertexBuffer vbo, IndexBuffer ibo) mesh;

        private float toGrowTimer;
		private float toGrowTime;

        public CubePosition TrackedPosition => CubePosition.FromWorldSpace(Position);

		public Sapling()
        {

        }

        public Sapling(CubePosition position)
        {
			if (position.Y == 0)
				throw new Exception();

			this.Position = position.InWorldSpace(null);
			toGrowTimer = Main.random.Next(3, 60) * 60; //any amount of time between three minutes and an hour, in intervals of a minute.
			toGrowTime = toGrowTimer;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			toGrowTimer -= (float)deltaTime;

			if (toGrowTimer <= 0)
            {
				Cube treeCube = Main.Registry.CubeRegistry.Get("tree");

				//Destroy self
				world.GetChunkManager().GetChunk(TrackedPosition).GetData().SetCube(TrackedPosition, 0);

				int num = Main.random.Next(3, 12);
				for (int i = 0; i < num; i++)
				{
					var posOffset = TrackedPosition;
					posOffset.Y += i;

					world.GetChunkManager().GetChunk(posOffset).GetData().SetCube(posOffset, treeCube.Id);
				}

				Tree tree = new Tree(TrackedPosition.InWorldSpace(null) - new Vector3(Cube.CUBE_SCALE * 1.25f, 0, Cube.CUBE_SCALE * 1.25f),
					num, TrackedPosition);
				world.EntityManager.Add(tree);
			}
        }

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

			SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);
			SaveHelper.SaveFloat32(saveBytes, toGrowTime);
			SaveHelper.SaveFloat32(saveBytes, toGrowTimer);

			if (Position.Y == 0)
				throw new Exception();
        }

        public override void OnLoad(byte[] loadBytes, in int version)
        {
            base.OnLoad(loadBytes, version);

			int index = 0;
			Position = SaveHelper.LoadCubePosition(loadBytes, ref index).InWorldSpace(null);
			toGrowTime = SaveHelper.LoadFloat32(loadBytes, ref index);
			toGrowTimer = SaveHelper.LoadFloat32(loadBytes, ref index);

			if (Position.Y == 0)
				throw new Exception();
		}

        public bool OnInteract(Player player)
        {
            return false;
        }

        public void TrackingCubeDestroyed(World world, ChunkManager cm)
        {
			world.GetChunkManager().GetChunk(TrackedPosition).GetData().SetCube(TrackedPosition, 0, killTrackedEntities: false);
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
			float percent = 1 - (toGrowTimer / toGrowTime);
			int i = (int)(3f * percent);

			return new RectangleF(16 * i, 80, 16, 16);
        }

        private void MakeMesh(GraphicsDevice device)
        {
			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			DrawHelper3D.MakeXMeshRaw(vertices, indices, Vector3.Zero, Vector3.One, new RectangleF(0, 0, 1, 1));

			VertexBuffer VBO = new VertexBuffer(device, typeof(VertexCube), vertices.Count, BufferUsage.WriteOnly);
			IndexBuffer IBO = new IndexBuffer(device, typeof(int), indices.Count, BufferUsage.WriteOnly);

			VBO.SetData(vertices.ToArray());
			IBO.SetData(indices.ToArray());

			mesh = (VBO, IBO);
		}
    }
}
