using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

namespace ViMG.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
	[EntityMeta(0, 0)]
	public class AncientAltar : Entity, ICubeTracker
	{
		private static VerySimpleMesh mesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("cubes_textures");

        private float radius;

		public CubePosition TrackedPosition { get; private set; }

		private int light = -1;
		private bool isShadowmapped;
		private float breatheOffset;

		public AncientAltar()
        {

        }

		public AncientAltar(CubePosition position, float radius)
		{
			TrackedPosition = position;
			this.radius = radius;
			this.Position = position.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2, 0, Cube.CUBE_SCALE / 2f);
		}

        public override void Initialize(World world)
        {
            base.Initialize(world);

			breatheOffset = Main.random.NextFloat(0, 10f);
        }

        public bool OnInteract(Player player)
		{
			return false;
		}

		public void TrackingCubeUpdated(World world, ChunkManager manager, ushort updatedId)
		{
			world.EntityManager.Remove(this);
			if (light != -1)
				world.LightManager.Remove(light);
		}

		public override void Update(double deltaTime)
		{
			base.Update(deltaTime);

			BoundingSphere sphere = new BoundingSphere(Position, radius);

			/*if (!Main.camera.GetFrustum().Intersects(sphere))
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
			}*/

			float p0 = ((world.GetTime() + breatheOffset) % 7f) / 7f;
			float s0 = MathF.Sin(MathF.PI * 2 * p0) * Cube.CUBE_SCALE * 3;

			LightHelper.UpdateLight(world.LightManager, new LightHelper.LightInfo(Position + new Vector3(Cube.CUBE_SCALE / 2f),
				0, MathF.Max(Cube.CUBE_SCALE, radius + s0), Color.Red.ToVector4()), LightHelper.LightUpdateType.UpdateClean,
				sphere, ref light, ref isShadowmapped, true);
		}

		//public override void Draw(GraphicsDevice device, Effect effect)
		//{
		//	base.Draw(device, effect);

		//	if (mesh.IBO == null)
		//		MakeMesh(device);

		//	Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
		//		Matrix.CreateTranslation(Position - new Vector3(0, Cube.CUBE_SCALE / 2f, 0)), new RectangleF(112, 16, 16, 16)));
		//	/*Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("cubes_textures"),
		//		DrawHelper.BlackPixel, DrawHelper.WhitePixel, mesh.VBO, mesh.IBO,
		//		Matrix.CreateRotationY(MathHelper.ToRadians(-45f)) * 
		//		Matrix.CreateTranslation(Position + new Vector3(0, Cube.CUBE_SCALE, 0)), new RectangleF(112, 16, 16, 16)));*/
		//}

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
			Position = TrackedPosition.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2, Cube.CUBE_SCALE * 1.5f, Cube.CUBE_SCALE / 2f);

			radius = SaveHelper.LoadFloat32(loadBytes, ref index);
		}

		private void MakeMesh(GraphicsDevice device)
		{
            FastList<VertexCube> vertices = new FastList<VertexCube>();
            List<int> indices = new List<int>();

			DrawHelper3D.MakeXMeshRaw(vertices, indices, Vector3.Zero, Vector3.One, new RectangleF(0, 0, 1, 1));

			mesh = VerySimpleMesh.Opaque(device, new ChunkRenderMesher.VertexAttributes(vertices, indices));
			//mesh = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
		}
	}
}
