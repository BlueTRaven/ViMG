using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Entities;

namespace ViMG.Entities
{
	public class Slime : Entity, IHitboxOwner
	{
		public Vector3 Velocity;

		public Vector3 MaxVelocity = new Vector3(3.2f * Cube.CUBE_SCALE, 17 * Cube.CUBE_SCALE, 3.2f * Cube.CUBE_SCALE);

		private static (VertexBuffer VBO, IndexBuffer IBO) mesh;
		//private static SimpleMesh<VertexCube, int> mesh;

		private bool onGround;

		private Rectangle3D Bounds => new Rectangle3D(Position - new Vector3(Cube.CUBE_SCALE * 0.35f, Cube.CUBE_SCALE * 0.70f, Cube.CUBE_SCALE * 0.35f), 
			new Vector3(Cube.CUBE_SCALE * 0.70f));
		private int hitbox = -1;

		private float invulnTimer;
		private float despawnTimer;
		private const float DESPAWN_TIME = 10f;

		private int health;
		private int maxHealth = 4;

		private Vector3 jumpDir;
		private int numJumps;
		private float jumpTimer;
		private float jumpTime;

		private float alive;

		private NoticeHandler<Player> noticeHandler;

		public Slime(Vector3 position)
		{
			this.Position = position;

			health = maxHealth;
		}

		public override void Initialize(World world)
		{
			base.Initialize(world);

			noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 6.4f, false);
		}

		public override void Update(double deltaTime)
		{
			alive += (float)deltaTime;

			if (hitbox == -1)
				hitbox = world.HitboxManager.Add(this, Bounds, Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, 1, 1f, invulnTimer <= 0);
			else world.HitboxManager.Update(hitbox, Bounds, invulnTimer <= 0);

			Vector3 actualMaxVel = MaxVelocity;

			Velocity.Y += World.GRAVITY;

			if (invulnTimer <= 0)
			{
				if (onGround)
				{
					jumpTimer -= (float)deltaTime;

					if (jumpTimer <= 0)
					{
						jumpTime = Main.random.NextFloat(0.25f, 3);
						jumpTimer = jumpTime;

						if (!noticeHandler.Noticed)
						{
							if (numJumps == 0)
							{
								numJumps = Main.random.Next(1, 6);
								jumpTime = Main.random.NextFloat(2, 6);
								jumpTimer = jumpTime;

								if (world.IsNight())
								{
									//During the night time, jump away from the player, regardless of whether or not they're noticed
									jumpDir = Position - world.player.Position;
									jumpDir.Normalize();
								}
								else
								{
									//During the day time, jump in random directions
									jumpDir = new Vector3(Main.random.NextFloat(-1, 1), 0, Main.random.NextFloat(-1, 1));
									jumpDir.Normalize();
								}
							}

							Velocity = new Vector3(jumpDir.X * 1.6f * Cube.CUBE_SCALE, MaxVelocity.Y * 0.75f, jumpDir.Y * 1.6f * Cube.CUBE_SCALE);

							numJumps--;
						}
						else
						{
							Vector2 playerDir = Vector2.Normalize(new Vector2(noticeHandler.Target.Position.X, noticeHandler.Target.Position.Z) - new Vector2(Position.X, Position.Z));
							Velocity = new Vector3(playerDir.X * 1.6f * Cube.CUBE_SCALE, MaxVelocity.Y * 0.75f, playerDir.Y * 1.6f * Cube.CUBE_SCALE);
						}
					
						onGround = false;
					}

					//At night, despawn 
					if (world.IsNight()) 
					{
						float distance = (Position - world.player.Position).Length();

						if (distance > Cube.CUBE_SCALE * 24)
						{
							despawnTimer -= (float)deltaTime;

							if (!noticeHandler.Noticed && despawnTimer <= 0)
								world.EntityManager.Remove(this);
						}
						else despawnTimer = DESPAWN_TIME;
					}
					else despawnTimer = DESPAWN_TIME;
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

			onGround = false;
			UpdateCollision(deltaTime);

			invulnTimer -= (float)deltaTime;

			noticeHandler.Update(deltaTime);

			//Kill self if too far away
			if ((world.player.Position - Position).Length() > 128 * Cube.CUBE_SCALE)
				world.EntityManager.Remove(this);
		}

		private void UpdateCollision(double deltaTime)
		{
			const float RADIUS = Cube.CUBE_SCALE * 0.25f;

			Vector3 realVelocity = Velocity * (float)deltaTime;

			CubePosition near = CubePosition.FromWorldSpace(Bounds.Position + (realVelocity + realVelocity * RADIUS));
			CubePosition far = CubePosition.FromWorldSpace(Bounds.FarPosition + (realVelocity + realVelocity * RADIUS));

			if (far.X < near.X)
			{
				var temp = far.X;
				far.X = near.X;
				near.X = temp;
			}

			if (far.Y < near.Y)
			{
				var temp = far.Y;
				far.Y = near.Y;
				near.Y = temp;
			}

			if (far.Z < near.Z)
			{
				var temp = far.Z;
				far.Z = near.Z;
				near.Z = temp;
			}

			for (int x = near.X; x <= far.X; x++)
			{
				for (int y = near.Y; y <= far.Y; y++)
				{
					for (int z = near.Z; z <= far.Z; z++)
					{
						CubePosition pos = new CubePosition(x, y, z);

						if (world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).Collision != Cube.CollisionValue.None)
						{
							Rectangle3D cubeBounds = CubePosition.BoundsWorldSpace(pos);

							Vector3 offset = new Vector3(0, RADIUS, 0);
							Vector3 checkPos = Position + offset;

							if (CollisionHelper.CheckCollision(cubeBounds, checkPos, RADIUS, out Vector3 change))
							{
								Position = (checkPos - offset) + change;

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
			base.OnDelete();

			EntityItem ent = new EntityItem(Position, new Items.ItemInstance(Main.Registry.ItemRegistry.Get("slime_chunk"), 1, 1));
			ent.Velocity = new Vector3(Main.random.NextFloat(-5 * Cube.CUBE_SCALE, 5 * Cube.CUBE_SCALE), 
				6.4f * Cube.CUBE_SCALE, Main.random.NextFloat(-5 * Cube.CUBE_SCALE, 5 * Cube.CUBE_SCALE));
			world.EntityManager.Add(ent);
			world.HitboxManager.Remove(hitbox);
			hitbox = -1;
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			if (mesh.VBO == null)
				mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);
			//MakeMeshes(device);

			int ysrc = 0;

			const float minInterval = 0.65f;
			const float maxInterval = 0.85f;

			float interval = MathHelper.Lerp(minInterval, maxInterval, jumpTimer / jumpTime) * 2;
			
			if (onGround && (alive % interval) / interval < 0.5f)
				ysrc = 16;

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("slime"), 
				DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position),
				noticeHandler.Noticed ? new RectangleF(16, ysrc, 16, 16) : new RectangleF(0, ysrc, 16, 16)));

			DrawHelper3D.DrawHealthbar(device, health, maxHealth, Position);
		}

		public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
		{
			if (invulnTimer <= 0)
			{
				if (other.group == HitboxManager.Group.PLAYER_DEAL)
				{
					Vector3 direction = Vector3.Normalize(other.direction);

					Velocity = new Vector3(direction.X * 3.2f * Cube.CUBE_SCALE, 6.4f * Cube.CUBE_SCALE, direction.Z * 3.2f * Cube.CUBE_SCALE);

					health -= other.damage;

					if (health <= 0)
					{
						world.EntityManager.Remove(this);
					}

					invulnTimer = 0.25f;

					noticeHandler.OnTakeDamage(other.owner as Player);
				}
			}
		}
	}
}
