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
    public class SlimeBig : Entity
    {
		private static SimpleMesh<VertexCube, int> mesh;

		private AISlime ai;
        private NoticeHandler<Player> noticeHandler;

        private bool onGround;

        public SlimeBig()
        {
            
        }

		public SlimeBig(Vector3 position)
        {
			this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);
			
			noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 6.4f, false);

			ai = new AISlime(world, this, noticeHandler, new Rectangle3D(-new Vector3(Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE),
				new Vector3(Cube.CUBE_SCALE * 2)), 16);
		}

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			ai.Update(deltaTime, onGround);

			onGround = false;
			UpdateCollision();
        }

        public override void OnDelete()
        {
            base.OnDelete();

			ai.OnDelete();
        }

        private Vector2[] offsetsDown = new Vector2[4]
		{
			new Vector2(-1) * Cube.CUBE_SCALE,
			new Vector2(-1, 1) * Cube.CUBE_SCALE,
			new Vector2(1, -1) * Cube.CUBE_SCALE,
			new Vector2(1) * Cube.CUBE_SCALE
		};

		private Vector3[] directions = new Vector3[4]
		{
			new Vector3(-1, 0, 0),
			new Vector3(1, 0, 0),
			new Vector3(0, 0, -1),
			new Vector3(0, 0, 1)
		};

		private void UpdateCollision()
		{
			const float height = Cube.CUBE_SCALE * 2;
			for (int i = 0; i < 4; i++)
			{
				Vector3 startPos = new Vector3(Position.X + offsetsDown[i].X, Position.Y - height, Position.Z + offsetsDown[i].Y);
				Vector3 dir = new Vector3(0, height, 0);
				var resultDown = world.RaycastVector(startPos, dir, height, (Vector3 pos) =>
				{
					return world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetRaw(pos) != 0;
				});

				if (resultDown.hasHit)
				{
					CubePosition pos = CubePosition.FromWorldSpace(resultDown.hit);

					Position.Y = pos.Y * Cube.CUBE_SCALE + Cube.CUBE_SCALE + height;
					ai.Velocity.Y = 0;
					onGround = true;
				}
			}

			const float sideWidth = 0.45f;

			for (int i = 0; i < 4; i++)
			{
				Vector3 startPos = new Vector3(Position.X, Position.Y - height + Cube.CUBE_SCALE * 0.5f, Position.Z);
				ref Vector3 dir = ref directions[i];
				var resultSideBot = world.RaycastVector(startPos, dir, Cube.CUBE_SCALE * sideWidth, (Vector3 pos) =>
				{
					return world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetRaw(pos) != 0;
				});

				if (resultSideBot.hasHit)
				{
					CubePosition pos = CubePosition.FromWorldSpace(resultSideBot.hit);

					Vector3 offset = resultSideBot.hit - directions[i] * Cube.CUBE_SCALE * sideWidth;

					Position = new Vector3(offset.X, Position.Y, offset.Z);
				}
			}
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			base.Draw(device, effect);

			if (mesh == null)
				MakeMesh(device);

			int ysrc = 32;

			const float minInterval = 0.65f;
			const float maxInterval = 0.85f;

			float interval = MathHelper.Lerp(minInterval, maxInterval, ai.JumpTimer / ai.JumpTime) * 2;

			if (onGround && (ai.Alive % interval) / interval < 0.5f)
				ysrc = 64;

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(mesh.texture, DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position),
				noticeHandler.Noticed ? new RectangleF(32, ysrc, 32, 32) : new RectangleF(0, ysrc, 32, 32)));

			DrawHelper3D.DrawHealthbar(device, ai.Health, ai.MaxHealth, Position);
		}

        private void MakeMesh(GraphicsDevice device)
        {
			Vector3 min = -new Vector3(Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2, 0);
			Vector3 max = new Vector3(Cube.CUBE_SCALE, 0, 0);

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

			mesh = new SimpleMesh<VertexCube, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("slime"));
		}
    }
}
