using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Items;

namespace ViMG.Entities
{
	[EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
	[EntityMeta(0, 0)]
	public class GlowNode : Entity, ICubeTracker
	{
		private float radius;
		private float fade;
		private Vector4 color;

		private int light = -1;

		private static SimpleMesh<VertexCube, int> mesh;

		public CubePosition TrackedPosition { get; private set; }

		public GlowNode()
        {

        }

		public GlowNode(CubePosition position, float radius, float fade, Vector4 color)
		{
			TrackedPosition = position;
			this.Position = position.InWorldSpace(null) + new Vector3(Cube.CUBE_SCALE / 2, Cube.CUBE_SCALE, Cube.CUBE_SCALE / 2f);
			this.radius = radius;
			this.fade = fade;
			this.color = color;
		}

        public override void Initialize(World world)
        {
            base.Initialize(world);

			DestroyOnInactive = false;	//Don't destroy glow node upon becoming inactive. Otherwise we orphan the cube.
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
					light = world.LightManager.Add(Position, radius - fade, radius, color);
				}
			}
		}

		public override void OnUnload()
		{
			base.OnUnload();

			if (light != -1)
				world.LightManager.Remove(light);
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			base.Draw(device, effect);

			if (mesh == null)
				MakeMesh(device);

			/*Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(mesh.texture, DrawHelper.BlackPixel, DrawHelper.WhitePixel,
				mesh.VBO, mesh.IBO, Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position), null));*/
		}

		private void MakeMesh(GraphicsDevice device)
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

			mesh = new SimpleMesh<VertexCube, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("glow_node"));
		}

		public bool OnInteract(Player player)
		{
			//world.MineCube(TrackedPosition, true);
			//TODO this had killtrackedentities false?
			world.ChunkManager2.SetCube(TrackedPosition, 0);
			world.EntityManager.Remove(this);
			
			List<ItemInstance> items = new List<ItemInstance>();
			Main.Registry.CubeRegistry.Get("glow_node").GetDrops(items);

			foreach (ItemInstance item in items)
			{
				EntityItem ent = new EntityItem(Position, item);
				ent.Velocity = new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5), Cube.CUBE_SCALE * 1.6f, Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5));
				world.EntityManager.Add(ent);
			}

			return true;
		}

		public void TrackingCubeUpdated(World world, ChunkManager2 manager, ushort updatedId)
		{
			//world.ChunkManager2.GetChunk(TrackedPosition).GetData().SetCube(TrackedPosition, 0, killTrackedEntities: false);
			world.EntityManager.Remove(this);
		}

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

			SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);
			SaveHelper.SaveVector4(saveBytes, color);
			SaveHelper.SaveFloat32(saveBytes, radius);
			SaveHelper.SaveFloat32(saveBytes, fade);
        }

        public override void OnLoad(byte[] loadBytes, in int version)
        {
            base.OnLoad(loadBytes, version);

			int index = 0;
			TrackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
			color = SaveHelper.LoadVector4(loadBytes, ref index);
			radius = SaveHelper.LoadFloat32(loadBytes, ref index);
			fade = SaveHelper.LoadFloat32(loadBytes, ref index);

			this.Position = TrackedPosition.InWorldSpace(null) + new Vector3(Cube.CUBE_SCALE / 2, Cube.CUBE_SCALE, Cube.CUBE_SCALE / 2f);

		}
	}
}
