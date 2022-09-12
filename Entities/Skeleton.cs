using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG.Entities
{
	public class Skeleton : Entity, IHitboxOwner
	{
		private static SimpleMesh<VertexPositionColorTextureNormal, int> mesh;

		private NoticeHandler<Player> noticeHandler;

		public Vector3 MaxVelocity = new Vector3(64, 340, 64);
		public Vector3 Velocity;

		private int health;
		private int maxHealth = 8;

		private bool onGround;

		private Rectangle3D Bounds => new Rectangle3D(Position - new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
			new Vector3(Cube.CUBE_SCALE * 0.70f, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 0.70f));
		private int hitbox = -1;

		private float invulnTimer;

		public Skeleton(Vector3 position)
		{
			this.Position = position;
		}

		public override void Initialize(World world)
		{
			base.Initialize(world);

			noticeHandler = new NoticeHandler<Player>(this, 128, false);
		}

		public override void Update(double deltaTime)
		{
			base.Update(deltaTime);

			if (hitbox == -1)
				hitbox = world.HitboxManager.Add(this, Bounds, Vector3.Zero, Slime.GROUP_ENEMYHOSTILE_SOURCE, 1, 1f);
			else world.HitboxManager.Update(hitbox, Bounds);

			Vector3 actualMaxVel = MaxVelocity;

			Velocity.Y += World.GRAVITY;

			noticeHandler.Update(deltaTime);

			if (invulnTimer <= 0)
			{
				if (noticeHandler.Noticed)
				{
					Vector3 dir = Vector3.Normalize(noticeHandler.GetNoticedEntity().Position - Position) * 100f;

					Velocity.X = dir.X;
					Velocity.Z = dir.Z;
				}

				Vector2 clampXY = new Vector2(actualMaxVel.X, actualMaxVel.Z);
				Vector2 velXY = new Vector2(Velocity.X, Velocity.Z);

				if (velXY.Length() > clampXY.Length())
				{
					velXY.Normalize();
					velXY *= clampXY.Length();
				}

				Velocity = new Vector3(velXY.X, Velocity.Y, velXY.Y);
			}

			if (Velocity.Y < -actualMaxVel.Y)
				Velocity.Y = -actualMaxVel.Y;

			if (onGround)
			{
				Vector2 velocitySlowed = new Vector2(Velocity.X, Velocity.Z);
				if (velocitySlowed.Length() > 0)
				{
					velocitySlowed = Vector2.Normalize(velocitySlowed) * velocitySlowed.Length() * 0.85f;
				}

				Velocity = new Vector3(velocitySlowed.X, Velocity.Y, velocitySlowed.Y);
			}

			Position += Velocity * (float)deltaTime;

			UpdateCollision();
		}

		private void UpdateCollision()
		{
			const int checkSize = 1;

			for (int x = -checkSize; x <= checkSize; x++)
			{
				for (int y = -checkSize; y <= checkSize; y++)
				{
					for (int z = -checkSize; z <= checkSize; z++)
					{
						CubePosition pos = CubePosition.FromWorldSpace(Position) + new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace); //CubePosition.FromWorldSpace(Position);

						if (world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).Collision != Cube.CollisionValue.None)
						{
							Rectangle3D cubeBounds = CubePosition.BoundsWorldSpace(pos);

							Vector3 checkPos = Position + new Vector3(0, 8, 0);
							if (CollisionHelper.CheckCollision(cubeBounds, checkPos, 8, out Vector3 change))
							{
								Position = (checkPos - new Vector3(0, 8, 0)) + change;

								if (change.Y > 0)
								{
									Velocity.Y = 0;
									onGround = true;
								}
								else if (change.Y < 0)
									Velocity.Y = 0;
								else if (change.X != 0)
									Velocity.X = 0;
								else if (change.Z != 0)
									Velocity.Z = 0;
							}
						}
					}
				}
			}
		}

		public override void OnDelete()
		{
			EntityItem ent = new EntityItem(Position, new Items.ItemInstance(Main.Registry.ItemRegistry.Get("brittle_bone"), 1, 1));
			ent.Velocity = new Vector3(Main.random.NextFloat(-100, 100), 128, Main.random.NextFloat(-100, 100));
			world.EntityManager.Add(ent);
			world.HitboxManager.Remove(hitbox);
			hitbox = -1;
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			base.Draw(device, effect);

			if (mesh == null)
				MakeMesh(device);

			//world.DrawWireframeUnscaled(device, Bounds, Color.Red);

			mesh.Draw(device, effect,
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position));
		}

		private static void MakeMesh(GraphicsDevice device)
		{
			Vector3 min = -new Vector3(Cube.CUBE_SCALE / 2f, 0, 0);
			Vector3 max = new Vector3(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE * 2, 0);

			Vector3 a = new Vector3(max.X, min.Y, max.Z);
			Vector3 b = new Vector3(min.X, min.Y, max.Z);
			Vector3 c = new Vector3(min.X, max.Y, max.Z);
			Vector3 d = new Vector3(max.X, max.Y, max.Z);

			List<VertexPositionColorTextureNormal> vertices = new List<VertexPositionColorTextureNormal>();
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

			mesh = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("skeleton"));
		}

		public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
		{
			//throw new NotImplementedException();
		}
	}
}
