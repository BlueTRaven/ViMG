using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG.Entities
{
	[EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
	[EntityMeta(0, 0)]
	public class AncientAltar : Entity, ICubeTracker
	{
		private static SimpleMesh<VertexCube, int> mesh;

		private float radius;

		public CubePosition TrackedPosition { get; private set; }

		private int light = -1;

		public AncientAltar()
        {

        }

		public AncientAltar(CubePosition position, float radius)
		{
			TrackedPosition = position;
			this.radius = radius;
			this.Position = position.InWorldSpace(null) + new Vector3(Cube.CUBE_SCALE / 2, Cube.CUBE_SCALE * 1.25f, Cube.CUBE_SCALE / 2f);
		}

		public bool OnInteract(Player player)
		{
			return false;
		}

		public void TrackingCubeDestroyed(World world, ChunkManager cm)
		{
			world.EntityManager.Remove(this);
			if (light != -1)
				world.LightManager.Remove(light);
		}

		public override void Update(double deltaTime)
		{
			base.Update(deltaTime);

			BoundingSphere sphere = new BoundingSphere(Position, radius);

			if (!Main.camera.GetFrustum().Intersects(sphere))
			{
				if (light != -1)
				{
					world.LightManager.Remove(light);
					light = -1;
				}
			}
			else
			{
				if (light == -1)
				{
					light = world.LightManager.Add(Position, 0, radius, Color.Red);
				}
			}
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			base.Draw(device, effect);

			if (mesh == null)
				MakeMesh(device);

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(mesh.texture, DrawHelper.BlackPixel, DrawHelper.WhitePixel, mesh.VBO, mesh.IBO,
				Matrix.CreateRotationY(MathHelper.ToRadians(45f)) * 
				Matrix.CreateTranslation(Position + new Vector3(0, Cube.CUBE_SCALE, 0)), new RectangleF(112, 16, 16, 16)));
			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(mesh.texture, DrawHelper.BlackPixel, DrawHelper.WhitePixel, mesh.VBO, mesh.IBO,
				Matrix.CreateRotationY(MathHelper.ToRadians(-45f)) * 
				Matrix.CreateTranslation(Position + new Vector3(0, Cube.CUBE_SCALE, 0)), new RectangleF(112, 16, 16, 16)));
		}

		public override void OnSave(List<byte> saveBytes)
		{
			base.OnSave(saveBytes);

			SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);
			SaveHelper.SaveFloat32(saveBytes, radius);
		}

		public override void OnLoad(byte[] loadBytes, in int version)
		{
			base.OnLoad(loadBytes, version);

			int index = 0;

			TrackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
			Position = TrackedPosition.InWorldSpace(null) + new Vector3(Cube.CUBE_SCALE / 2, Cube.CUBE_SCALE * 1.5f, Cube.CUBE_SCALE / 2f);

			radius = SaveHelper.LoadFloat32(loadBytes, ref index);
		}

		private static void MakeMesh(GraphicsDevice device)
		{
			Vector3 min = -new Vector3(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE, 0);
			Vector3 max = new Vector3(Cube.CUBE_SCALE / 2f, 0, 0);

			Vector3 a = new Vector3(max.X, min.Y, max.Z);
			Vector3 b = new Vector3(min.X, min.Y, max.Z);
			Vector3 c = new Vector3(min.X, max.Y, max.Z);
			Vector3 d = new Vector3(max.X, max.Y, max.Z);

			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			Vector2 atx = new Vector2(0, 1);
			Vector2 btx = new Vector2(1, 1);
			Vector2 ctx = new Vector2(1, 0);
			Vector2 dtx = new Vector2(0, 0);

			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(a, Color.White, atx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(b, Color.White, btx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(c, Color.White, ctx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(d, Color.White, dtx, new Vector3(0, 0, 1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(b, Color.White, btx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(a, Color.White, atx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(d, Color.White, dtx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(c, Color.White, ctx, new Vector3(0, 0, -1)));

			mesh = new SimpleMesh<VertexCube, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("cubes_textures"));
		}
	}
}
