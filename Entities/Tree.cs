using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG.Entities
{
	[Serializable]
	[EntityMeta(0, 0)]
	public class Tree : Entity
	{
		private static SimpleMesh<VertexPositionColorTextureNormal, int> meshTrunk;
		private static SimpleMesh<VertexPositionColorTextureNormal, int> meshSegmentA;
		private static SimpleMesh<VertexPositionColorTextureNormal, int> meshSegmentB;
		private static SimpleMesh<VertexPositionColorTextureNormal, int> meshTreeTop;

		private int baseSize;
		private int size;

		private CubePosition basePosition;

		private Rectangle3D bounds;

		public Tree()
		{
			AlwaysRender = true;
		}

		public Tree(Vector3 position, int size, CubePosition basePosition)
		{
			AlwaysRender = true;

			this.Position = position;
			this.baseSize = size;
			this.size = baseSize;
			this.basePosition = basePosition;

			bounds = new Rectangle3D(basePosition.InWorldSpace(null), new Vector3(Cube.CUBE_SCALE, Cube.CUBE_SCALE * (size + 4), Cube.CUBE_SCALE));
		}

		public override void Update(double deltaTime)
		{
			base.Update(deltaTime);
		}

		public override void OnCubeUpdated(ChunkData updatingParent, CubePosition updating, int updatedId)
		{
			base.OnCubeUpdated(updatingParent, updating, updatedId);

			// If we deleted the base position, we know that the entire tree is going to fall.
			if (updating == basePosition && updatedId == 0)
			{
				size = 0;
				world.EntityManager.Remove(this);
				return;
			}

			if (size == 0)
				return;
			
			// If we're on the same y axis
			if (updating.X == basePosition.X && updating.Z == basePosition.Z && updating.Y > basePosition.Y && updating.Y < basePosition.Y + size)
			{
				if (updatedId == 0)
				{
					int sizeA = updating.Y - basePosition.Y - 1;

					if (sizeA < size)
						size = sizeA;
				}
			}
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			//Don't draw in depth buffer.
			if (effect.Name == "Effects/depth")
				return;

			base.Draw(device, effect);

			if (meshTrunk == null)
				MakeMesh(device);

			if (Main.camera.GetFrustum().Contains(new BoundingBox(bounds.Position, bounds.FarPosition)) == ContainmentType.Disjoint)
				return;

			//device.RasterizerState = Main.wireframeRS;

			meshTrunk.Draw(device, effect, 
				Matrix.CreateRotationY(MathHelper.ToRadians(45f)) * 
				Matrix.CreateTranslation(Position));
			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.DeferredDraw(meshTrunk.texture, DrawHelper.BlackPixel, DrawHelper.BlackPixel, meshTrunk.VBO, meshTrunk.IBO,
				Matrix.CreateRotationY(MathHelper.ToRadians(45f)) *
				Matrix.CreateTranslation(Position),
				Main.camera.GetViewMatrix(), Main.camera.GetProjectionMatrix(), new RectangleF(0, 96 - 16, 80, 16)));

			for (int i = 0; i < size; i++)
			{
				meshSegmentB.Draw(device, effect, 
					Matrix.CreateRotationY(MathHelper.ToRadians(45f)) * 
					Matrix.CreateTranslation(Position + new Vector3(0, Cube.CUBE_SCALE * (i + 1), 0)));
				Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.DeferredDraw(meshSegmentB.texture, DrawHelper.BlackPixel, DrawHelper.BlackPixel, meshSegmentB.VBO, meshSegmentB.IBO,
					Matrix.CreateRotationY(MathHelper.ToRadians(45f)) *
					Matrix.CreateTranslation(Position + new Vector3(0, Cube.CUBE_SCALE * (i + 1), 0)),
					Main.camera.GetViewMatrix(), Main.camera.GetProjectionMatrix(), new RectangleF(0, 48, 80, 32)));
			}

			if (size == baseSize)
			{
				meshTreeTop.Draw(device, effect,
					Matrix.CreateRotationY(MathHelper.ToRadians(45f)) *
					Matrix.CreateTranslation(Position + new Vector3(0, Cube.CUBE_SCALE * (baseSize + 1), 0)));

				Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.DeferredDraw(meshTreeTop.texture, DrawHelper.BlackPixel, DrawHelper.BlackPixel, meshTreeTop.VBO, meshTreeTop.IBO,
					Matrix.CreateRotationY(MathHelper.ToRadians(45f)) *
					Matrix.CreateTranslation(Position + new Vector3(0, Cube.CUBE_SCALE * (baseSize + 1), 0)),
					Main.camera.GetViewMatrix(), Main.camera.GetProjectionMatrix(), new RectangleF(0, 0, 80, 96)));
			}
		}

		private static void MakeMesh(GraphicsDevice device)
		{
			MakeMeshTrunk(device);
			MakeMeshSegmentA(device);
			MakeMeshSegmentB(device);
			MakeMeshTreeTop(device);
		}

		private static void MakeMeshTrunk(GraphicsDevice device)
		{
			Vector3 min = -new Vector3(Cube.CUBE_SCALE * 5 / 2, 0, -Cube.CUBE_SCALE * 5 / 2);
			Vector3 max = new Vector3(Cube.CUBE_SCALE * 5 / 2, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 5 / 2);

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

			const float segmentSize = ((1f / 96f) * 16f);
			Vector2 atx = new Vector2(0, 1);
			Vector2 btx = new Vector2(1, 1);
			Vector2 ctx = new Vector2(1, segmentSize * 5);
			Vector2 dtx = new Vector2(0, segmentSize * 5);

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

			meshTrunk = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("tree"));
		}

		private static void MakeMeshSegmentA(GraphicsDevice device)
		{
			Vector3 min = -new Vector3(Cube.CUBE_SCALE * 5 / 2, 0, -Cube.CUBE_SCALE * 5 / 2);
			Vector3 max = new Vector3(Cube.CUBE_SCALE * 5 / 2, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 5 / 2);

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

			const float segmentSize = ((1f / 80f) * 16f);
			Vector2 atx = new Vector2(0, segmentSize * 4);
			Vector2 btx = new Vector2(1, segmentSize * 4);
			Vector2 ctx = new Vector2(1, segmentSize * 3);
			Vector2 dtx = new Vector2(0, segmentSize * 3);

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

			meshSegmentA = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("tree"));
		}

		private static void MakeMeshSegmentB(GraphicsDevice device)
		{
			Vector3 min = -new Vector3(Cube.CUBE_SCALE * 5 / 2, 0, -Cube.CUBE_SCALE * 5 / 2);
			Vector3 max = new Vector3(Cube.CUBE_SCALE * 5 / 2, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 5 / 2);

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

			const float segmentSize = ((1f / 96f) * 16f);
			Vector2 atx = new Vector2(0, segmentSize * 4);
			Vector2 btx = new Vector2(1, segmentSize * 4);
			Vector2 ctx = new Vector2(1, segmentSize * 3);
			Vector2 dtx = new Vector2(0, segmentSize * 3);

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

			meshSegmentB = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("tree"));
		}

		private static void MakeMeshTreeTop(GraphicsDevice device)
		{
			Vector3 min = -new Vector3(Cube.CUBE_SCALE * 5 / 2, 0, -Cube.CUBE_SCALE * 5 / 2);
			Vector3 max = new Vector3(Cube.CUBE_SCALE * 5 / 2, Cube.CUBE_SCALE * 3, Cube.CUBE_SCALE * 5 / 2);

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

			const float segmentSize = ((1f / 96f) * 16f);
			Vector2 atx = new Vector2(0, segmentSize * 3);
			Vector2 btx = new Vector2(1, segmentSize * 3);
			Vector2 ctx = new Vector2(1, 0);
			Vector2 dtx = new Vector2(0, 0);

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

			meshTreeTop = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("tree"));
		}

		public override void OnSave(List<byte> saveBytes)
		{
			base.OnSave(saveBytes);

			SaveHelper.SaveInt32(saveBytes, baseSize);
			SaveHelper.SaveInt32(saveBytes, size);

			SaveHelper.SaveCubePosition(saveBytes, basePosition);
		}

		public override void OnLoad(byte[] loadBytes, in int version)
		{
			base.OnLoad(loadBytes, version);

			int index = 0;
			baseSize =  SaveHelper.LoadInt32(loadBytes, ref index);
			size = SaveHelper.LoadInt32(loadBytes, ref index);

			basePosition = SaveHelper.LoadCubePosition(loadBytes, ref index);

			Position = basePosition.InWorldSpace(null) - new Vector3(Cube.CUBE_SCALE + Cube.CUBE_SCALE / 4, 0, Cube.CUBE_SCALE + Cube.CUBE_SCALE / 4);
			bounds = new Rectangle3D(basePosition.InWorldSpace(null), new Vector3(Cube.CUBE_SCALE, Cube.CUBE_SCALE * (size + (Cube.CUBE_SCALE / 5)), Cube.CUBE_SCALE));
		}
	}
}
