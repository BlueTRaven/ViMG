using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.VertexDeclarations;

namespace ViMG.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
	[EntityMeta(1, 0)]
	public class Tree : Entity, IMultiCubeTracker
	{
		private static (VertexBuffer VBO, IndexBuffer IBO) meshTrunk;
		private static (VertexBuffer VBO, IndexBuffer IBO) meshSegmentA;
		private static (VertexBuffer VBO, IndexBuffer IBO) meshSegmentB;
		private static (VertexBuffer VBO, IndexBuffer IBO) meshTreeTop;

		private int baseSize;
		private int size;

		public int Size => size;
		public int MaxSize => baseSize;

		private CubePosition[] trackedPositions;
        public IEnumerable<CubePosition> TrackedPositions => trackedPositions;

		private Rectangle3D bounds;

		public bool NeedsRerender = true;

		private static CubeTree cube;

		public Tree()
		{
		}

		public Tree(Vector3 position, int size, CubePosition basePosition)
		{
			AlwaysRender = true;

			this.Position = position;
			this.baseSize = size;
			this.size = baseSize;

            trackedPositions = new CubePosition[size];
			trackedPositions[0] = basePosition;

            bounds = new Rectangle3D(basePosition.InWorldSpace(), new Vector3(Cube.CUBE_SCALE, Cube.CUBE_SCALE * (size + 4), Cube.CUBE_SCALE));
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            if (cube == null)
                cube = Main.Registry.CubeRegistry.Get("tree") as CubeTree;

            for (int i = 1; i < size; i++)
            {
                var posOffset = trackedPositions[0] + new CubePosition(0, i, 0);

                world.ChunkManager.CubeView.SetCube(posOffset, cube.Id);

                trackedPositions[i] = posOffset;
            }
        }

        public override void Update(double deltaTime)
		{
			base.Update(deltaTime);
		}

        public bool OnInteract(Player player)
        {
			return false;
        }

        public void TrackingCubeUpdated(World world, ChunkManager cm, Player? playerWhoInitiated, CubePosition position, ushort updatedId, double updatedTime)
        {
			if (updatedTime >= TimeInitialized && updatedId != cube.Id)
			{
				if (position == trackedPositions[0])
				{
					world.EntityManager.Remove(this);

                    for (int i = position.Y; i < position.Y + size; i++)
                    {
                        world.TryMineCube(playerWhoInitiated, new CubePosition(position.X, i, position.Z), 0, 0, true);
                    }

					size = 0;
                    return;
				}
				else
				{
					for (int i = position.Y + 1; i < trackedPositions[0].Y + size; i++)
					{
						world.TryMineCube(playerWhoInitiated, new CubePosition(position.X, i, position.Z), 0, 0, true);
					}

					size = position.Y - trackedPositions[0].Y;

					CubePosition[] oldTracked = trackedPositions;

					trackedPositions = new CubePosition[size];
					trackedPositions[0] = oldTracked[0];
                    for (int i = 1; i < size; i++)
                        trackedPositions[i] = trackedPositions[0] + new CubePosition(0, i, 0);

					world.EntityManager.UpdateTrackedPositions(this, oldTracked);

					NeedsRerender = true;
				}
			}
        }

        /*public override void OnCubeUpdated(CubePosition updating, int updatedId)
		{
			base.OnCubeUpdated(updating, updatedId);

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
			if (updating.X == basePosition.X && updating.Z == basePosition.Z && updating.Y > basePosition.Y && updating.Y <= basePosition.Y + size)
			{
				if (updatedId == 0)
				{
					int sizeA = updating.Y - basePosition.Y - 1;

					if (sizeA < size)
						size = sizeA;
				}
			}
		}*/

		/*public override void Draw(GraphicsDevice device, Effect effect)
		{
			base.Draw(device, effect);

			if (meshTrunk.VBO == null)
				MakeMesh(device);

			if (Main.camera.GetFrustum().Contains(new BoundingBox(bounds.Position, bounds.FarPosition)) == ContainmentType.Disjoint)
				return;

			//device.RasterizerState = Main.wireframeRS;

			Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("tree"),
				DrawHelper.BlackPixel, DrawHelper.BlackPixel, meshTrunk.VBO, meshTrunk.IBO,
				Matrix.CreateRotationY(MathHelper.ToRadians(45f)) *
				Matrix.CreateTranslation(Position), new RectangleF(0, 96 - 16, 80, 16)));

			for (int i = 0; i < size; i++)
			{
				Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("tree"),
					DrawHelper.BlackPixel, DrawHelper.BlackPixel, meshSegmentB.VBO, meshSegmentB.IBO,
					Matrix.CreateRotationY(MathHelper.ToRadians(45f)) *
					Matrix.CreateTranslation(Position + new Vector3(0, Cube.CUBE_SCALE * (i + 1), 0)), new RectangleF(0, 48, 80, 32)));
			}

			if (size == baseSize)
			{
				Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("tree"),
					DrawHelper.BlackPixel, DrawHelper.BlackPixel, meshTreeTop.VBO, meshTreeTop.IBO,
					Matrix.CreateRotationY(MathHelper.ToRadians(45f)) *
					Matrix.CreateTranslation(Position + new Vector3(0, Cube.CUBE_SCALE * (baseSize + 1), 0)), new RectangleF(0, 0, 80, 96)));
			}
		}*/

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

            FastList<VertexCube> vertices = new FastList<VertexCube>();
            List<int> indices = new List<int>();

			const float segmentSize = ((1f / 96f) * 16f);
			Vector2 atx = new Vector2(0, 1);
			Vector2 btx = new Vector2(1, 1);
			Vector2 ctx = new Vector2(1, segmentSize * 5);
			Vector2 dtx = new Vector2(0, segmentSize * 5);

			int offset = vertices.Length;
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

			offset = vertices.Length;
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

			offset = vertices.Length;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(e, Color.White, atx, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(f, Color.White, btx, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(g, Color.White, ctx, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(h, Color.White, dtx, new Vector3(-1, 0, 0)));

			offset = vertices.Length;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(f, Color.White, btx, new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(e, Color.White, atx, new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(h, Color.White, dtx, new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(g, Color.White, ctx, new Vector3(1, 0, 0)));

			meshTrunk = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices); //new SimpleMesh<VertexCube, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("tree"));
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

            FastList<VertexCube> vertices = new FastList<VertexCube>();
            List<int> indices = new List<int>();

			const float segmentSize = ((1f / 80f) * 16f);
			Vector2 atx = new Vector2(0, segmentSize * 4);
			Vector2 btx = new Vector2(1, segmentSize * 4);
			Vector2 ctx = new Vector2(1, segmentSize * 3);
			Vector2 dtx = new Vector2(0, segmentSize * 3);

			int offset = vertices.Length;
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

			offset = vertices.Length;
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

			offset = vertices.Length;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(e, Color.White, atx, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(f, Color.White, btx, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(g, Color.White, ctx, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(h, Color.White, dtx, new Vector3(-1, 0, 0)));

			offset = vertices.Length;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(f, Color.White, btx, new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(e, Color.White, atx, new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(h, Color.White, dtx, new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(g, Color.White, ctx, new Vector3(1, 0, 0)));

			meshSegmentA = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices); //new SimpleMesh<VertexCube, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("tree"));
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

            FastList<VertexCube> vertices = new FastList<VertexCube>();
            List<int> indices = new List<int>();

			const float segmentSize = ((1f / 96f) * 16f);
			Vector2 atx = new Vector2(0, segmentSize * 4);
			Vector2 btx = new Vector2(1, segmentSize * 4);
			Vector2 ctx = new Vector2(1, segmentSize * 3);
			Vector2 dtx = new Vector2(0, segmentSize * 3);

			int offset = vertices.Length;
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

			offset = vertices.Length;
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

			offset = vertices.Length;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(e, Color.White, atx, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(f, Color.White, btx, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(g, Color.White, ctx, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(h, Color.White, dtx, new Vector3(-1, 0, 0)));

			offset = vertices.Length;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(f, Color.White, btx, new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(e, Color.White, atx, new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(h, Color.White, dtx, new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(g, Color.White, ctx, new Vector3(1, 0, 0)));

			meshSegmentB = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices); //new SimpleMesh<VertexCube, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("tree"));
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

            FastList<VertexCube> vertices = new FastList<VertexCube>();
            List<int> indices = new List<int>();

			const float segmentSize = ((1f / 96f) * 16f);
			Vector2 atx = new Vector2(0, segmentSize * 3);
			Vector2 btx = new Vector2(1, segmentSize * 3);
			Vector2 ctx = new Vector2(1, 0);
			Vector2 dtx = new Vector2(0, 0);

			int offset = vertices.Length;
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

			offset = vertices.Length;
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

			offset = vertices.Length;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(e, Color.White, atx, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(f, Color.White, btx, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(g, Color.White, ctx, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(h, Color.White, dtx, new Vector3(-1, 0, 0)));

			offset = vertices.Length;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(f, Color.White, btx, new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(e, Color.White, atx, new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(h, Color.White, dtx, new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(g, Color.White, ctx, new Vector3(1, 0, 0)));

			meshTreeTop = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices); //new SimpleMesh<VertexCube, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("tree"));
		}

		public override void OnSave(List<byte> saveBytes)
		{
			base.OnSave(saveBytes);

			SaveHelper.SaveInt32(saveBytes, baseSize);
			SaveHelper.SaveInt32(saveBytes, size);

			for (int i = 0; i < size; i++)
				SaveHelper.SaveCubePosition(saveBytes, trackedPositions[i]);
		}

		public override void OnLoad(byte[] loadBytes, in int version)
		{
			base.OnLoad(loadBytes, version);

			int index = 0;
			baseSize =  SaveHelper.LoadInt32(loadBytes, ref index);
			size = SaveHelper.LoadInt32(loadBytes, ref index);

			if (version == 0)
			{
				//version 0 had weirdly generated sizes. They would have one too many tree blocks for its size.
				size++;
				baseSize++;

				CubePosition basePosition = SaveHelper.LoadCubePosition(loadBytes, ref index);

				Position = basePosition.InWorldSpace() + new Vector3(Cube.CUBE_SCALE * 0.5f, 0, Cube.CUBE_SCALE * 0.5f);
				bounds = new Rectangle3D(basePosition.InWorldSpace(), new Vector3(Cube.CUBE_SCALE, Cube.CUBE_SCALE * (size + (Cube.CUBE_SCALE / 5)), Cube.CUBE_SCALE));

                trackedPositions = new CubePosition[size];

                for (int i = 0; i < size; i++)
                    trackedPositions[i] = basePosition + new CubePosition(0, i, 0);
            }
			else
			{
				trackedPositions = new CubePosition[size];
				for (int i = 0; i < size; i++)
					trackedPositions[i] = SaveHelper.LoadCubePosition(loadBytes, ref index);

                Position = trackedPositions[0].InWorldSpace() + new Vector3(Cube.CUBE_SCALE * 0.5f, 0, Cube.CUBE_SCALE * 0.5f);
                bounds = new Rectangle3D(trackedPositions[0].InWorldSpace(), new Vector3(Cube.CUBE_SCALE, Cube.CUBE_SCALE * (size + (Cube.CUBE_SCALE / 5)), Cube.CUBE_SCALE));
            }
		}
    }
}
