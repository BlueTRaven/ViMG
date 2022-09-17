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
    [Serializable]
    [EntityMeta(0, 0)]
    public class Sapling : Entity, ICubeTracker
    {
        private static SimpleMesh<VertexPositionColorTextureNormal, int> xMesh;

        private float toGrowTimer;
		private float toGrowTime;

        public CubePosition TrackedPosition => CubePosition.FromWorldSpace(Position);

		public Sapling()
        {

        }

        public Sapling(CubePosition position)
        {
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
        }

        public override void OnLoad(byte[] loadBytes, in int version)
        {
            base.OnLoad(loadBytes, version);

			int index = 0;
			Position = SaveHelper.LoadCubePosition(loadBytes, ref index).InWorldSpace(null);
			toGrowTime = SaveHelper.LoadFloat32(loadBytes, ref index);
			toGrowTimer = SaveHelper.LoadFloat32(loadBytes, ref index);
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

			if (xMesh == null)
				MakeMesh(device);

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(xMesh.texture,
				DrawHelper.BlackPixel, DrawHelper.BlackPixel, xMesh.VBO, xMesh.IBO,
				Matrix.CreateRotationY(MathHelper.ToRadians(45f)) *
				Matrix.CreateTranslation(Position), GetSourceRect()));
		}

		private RectangleF GetSourceRect()
        {
			float percent = 1 - (toGrowTimer / toGrowTime);
			int i = (int)(3f * percent);

			return new RectangleF(16 * i, 80, 16, 16);
        }

        private void MakeMesh(GraphicsDevice device)
        {
			Vector3 min = -new Vector3(Cube.CUBE_SCALE / 2, 0, -Cube.CUBE_SCALE / 2);
			Vector3 max = new Vector3(Cube.CUBE_SCALE / 2, Cube.CUBE_SCALE, Cube.CUBE_SCALE / 2);

			Vector3 a = new Vector3(max.X, min.Y, max.Z);
			Vector3 b = new Vector3(min.X, min.Y, max.Z);
			Vector3 c = new Vector3(min.X, max.Y, max.Z);
			Vector3 d = new Vector3(max.X, max.Y, max.Z);

			Vector3 e = new Vector3(min.X, min.Y, max.Z) + new Vector3(-max.X, 0, -max.Z);
			Vector3 f = new Vector3(max.X, min.Y, max.Z) + new Vector3(-max.X, 0, -max.Z);
			Vector3 g = new Vector3(max.X, max.Y, max.Z) + new Vector3(-max.X, 0, -max.Z);
			Vector3 h = new Vector3(min.X, max.Y, max.Z) + new Vector3(-max.X, 0, -max.Z);

			e = Vector3.Transform(e, Matrix.CreateRotationY(MathHelper.ToRadians(90)));
			f = Vector3.Transform(f, Matrix.CreateRotationY(MathHelper.ToRadians(90)));
			g = Vector3.Transform(g, Matrix.CreateRotationY(MathHelper.ToRadians(90)));
			h = Vector3.Transform(h, Matrix.CreateRotationY(MathHelper.ToRadians(90)));

			List<VertexPositionColorTextureNormal> vertices = new List<VertexPositionColorTextureNormal>();
			List<int> indices = new List<int>();

            Vector2 atx = new Vector2(1, 1);
            Vector2 btx = new Vector2(0, 1);
            Vector2 ctx = new Vector2(0, 0);
			Vector2 dtx = new Vector2(1, 0);

			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(a, Color.White, atx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(b, Color.White, btx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(c, Color.White, ctx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(d, Color.White, dtx, new Vector3(0, 0, 1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(b, Color.White, btx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(a, Color.White, atx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(d, Color.White, dtx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(c, Color.White, ctx, new Vector3(0, 0, -1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(e, Color.White, atx, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(f, Color.White, btx, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(g, Color.White, ctx, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(h, Color.White, dtx, new Vector3(-1, 0, 0)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(f, Color.White, btx, new Vector3(1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(e, Color.White, atx, new Vector3(1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(h, Color.White, dtx, new Vector3(1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(g, Color.White, ctx, new Vector3(1, 0, 0)));

			xMesh = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("cubes_textures"));
		}
    }
}
